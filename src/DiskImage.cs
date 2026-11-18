// Lettura delle immagini disco D64 (1541), D71 (1571 doppia faccia) e D81 (1581) dal PC, senza drive:
// directory, blocchi liberi (BAM) e contenuto dei file, seguendo le catene di blocchi come fa il DOS.
// Pensato per immagini anche rovinate: catene che puntano fuori dal disco o che girano in tondo
// danno un errore chiaro invece di bloccare il programma.
using System;
using System.Collections.Generic;
using System.IO;

namespace XumerTool
{
    public class DiskImage
    {
        public readonly ImageKind Kind;
        public readonly byte[] Data;
        public readonly string FilePath;
        /// <summary>True se in fondo all'immagine c'è la tabella degli errori dei settori.</summary>
        public readonly bool HasErrorTable;

        DiskImage(ImageKind kind, byte[] data, string path)
        {
            Kind = kind; Data = data; FilePath = path;
            HasErrorTable = data.Length > CbmDrive.TotalBlocks(kind) * 256;
        }

        public static DiskImage Load(string path) { return FromBytes(File.ReadAllBytes(path), path); }

        /// <summary>Immagine già in memoria (es. appena letta dal drive): path serve solo come nome.</summary>
        public static DiskImage FromBytes(byte[] data, string path)
        {
            ImageKind? k = CbmDrive.KindFromSize(data.Length);
            if (k == null)
                throw new InvalidDataException(string.Format(Lang.T("Dimensione dell'immagine non riconosciuta ({0} byte): sono supportate D64 (35 tracce), D71 e D81."), data.Length));
            return new DiskImage(k.Value, data, path);
        }

        /// <summary>Posizione nell'immagine del blocco traccia/settore (con controllo che esista).</summary>
        int Offset(int track, int sector)
        {
            if (track < 1 || track > CbmDrive.Tracks(Kind) || sector < 0 || sector >= CbmDrive.SectorsPerTrack(Kind, track))
                throw new InvalidDataException(string.Format(Lang.T("Immagine rovinata: rimanda a traccia {0} settore {1}, che non esiste."), track, sector));
            return CbmDrive.BlockOffset(Kind, track, sector);
        }

        int HeaderTrack { get { return Kind == ImageKind.D81 ? 40 : 18; } }

        public DiskDirectory ReadDirectory()
        {
            DiskDirectory d = new DiskDirectory();
            int hdr = Offset(HeaderTrack, 0);
            if (Kind == ImageKind.D81)
            {
                d.DiskName = Petscii.ToText(Data, hdr + 0x04, 16).TrimEnd();
                d.DiskId = Petscii.ToText(Data, hdr + 0x16, 5).Trim();
            }
            else
            {
                d.DiskName = Petscii.ToText(Data, hdr + 0x90, 16).TrimEnd();
                d.DiskId = Petscii.ToText(Data, hdr + 0xA2, 5).Trim();
            }
            d.BlocksFree = BlocksFree(hdr);

            // catena dei settori della directory: parte dal collegamento nell'intestazione
            int t = Data[hdr], s = Data[hdr + 1];
            HashSet<int> seen = new HashSet<int>();
            while (t != 0)
            {
                int o = Offset(t, s);
                if (!seen.Add(o)) break;   // directory che gira in tondo: ci si ferma
                for (int k = 0; k < 8; k++)
                {
                    int e = o + k * 32;
                    byte type = Data[e + 2];
                    if (type == 0) continue;   // voce vuota o file cancellato
                    int len = 16;
                    while (len > 0 && Data[e + 5 + len - 1] == 0xA0) len--;
                    byte[] name = new byte[len];
                    Array.Copy(Data, e + 5, name, 0, len);
                    d.Entries.Add(new DirEntry
                    {
                        NameBytes = name, Name = Petscii.ToText(name),
                        Blocks = Data[e + 30] | (Data[e + 31] << 8),
                        Type = TypeName(type & 0x07), Splat = (type & 0x80) == 0, Locked = (type & 0x40) != 0,
                        Track = Data[e + 3], Sector = Data[e + 4]
                    });
                }
                t = Data[o]; s = Data[o + 1];
            }
            return d;
        }

        static string TypeName(int t)
        {
            switch (t)
            {
                case 0: return "DEL";
                case 1: return "SEQ";
                case 2: return "PRG";
                case 3: return "USR";
                case 4: return "REL";
                case 5: return "CBM";   // partizione (1581)
                default: return "???";
            }
        }

        /// <summary>Blocchi liberi secondo la BAM, esclusa la traccia della directory (come "BLOCKS FREE." del drive).</summary>
        int BlocksFree(int hdr)
        {
            int free = 0;
            if (Kind == ImageKind.D81)
            {
                // BAM in traccia 40 settori 1 (tracce 1-40) e 2 (tracce 41-80): 6 byte per traccia da 0x10
                for (int part = 0; part < 2; part++)
                {
                    int bam = Offset(40, 1 + part);
                    for (int i = 0; i < 40; i++)
                        if (part * 40 + i + 1 != 40) free += Data[bam + 0x10 + i * 6];
                }
                return free;
            }
            for (int t = 1; t <= 35; t++)
                if (t != 18) free += Data[hdr + 4 * t];
            if (Kind == ImageKind.D71)
                for (int t = 36; t <= 70; t++)
                    if (t != 53) free += Data[hdr + 0xDD + (t - 36)];   // seconda faccia: contatori in 0xDD…0xFF
            return free;
        }

        /// <summary>Contenuto di un file della directory, seguendo la sua catena di blocchi.</summary>
        public byte[] ReadFile(DirEntry e)
        {
            if (e.Type == "CBM") throw new InvalidDataException(Lang.T("Le partizioni del 1581 non si possono estrarre come file."));
            MemoryStream ms = new MemoryStream();
            int t = e.Track, s = e.Sector;
            int guard = CbmDrive.TotalBlocks(Kind);
            while (t != 0)
            {
                if (guard-- <= 0) throw new InvalidDataException(Lang.F("Immagine rovinata: la catena dei blocchi del file \"{0}\" gira in tondo.", e.Name));
                int o = Offset(t, s);
                int nt = Data[o], ns = Data[o + 1];
                if (nt == 0)
                {
                    // ultimo blocco: il secondo byte è la posizione dell'ultimo byte usato
                    int last = Math.Max(1, (int)ns);
                    ms.Write(Data, o + 2, last - 1);
                }
                else ms.Write(Data, o + 2, 254);
                t = nt; s = ns;
            }
            return ms.ToArray();
        }
    }
}
