// Una sola finestra di XUMER TOOL per utente: un secondo avvio (es. doppio clic su un .d64 in
// Esplora risorse con l'app già aperta) passa l'immagine alla finestra esistente e si chiude,
// invece di aprire una seconda copia che litigherebbe con la prima per l'adattatore USB.
// Il passaggio avviene con una "pipe" locale con nome (solo su questo PC, solo questo utente).
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace XumerTool
{
    static class SingleInstance
    {
        [DllImport("user32.dll")]
        static extern bool AllowSetForegroundWindow(int processId);
        const int ASFW_ANY = -1;

        static Mutex mutex;

        static string Suffix { get { return Process.GetCurrentProcess().SessionId + "." + Environment.UserName; } }
        static string PipeName { get { return "XUMERTool.Open." + Suffix; } }

        /// <summary>
        /// True = questa è la prima finestra e deve partire. False = ce n'è già una: le è stata passata
        /// l'immagine (o la richiesta di venire in primo piano) e questo avvio deve chiudersi.
        /// </summary>
        public static bool Acquire(string imagePath)
        {
            bool created;
            mutex = new Mutex(true, @"Local\XUMERTool.SingleInstance." + Suffix, out created);
            if (created) return true;
            try
            {
                AllowSetForegroundWindow(ASFW_ANY);   // la finestra già aperta può venire in primo piano
                using (NamedPipeClientStream c = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    c.Connect(3000);
                    byte[] b = Encoding.UTF8.GetBytes(imagePath ?? "");
                    c.Write(b, 0, b.Length);
                }
                mutex.Dispose(); mutex = null;
                return false;
            }
            catch
            {
                // la finestra esistente non risponde (es. si sta chiudendo): si parte comunque
                mutex.Dispose(); mutex = null;
                return true;
            }
        }

        /// <summary>Ascolta i messaggi dei nuovi avvii (percorso dell'immagine, o vuoto = solo primo piano).</summary>
        public static void Listen(Action<string> onMessage)
        {
            Thread t = new Thread(delegate ()
            {
                while (true)
                {
                    try
                    {
                        using (NamedPipeServerStream s = new NamedPipeServerStream(PipeName, PipeDirection.In, 1))
                        {
                            s.WaitForConnection();
                            MemoryStream ms = new MemoryStream();
                            s.CopyTo(ms);
                            onMessage(Encoding.UTF8.GetString(ms.ToArray()));
                        }
                    }
                    catch { Thread.Sleep(500); }   // es. la pipe è ancora occupata dalla finestra che si sta chiudendo
                }
            });
            t.IsBackground = true;
            t.Start();
        }

        /// <summary>Da chiamare prima di riavviare l'app (cambio lingua): la nuova finestra deve poter partire.</summary>
        public static void Release()
        {
            if (mutex == null) return;
            try { mutex.ReleaseMutex(); } catch { }
            mutex.Dispose();
            mutex = null;
        }
    }
}
