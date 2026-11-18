// Protocollo USB dell'adattatore XUM1541 (compatibile con il firmware OpenCBM v07/v08).
using System;
using System.Collections.Generic;
using System.IO;

namespace XumerTool
{
    public sealed class Xum1541 : IDisposable
    {
        public const int VID = 0x16D0, PID = 0x0504;
        const byte EP_IN = 0x83, EP_OUT = 0x04;

        // richieste di controllo
        const byte REQ_INIT = 1, REQ_RESET = 2, REQ_SHUTDOWN = 3;
        // comandi bulk
        const byte CMD_READ = 8, CMD_WRITE = 9, CMD_GET_EOI = 23, CMD_CLEAR_EOI = 24;
        // protocollo e flag
        public const byte CBM = 0x10;
        const byte WRITE_TALK = 0x01, WRITE_ATN = 0x02;
        // stati
        const byte IO_BUSY = 1, IO_READY = 2, IO_ERROR = 3;

        public const int MaxXfer = 32768;

        WinUsbDevice usb;
        public int Version, Capabilities, InitStatus;

        /// <summary>Tempo massimo tra due pacchetti di stato (il firmware manda "occupato" periodicamente).</summary>
        public int StatusTimeoutMs = 8000;
        /// <summary>Tempo massimo per ricevere i dati di una lettura.</summary>
        public int DataTimeoutMs = 15000;

        Xum1541(WinUsbDevice d) { usb = d; }

        public static Xum1541 Open()
        {
            List<string> paths = WinUsbDevice.FindDevicePaths(VID, PID);
            if (paths.Count == 0)
                throw new Exception(Lang.T("Adattatore xum1541 non trovato. Controlla il cavo USB e che il driver WinUSB sia installato con Zadig."));
            WinUsbDevice d = WinUsbDevice.Open(paths[0]);
            Xum1541 x = new Xum1541(d);
            try { x.Init(); }
            catch { d.Dispose(); throw; }
            return x;
        }

        public void Init()
        {
            byte[] r = usb.ControlIn(0xA1, REQ_INIT, 0, 0, 8);
            if (r.Length < 2) throw new IOException(Lang.T("Risposta INIT non valida dall'adattatore"));
            Version = r[0];
            Capabilities = r[1];
            InitStatus = r.Length > 2 ? r[2] : 0;
            if ((InitStatus & 0x01) != 0)
            {
                // il comando precedente era stato interrotto
                usb.ResetPipe(EP_IN);
                usb.ResetPipe(EP_OUT);
            }
        }

        /// <summary>Tentativo di ripristino dopo un errore/timeout.</summary>
        public void Recover()
        {
            try { usb.AbortPipe(EP_IN); } catch { }
            try { usb.AbortPipe(EP_OUT); } catch { }
            try { usb.ResetPipe(EP_IN); } catch { }
            try { usb.ResetPipe(EP_OUT); } catch { }
            try { Init(); } catch { }
        }

        /// <summary>Riavvia il Pro Micro nel suo bootloader (per scrivere un nuovo firmware).</summary>
        public void EnterBootloader()
        {
            try { usb.ControlOut(0x21, 4, 0, 0); } catch { } // il dispositivo si scollega subito
            usb.Dispose();
            usb = null;
        }

        /// <summary>Reset del bus IEC (riavvia i drive collegati).</summary>
        public void BusReset()
        {
            usb.ControlOut(0x21, REQ_RESET, 0, 0);
        }

        /// <summary>Limite massimo di attesa di una risposta, anche se l'adattatore continua a dire "occupato".</summary>
        public int MaxWaitMs = 240000;

        int WaitStatus()
        {
            DateTime limit = DateTime.Now.AddMilliseconds(MaxWaitMs);
            while (true)
            {
                if (DateTime.Now > limit)
                    throw new UsbTimeoutException(Lang.T("Il drive non risponde da troppo tempo (è stato spento o scollegato?)"));
                byte[] s = usb.Read(EP_IN, 3, StatusTimeoutMs);
                if (s.Length == 0) continue;
                if (s.Length != 3) throw new IOException(Lang.T("Risposta di stato non valida (") + s.Length + Lang.T(" byte)"));
                if (s[0] == IO_BUSY) continue;
                if (s[0] == IO_READY) return s[1] | (s[2] << 8);
                if (s[0] == IO_ERROR) return -1;
                throw new IOException(Lang.T("Stato sconosciuto dall'adattatore: ") + s[0]);
            }
        }

        void SendCmd(byte a, byte b, byte c, byte d)
        {
            usb.Write(EP_OUT, new byte[] { a, b, c, d }, 0, 4, 5000);
        }

        public int Write(byte mode, byte[] data, int offset, int length)
        {
            if (length > MaxXfer) throw new ArgumentException(Lang.T("troppi dati"));
            SendCmd(CMD_WRITE, mode, (byte)(length & 0xFF), (byte)(length >> 8));
            usb.Write(EP_OUT, data, offset, length, 5000);
            if ((mode & 0xF0) == CBM) return WaitStatus();
            return length;
        }

        public byte[] Read(byte mode, int length)
        {
            if (length > MaxXfer) throw new ArgumentException(Lang.T("troppi dati"));
            SendCmd(CMD_READ, mode, (byte)(length & 0xFF), (byte)(length >> 8));
            return usb.Read(EP_IN, length, DataTimeoutMs);
        }

        public int Ioctl(byte cmd, byte addr, byte sec)
        {
            SendCmd(cmd, addr, sec, 0);
            return WaitStatus();
        }

        // ---- comandi IEC di base (come opencbm) ----

        bool AtnBytes(byte flags, params byte[] b)
        {
            return Write((byte)(CBM | flags), b, 0, b.Length) > 0;
        }

        public bool Listen(int dev, int sec) { return AtnBytes(WRITE_ATN, (byte)(0x20 | dev), (byte)(0x60 | sec)); }
        public bool Talk(int dev, int sec) { return AtnBytes(WRITE_ATN | WRITE_TALK, (byte)(0x40 | dev), (byte)(0x60 | sec)); }
        public bool OpenChannel(int dev, int sec) { return AtnBytes(WRITE_ATN, (byte)(0x20 | dev), (byte)(0xF0 | sec)); }
        public bool CloseChannel(int dev, int sec) { return AtnBytes(WRITE_ATN, (byte)(0x20 | dev), (byte)(0xE0 | sec)); }
        public bool Unlisten() { return AtnBytes(WRITE_ATN, 0x3F); }
        public bool Untalk() { return AtnBytes(WRITE_ATN, 0x5F); }

        // ---- linee IEC e protocollo veloce S2 ----
        public const byte S2 = 0x30;
        public const byte IEC_DATA = 0x01, IEC_CLOCK = 0x02, IEC_ATN = 0x04;
        const byte CMD_IEC_POLL = 27, CMD_IEC_SETRELEASE = 29;

        /// <summary>Stato delle linee (bit a 1 = linea attiva/bassa).</summary>
        public int IecPoll() { return Ioctl(CMD_IEC_POLL, 0, 0); }
        public void IecSetRelease(byte set, byte release) { Ioctl(CMD_IEC_SETRELEASE, set, release); }

        /// <summary>Interrompe un'operazione bloccata nel firmware (fa anche un reset del bus IEC).</summary>
        public void Abort()
        {
            try { usb.ControlOut(0x21, REQ_RESET, 0, 0); } catch { }
            System.Threading.Thread.Sleep(300);
            Recover();
        }

        public int RawWrite(byte[] data, int offset, int length) { return Write(CBM, data, offset, length); }
        public byte[] RawRead(int length) { return Read(CBM, length); }
        public bool GetEoi() { return Ioctl(CMD_GET_EOI, 0, 0) > 0; }
        public void ClearEoi() { Ioctl(CMD_CLEAR_EOI, 0, 0); }

        public void Dispose()
        {
            if (usb == null) return;
            try { usb.ControlOut(0x21, REQ_SHUTDOWN, 0, 0); } catch { }
            usb.Dispose();
            usb = null;
        }
    }
}
