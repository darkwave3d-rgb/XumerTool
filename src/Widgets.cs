// Temi, impostazioni e controlli grafici personalizzati.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace XumerTool
{
    /// <summary>Impostazioni salvate in %APPDATA%\XUMERTool\settings.ini</summary>
    static class Settings
    {
        static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XUMERTool");
        static readonly string FilePath = Path.Combine(Dir, "settings.ini");
        static Dictionary<string, string> values;
        static bool migrated;

        /// <summary>Cartella dei dati dell'app (%APPDATA%\XUMERTool): impostazioni e firmware scaricati.</summary>
        public static string AppDataDir
        {
            get
            {
                if (!migrated)
                {
                    migrated = true;
                    // le prime versioni si chiamavano "XUM1571 Tool": sposta impostazioni e firmware nella nuova cartella
                    try
                    {
                        string old = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XUM1571Tool");
                        if (!Directory.Exists(Dir) && Directory.Exists(old)) Directory.Move(old, Dir);
                    }
                    catch { }
                }
                return Dir;
            }
        }

        static void Load()
        {
            if (values != null) return;
            values = new Dictionary<string, string>();
            if (AppDataDir == null) return; // esegue lo spostamento dalla cartella delle prime versioni
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    int eq = line.IndexOf('=');
                    if (eq > 0) values[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
                }
            }
            catch { }
        }

        public static string Get(string key, string def)
        {
            Load();
            string v;
            return values.TryGetValue(key, out v) ? v : def;
        }

        public static void Set(string key, string value) { SetMany(key, value); }

        /// <summary>Salva più valori insieme (coppie chiave, valore) con una sola scrittura del file.</summary>
        public static void SetMany(params string[] keyValues)
        {
            Load();
            for (int i = 0; i + 1 < keyValues.Length; i += 2) values[keyValues[i]] = keyValues[i + 1] ?? "";
            try
            {
                Directory.CreateDirectory(Dir);
                List<string> lines = new List<string>();
                foreach (KeyValuePair<string, string> kv in values) lines.Add(kv.Key + "=" + kv.Value);
                File.WriteAllLines(FilePath, lines.ToArray());
            }
            catch { }
        }
    }

    /// <summary>Tavolozza completa di un tema (usata anche per le anteprime nelle Preferenze).</summary>
    class Palette
    {
        public string Name, Note;
        public bool Dark;
        public Color Window, Header, HeaderButton, HeaderButtonHover, HeaderText, HeaderMuted;
        public Color Surface, Border, Text, Muted, Icon, Danger, Accent, AccentHover, AccentText;
        public Color Ok, Busy, Off;
        public Color C64Frame, C64FrameDrop, C64Back, C64Text;
        public Color LogBack, LogText, LogSent, LogReply, LogError, LogTime;
        public Color InputBack, InputText, FieldBack, FieldText;
        public Color SectionDisk, SectionFile, SectionImage, SectionCaptionText;
        public Color TabLog, TabDos, TabVice, TabGames, TabAdapter, TabArchive;
    }

    static class Theme
    {
        // prima i temi chiari, poi quelli scuri (le Preferenze li raggruppano così)
        public static readonly string[] Names = { "Chiaro", "Commodore", "Rosé Pine", "Scuro", "Tokyo Night", "Catppuccin", "Everforest", "Gruvbox", "Nord", "Lunar Tear" };
        public static string Current = "Chiaro";
        public static bool Dark;

        public static Color Window, Header, HeaderButton, HeaderButtonHover, HeaderText, HeaderMuted;
        public static Color Surface, Border, Text, Muted, Icon, Danger, Accent, AccentHover, AccentText;
        public static Color Ok, Busy, Off;
        public static Color C64Frame, C64FrameDrop, C64Back, C64Text;
        public static Color LogBack, LogText, LogSent, LogReply, LogError, LogTime;
        public static Color InputBack, InputText, FieldBack, FieldText;
        public static Color SectionDisk, SectionFile, SectionImage, SectionCaptionText;
        public static Color TabLog, TabDos, TabVice, TabGames, TabAdapter, TabArchive;

        static Color H(int rgb) { return Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF); }

        static Theme() { Apply("Chiaro"); }

        public static void Apply(string name)
        {
            Palette p = Get(name);
            Current = p.Name; Dark = p.Dark;
            Window = p.Window; Header = p.Header; HeaderButton = p.HeaderButton; HeaderButtonHover = p.HeaderButtonHover;
            HeaderText = p.HeaderText; HeaderMuted = p.HeaderMuted;
            Surface = p.Surface; Border = p.Border; Text = p.Text; Muted = p.Muted;
            Icon = p.Icon; Danger = p.Danger; Accent = p.Accent; AccentHover = p.AccentHover; AccentText = p.AccentText;
            Ok = p.Ok; Busy = p.Busy; Off = p.Off;
            C64Frame = p.C64Frame; C64FrameDrop = p.C64FrameDrop; C64Back = p.C64Back; C64Text = p.C64Text;
            LogBack = p.LogBack; LogText = p.LogText; LogSent = p.LogSent; LogReply = p.LogReply; LogError = p.LogError; LogTime = p.LogTime;
            InputBack = p.InputBack; InputText = p.InputText; FieldBack = p.FieldBack; FieldText = p.FieldText;
            SectionDisk = p.SectionDisk; SectionFile = p.SectionFile; SectionImage = p.SectionImage; SectionCaptionText = p.SectionCaptionText;
            TabLog = p.TabLog; TabDos = p.TabDos; TabVice = p.TabVice; TabGames = p.TabGames; TabAdapter = p.TabAdapter; TabArchive = p.TabArchive;
        }

        /// <summary>Tavolozza di un tema per nome (senza applicarla). Nome sconosciuto = "Chiaro".</summary>
        public static Palette Get(string name)
        {
            switch (name)
            {
                case "Scuro": return new Palette
                {
                    Name = "Scuro", Note = Lang.T("Grigio ardesia con accenti indaco"), Dark = true,
                    Window = H(0x111827), Header = H(0x0B1220), HeaderButton = H(0x1F2937), HeaderButtonHover = H(0x374151),
                    HeaderText = H(0xF9FAFB), HeaderMuted = H(0x9CA3AF),
                    Surface = H(0x1F2937), Border = H(0x374151), Text = H(0xE5E7EB), Muted = H(0x9CA3AF),
                    Icon = H(0xA5B4FC), Danger = H(0xF87171), Accent = H(0x6366F1), AccentHover = H(0x818CF8), AccentText = Color.White,
                    Ok = H(0x22C55E), Busy = H(0xF59E0B), Off = H(0xEF4444),
                    C64Frame = H(0x4C47B8), C64FrameDrop = H(0x7C78F0), C64Back = H(0x1E1B4B), C64Text = H(0xA5B4FC),
                    LogBack = H(0x0B1220), LogText = H(0xD1D5DB), LogSent = H(0x93C5FD), LogReply = H(0x86EFAC), LogError = H(0xFCA5A5), LogTime = H(0x6B7280),
                    InputBack = H(0x1F2937), InputText = H(0xF9FAFB), FieldBack = H(0x111827), FieldText = H(0xE5E7EB),
                    SectionDisk = H(0x818CF8), SectionFile = H(0x2DD4BF), SectionImage = H(0xFB923C), SectionCaptionText = H(0x0B1220),
                    TabLog = H(0x38BDF8), TabDos = H(0xA3E635), TabVice = H(0xF472B6), TabGames = H(0xC084FC), TabAdapter = H(0xFACC15), TabArchive = H(0x14B8A6)
                };
                case "Tokyo Night": return new Palette
                {
                    Name = "Tokyo Night", Note = Lang.T("Blu notte con accenti neon tenui"), Dark = true,
                    Window = H(0x1A1B26), Header = H(0x16161E), HeaderButton = H(0x292E42), HeaderButtonHover = H(0x3B4261),
                    HeaderText = H(0xC0CAF5), HeaderMuted = H(0x737AA2),
                    Surface = H(0x1F2335), Border = H(0x292E42), Text = H(0xC0CAF5), Muted = H(0x737AA2),
                    Icon = H(0x7AA2F7), Danger = H(0xF7768E), Accent = H(0x7AA2F7), AccentHover = H(0x89B4FA), AccentText = H(0x1A1B26),
                    Ok = H(0x9ECE6A), Busy = H(0xE0AF68), Off = H(0xF7768E),
                    C64Frame = H(0x3D59A1), C64FrameDrop = H(0x7AA2F7), C64Back = H(0x16161E), C64Text = H(0x7AA2F7),
                    LogBack = H(0x16161E), LogText = H(0xA9B1D6), LogSent = H(0x7DCFFF), LogReply = H(0x9ECE6A), LogError = H(0xF7768E), LogTime = H(0x565F89),
                    InputBack = H(0x292E42), InputText = H(0xC0CAF5), FieldBack = H(0x1A1B26), FieldText = H(0xC0CAF5),
                    SectionDisk = H(0xBB9AF7), SectionFile = H(0x73DACA), SectionImage = H(0xFF9E64), SectionCaptionText = H(0x1A1B26),
                    TabLog = H(0x7DCFFF), TabDos = H(0x9ECE6A), TabVice = H(0xE0AF68), TabGames = H(0xBB9AF7), TabAdapter = H(0xFF9E64), TabArchive = H(0x73DACA)
                };
                // Commodore: plastica beige del C64 e del 1541, tasti marroni, schermo con i colori originali (Colodore),
                // sezioni e schede con l'arcobaleno del logo in versione pastello.
                case "Commodore": return new Palette
                {
                    Name = "Commodore", Note = Lang.T("Beige e marrone del C64, arcobaleno C="), Dark = false,
                    Window = H(0xE6DFCE), Header = H(0x3F332A), HeaderButton = H(0x56483C), HeaderButtonHover = H(0x6A5A4C),
                    HeaderText = H(0xF4ECDD), HeaderMuted = H(0xB5A58F),
                    Surface = H(0xF5F0E4), Border = H(0xD9CEB8), Text = H(0x3A2E25), Muted = H(0x84735F),
                    Icon = H(0x5E54B5), Danger = H(0xC0504D), Accent = H(0x5E54B5), AccentHover = H(0x6E64C6), AccentText = Color.White,
                    Ok = H(0x5DAA55), Busy = H(0xE2A23B), Off = H(0xC9554E),
                    C64Frame = H(0x7C70DA), C64FrameDrop = H(0xABA4F2), C64Back = H(0x40318D), C64Text = H(0x7C70DA),
                    LogBack = H(0x2D251F), LogText = H(0xE8DFCF), LogSent = H(0xA9B8EC), LogReply = H(0xA8D69A), LogError = H(0xF0A39A), LogTime = H(0x8C7D6A),
                    InputBack = H(0x4A3D33), InputText = H(0xF4ECDD), FieldBack = H(0xFFFCF5), FieldText = H(0x3A2E25),
                    SectionDisk = H(0x8F88E0), SectionFile = H(0x84BF78), SectionImage = H(0xE59A5C), SectionCaptionText = H(0x2E241C),
                    TabLog = H(0x82A9DE), TabDos = H(0x8CC67E), TabVice = H(0xE58074), TabGames = H(0xEBA35E), TabAdapter = H(0xE6CF6A), TabArchive = H(0x6FC2C2)
                };
                // Rosé Pine (variante Dawn per l'interfaccia, Moon per registro e schermo)
                case "Rosé Pine": return new Palette
                {
                    Name = "Rosé Pine", Note = Lang.T("Chiaro e caldo, toni rosa, iris e pino"), Dark = false,
                    Window = H(0xF2E9E1), Header = H(0x46415F), HeaderButton = H(0x5A5576), HeaderButtonHover = H(0x6E698A),
                    HeaderText = H(0xFAF4ED), HeaderMuted = H(0xB9B4C9),
                    Surface = H(0xFFFAF3), Border = H(0xDFDAD9), Text = H(0x575279), Muted = H(0x797593),
                    Icon = H(0x907AA9), Danger = H(0xB4637A), Accent = H(0x286983), AccentHover = H(0x3A7D97), AccentText = H(0xFFFAF3),
                    Ok = H(0x9CCFD8), Busy = H(0xF6C177), Off = H(0xEB6F92),
                    C64Frame = H(0x907AA9), C64FrameDrop = H(0xC4A7E7), C64Back = H(0x393552), C64Text = H(0xC4A7E7),
                    LogBack = H(0x2A273F), LogText = H(0xE0DEF4), LogSent = H(0xC4A7E7), LogReply = H(0x9CCFD8), LogError = H(0xEB6F92), LogTime = H(0x6E6A86),
                    InputBack = H(0x44415A), InputText = H(0xE0DEF4), FieldBack = H(0xFFFAF3), FieldText = H(0x575279),
                    SectionDisk = H(0x7E6A99), SectionFile = H(0x3F7F8A), SectionImage = H(0xC2706C), SectionCaptionText = H(0xFFFAF3),
                    TabLog = H(0x9CCFD8), TabDos = H(0xF6C177), TabVice = H(0xEB6F92), TabGames = H(0xC4A7E7), TabAdapter = H(0xEA9A97), TabArchive = H(0xEBBCBA)
                };
                case "Catppuccin": return new Palette
                {
                    Name = "Catppuccin", Note = Lang.T("Mocha: pastelli morbidi su fondo scuro"), Dark = true,
                    Window = H(0x1E1E2E), Header = H(0x11111B), HeaderButton = H(0x313244), HeaderButtonHover = H(0x45475A),
                    HeaderText = H(0xCDD6F4), HeaderMuted = H(0x7F849C),
                    Surface = H(0x262637), Border = H(0x313244), Text = H(0xCDD6F4), Muted = H(0x7F849C),
                    Icon = H(0xCBA6F7), Danger = H(0xF38BA8), Accent = H(0xCBA6F7), AccentHover = H(0xD7BAFA), AccentText = H(0x1E1E2E),
                    Ok = H(0xA6E3A1), Busy = H(0xF9E2AF), Off = H(0xF38BA8),
                    C64Frame = H(0x45475A), C64FrameDrop = H(0xB4BEFE), C64Back = H(0x11111B), C64Text = H(0xB4BEFE),
                    LogBack = H(0x11111B), LogText = H(0xBAC2DE), LogSent = H(0x89DCEB), LogReply = H(0xA6E3A1), LogError = H(0xF38BA8), LogTime = H(0x6C7086),
                    InputBack = H(0x313244), InputText = H(0xCDD6F4), FieldBack = H(0x1E1E2E), FieldText = H(0xCDD6F4),
                    SectionDisk = H(0xB4BEFE), SectionFile = H(0x94E2D5), SectionImage = H(0xFAB387), SectionCaptionText = H(0x11111B),
                    TabLog = H(0x89B4FA), TabDos = H(0xA6E3A1), TabVice = H(0xF5C2E7), TabGames = H(0xCBA6F7), TabAdapter = H(0xF9E2AF), TabArchive = H(0x94E2D5)
                };
                case "Everforest": return new Palette
                {
                    Name = "Everforest", Note = Lang.T("Verde bosco e sabbia, riposante"), Dark = true,
                    Window = H(0x2D353B), Header = H(0x232A2E), HeaderButton = H(0x343F44), HeaderButtonHover = H(0x475258),
                    HeaderText = H(0xD3C6AA), HeaderMuted = H(0x859289),
                    Surface = H(0x343F44), Border = H(0x475258), Text = H(0xD3C6AA), Muted = H(0x859289),
                    Icon = H(0xA7C080), Danger = H(0xE67E80), Accent = H(0xA7C080), AccentHover = H(0xB7CD94), AccentText = H(0x2D353B),
                    Ok = H(0xA7C080), Busy = H(0xDBBC7F), Off = H(0xE67E80),
                    C64Frame = H(0x425047), C64FrameDrop = H(0xA7C080), C64Back = H(0x232A2E), C64Text = H(0xA7C080),
                    LogBack = H(0x232A2E), LogText = H(0xD3C6AA), LogSent = H(0x7FBBB3), LogReply = H(0xA7C080), LogError = H(0xE67E80), LogTime = H(0x7A8478),
                    InputBack = H(0x343F44), InputText = H(0xD3C6AA), FieldBack = H(0x2D353B), FieldText = H(0xD3C6AA),
                    SectionDisk = H(0x7FBBB3), SectionFile = H(0x83C092), SectionImage = H(0xE69875), SectionCaptionText = H(0x232A2E),
                    TabLog = H(0x7FBBB3), TabDos = H(0xA7C080), TabVice = H(0xD699B6), TabGames = H(0xDBBC7F), TabAdapter = H(0xE69875), TabArchive = H(0x83C092)
                };
                case "Gruvbox": return new Palette
                {
                    Name = "Gruvbox", Note = Lang.T("Caldo e retrò, schermo color ambra"), Dark = true,
                    Window = H(0x282828), Header = H(0x1D2021), HeaderButton = H(0x3C3836), HeaderButtonHover = H(0x504945),
                    HeaderText = H(0xEBDBB2), HeaderMuted = H(0xA89984),
                    Surface = H(0x32302F), Border = H(0x504945), Text = H(0xEBDBB2), Muted = H(0xA89984),
                    Icon = H(0xFABD2F), Danger = H(0xFB4934), Accent = H(0xD79921), AccentHover = H(0xFABD2F), AccentText = H(0x282828),
                    Ok = H(0xB8BB26), Busy = H(0xFABD2F), Off = H(0xFB4934),
                    C64Frame = H(0x504945), C64FrameDrop = H(0xFABD2F), C64Back = H(0x1D2021), C64Text = H(0xFABD2F),
                    LogBack = H(0x1D2021), LogText = H(0xD5C4A1), LogSent = H(0x83A598), LogReply = H(0xB8BB26), LogError = H(0xFB4934), LogTime = H(0x7C6F64),
                    InputBack = H(0x3C3836), InputText = H(0xEBDBB2), FieldBack = H(0x282828), FieldText = H(0xEBDBB2),
                    SectionDisk = H(0x83A598), SectionFile = H(0x8EC07C), SectionImage = H(0xE78A4E), SectionCaptionText = H(0x1D2021),
                    TabLog = H(0x83A598), TabDos = H(0xB8BB26), TabVice = H(0xD3869B), TabGames = H(0x8EC07C), TabAdapter = H(0xE78A4E), TabArchive = H(0xFABD2F)
                };
                case "Nord": return new Palette
                {
                    Name = "Nord", Note = Lang.T("Blu artici e grigi freddi"), Dark = true,
                    Window = H(0x2E3440), Header = H(0x242933), HeaderButton = H(0x3B4252), HeaderButtonHover = H(0x4C566A),
                    HeaderText = H(0xECEFF4), HeaderMuted = H(0x8C96A8),
                    Surface = H(0x3B4252), Border = H(0x4C566A), Text = H(0xE5E9F0), Muted = H(0x9AA3B5),
                    Icon = H(0x88C0D0), Danger = H(0xBF616A), Accent = H(0x88C0D0), AccentHover = H(0x8FBCBB), AccentText = H(0x2E3440),
                    Ok = H(0xA3BE8C), Busy = H(0xEBCB8B), Off = H(0xBF616A),
                    C64Frame = H(0x5E81AC), C64FrameDrop = H(0x88C0D0), C64Back = H(0x242933), C64Text = H(0x88C0D0),
                    LogBack = H(0x242933), LogText = H(0xD8DEE9), LogSent = H(0x81A1C1), LogReply = H(0xA3BE8C), LogError = H(0xBF616A), LogTime = H(0x616E88),
                    InputBack = H(0x3B4252), InputText = H(0xECEFF4), FieldBack = H(0x2E3440), FieldText = H(0xE5E9F0),
                    SectionDisk = H(0x81A1C1), SectionFile = H(0x8FBCBB), SectionImage = H(0xD08770), SectionCaptionText = H(0x242933),
                    TabLog = H(0x88C0D0), TabDos = H(0xA3BE8C), TabVice = H(0xB48EAD), TabGames = H(0xEBCB8B), TabAdapter = H(0xD08770), TabArchive = H(0x8FBCBB)
                };
                // Lunar Tear: ispirato ai colori del tema per Blender "Lunar Tear UI" (Raze-Demon):
                // blu-verde della notte quasi nero, luce di luna azzurra, verde acqua, oliva e un tocco d'arancio.
                case "Lunar Tear": return new Palette
                {
                    Name = "Lunar Tear", Note = Lang.T("Notte blu-verde e luce di luna"), Dark = true,
                    Window = H(0x0F1B25), Header = H(0x07131C), HeaderButton = H(0x16232F), HeaderButtonHover = H(0x26323B),
                    HeaderText = H(0xD6DCDA), HeaderMuted = H(0x7F8C8E),
                    Surface = H(0x15222B), Border = H(0x26323B), Text = H(0xD0D6D4), Muted = H(0x8B908B),
                    Icon = H(0x6DAFE9), Danger = H(0xD9704F), Accent = H(0x6DAFE9), AccentHover = H(0x8BC2F0), AccentText = H(0x07131C),
                    Ok = H(0x4FB8A2), Busy = H(0xC9B458), Off = H(0xD9704F),
                    C64Frame = H(0x1D3A47), C64FrameDrop = H(0x6DAFE9), C64Back = H(0x081820), C64Text = H(0x8FC7E8),
                    LogBack = H(0x081018), LogText = H(0xC2C8C4), LogSent = H(0x6DAFE9), LogReply = H(0x6CC0A8), LogError = H(0xE07A5F), LogTime = H(0x55666B),
                    InputBack = H(0x16232F), InputText = H(0xD6DCDA), FieldBack = H(0x0F1B25), FieldText = H(0xD6DCDA),
                    SectionDisk = H(0x4A9BD0), SectionFile = H(0x3FA595), SectionImage = H(0xC9B458), SectionCaptionText = H(0x061018),
                    TabLog = H(0x6DAFE9), TabDos = H(0x6CC0A8), TabVice = H(0xE0874F), TabGames = H(0xC9B458), TabAdapter = H(0x9DBB6E), TabArchive = H(0x5FC4C4)
                };
                default: return new Palette
                {
                    Name = "Chiaro", Note = Lang.T("Neutro, con lo schermo blu del C64"), Dark = false,
                    Window = H(0xF3F4F6), Header = H(0x1F2937), HeaderButton = H(0x374151), HeaderButtonHover = H(0x4B5563),
                    HeaderText = Color.White, HeaderMuted = H(0x9CA3AF),
                    Surface = Color.White, Border = H(0xE5E7EB), Text = H(0x111827), Muted = H(0x6B7280),
                    Icon = H(0x4338CA), Danger = H(0xDC2626), Accent = H(0x2563EB), AccentHover = H(0x1D4ED8), AccentText = Color.White,
                    Ok = H(0x22C55E), Busy = H(0xF59E0B), Off = H(0xEF4444),
                    C64Frame = H(0x706DEB), C64FrameDrop = H(0xB4B2FF), C64Back = H(0x2E2C9B), C64Text = H(0x9A97F7),
                    LogBack = H(0x111827), LogText = H(0xD1D5DB), LogSent = H(0x93C5FD), LogReply = H(0x86EFAC), LogError = H(0xFCA5A5), LogTime = H(0x6B7280),
                    InputBack = H(0x374151), InputText = Color.White, FieldBack = Color.White, FieldText = H(0x111827),
                    SectionDisk = H(0x4F46E5), SectionFile = H(0x0D9488), SectionImage = H(0xEA580C), SectionCaptionText = Color.White,
                    TabLog = H(0x38BDF8), TabDos = H(0xA3E635), TabVice = H(0xF472B6), TabGames = H(0xC084FC), TabAdapter = H(0xFACC15), TabArchive = H(0x2DD4BF)
                };
            }
        }

        /// <summary>Mescola a verso b: t=0 restituisce a, t=1 restituisce b.</summary>
        public static Color Blend(Color a, Color b, float t)
        {
            return Color.FromArgb(
                (int)Math.Round(a.R + (b.R - a.R) * t),
                (int)Math.Round(a.G + (b.G - a.G) * t),
                (int)Math.Round(a.B + (b.B - a.B) * t));
        }

        public static float Scale = 1f;
        public static int S(int px) { return (int)Math.Round(px * Scale); }

        static Font iconFont;
        static bool iconFontResolved;

        /// <summary>Font di icone di Windows 11/10 (null se non disponibile).</summary>
        public static Font IconFont
        {
            get
            {
                if (!iconFontResolved)
                {
                    iconFontResolved = true;
                    using (InstalledFontCollection fonts = new InstalledFontCollection())
                    {
                        foreach (string name in new[] { "Segoe Fluent Icons", "Segoe MDL2 Assets" })
                            foreach (FontFamily f in fonts.Families)
                                if (f.Name == name) { iconFont = new Font(name, 11f); return iconFont; }
                    }
                }
                return iconFont;
            }
        }

        public static GraphicsPath Rounded(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            int d = radius * 2;
            if (d <= 0 || r.Width < d || r.Height < d) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        /// <summary>
        /// Il logo dell'app come immagine (dal PNG incluso: le icone con immagini PNG
        /// non si convertono correttamente con Icon.ToBitmap).
        /// </summary>
        public static Image AppLogo()
        {
            // il PNG si decodifica una volta sola; ogni chiamata riceve una copia sua (chi la usa può eliminarla)
            if (appLogo == null)
            {
                try
                {
                    using (Stream s = typeof(Theme).Assembly.GetManifestResourceStream("app-icona.png"))
                        if (s != null) using (Image img = Image.FromStream(s)) appLogo = new Bitmap(img);
                }
                catch { }
                if (appLogo == null) appLogo = MakeAppIcon().ToBitmap();
            }
            return new Bitmap(appLogo);
        }

        static Bitmap appLogo;

        /// <summary>Il logo rimpicciolito con cura (a passi, per restare nitido nelle misure piccole).</summary>
        public static Bitmap AppLogo(int size)
        {
            using (Image src = AppLogo())
            {
                Image cur = src;
                Bitmap step = null;
                // dimezza finché serve, poi l'ultimo passo alla misura giusta
                while (cur.Width / 2 >= size * 2)
                {
                    Bitmap half = Resample(cur, cur.Width / 2);
                    if (step != null) step.Dispose();
                    step = half; cur = half;
                }
                Bitmap res = Resample(cur, size);
                if (step != null) step.Dispose();
                return res;
            }
        }

        static Bitmap Resample(Image src, int size)
        {
            Bitmap b = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(b))
            using (System.Drawing.Imaging.ImageAttributes ia = new System.Drawing.Imaging.ImageAttributes())
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                ia.SetWrapMode(WrapMode.TileFlipXY);
                g.DrawImage(src, new Rectangle(0, 0, size, size), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, ia);
            }
            return b;
        }

        static System.Drawing.Icon appIcon;

        /// <summary>L'icona dell'app (letta una volta sola e condivisa da tutte le finestre).</summary>
        public static System.Drawing.Icon MakeAppIcon()
        {
            if (appIcon != null) return appIcon;
            try
            {
                using (Stream s = typeof(Theme).Assembly.GetManifestResourceStream("app.ico"))
                    if (s != null) return appIcon = new System.Drawing.Icon(s);
            }
            catch { }
            Color back = Color.FromArgb(0x2E, 0x2C, 0x9B), fore = Color.FromArgb(0x9A, 0x97, 0xF7);
            Bitmap b = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (GraphicsPath p = Rounded(new Rectangle(1, 1, 29, 29), 6))
                using (SolidBrush br = new SolidBrush(back)) g.FillPath(br, p);
                using (SolidBrush br = new SolidBrush(fore)) g.FillRectangle(br, 8, 6, 16, 20);
                using (SolidBrush br = new SolidBrush(back))
                {
                    g.FillEllipse(br, 13, 13, 6, 6);
                    g.FillRectangle(br, 15, 21, 2, 5);
                }
            }
            return System.Drawing.Icon.FromHandle(b.GetHicon());
        }
    }

    /// <summary>Un carattere disponibile per lo schermo della directory.</summary>
    class ScreenFontInfo
    {
        public string Id, Name, Family, Resource, Style;
        public float Px;
        public int Spacing;
    }

    /// <summary>
    /// Caratteri per lo schermo della directory. Quelli inclusi nell'eseguibile hanno licenza
    /// SIL Open Font License (vedi fonts\OFL-*.txt); Consolas e Cascadia Mono sono di Windows.
    /// </summary>
    static class ScreenFonts
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        static extern IntPtr AddFontMemResourceEx(IntPtr data, uint len, IntPtr reserved, ref uint fonts);

        public static readonly ScreenFontInfo[] All =
        {
            new ScreenFontInfo { Id = "sixtyfour", Name = "Sixtyfour", Family = "Sixtyfour Overlapping", Resource = "Sixtyfour.ttf", Px = 12, Spacing = 8, Style = Lang.T("Anni '80 · ispirato al carattere del Commodore 64") },
            new ScreenFontInfo { Id = "sixtyfour-crt", Name = "Sixtyfour CRT", Family = "Sixtyfour Evenly", Resource = "Sixtyfour.ttf", Px = 13, Spacing = 9, Style = Lang.T("Anni '80 · come sopra, con le righe di un monitor a tubo") },
            new ScreenFontInfo { Id = "press", Name = "Press Start 2P", Family = "Press Start 2P", Resource = "PressStart2P-Regular.ttf", Px = 12, Spacing = 9, Style = Lang.T("Anni '80 · 8 bit da sala giochi") },
            new ScreenFontInfo { Id = "vt323", Name = "VT323", Family = "VT323", Resource = "VT323.ttf", Px = 20, Spacing = 4, Style = Lang.T("Anni '80 · terminale DEC VT320") },
            new ScreenFontInfo { Id = "sharetech", Name = "Share Tech Mono", Family = "Share Tech Mono", Resource = "ShareTechMono.ttf", Px = 16, Spacing = 6, Style = Lang.T("Fantascienza · stile console tecnica") },
            new ScreenFontInfo { Id = "jetbrains", Name = "JetBrains Mono", Family = "JetBrains Mono", Resource = "JetBrainsMono.ttf", Px = 15, Spacing = 6, Style = Lang.T("Programmatori · molto leggibile") },
            new ScreenFontInfo { Id = "fira", Name = "Fira Code", Family = "Fira Code", Resource = "FiraCode.ttf", Px = 15, Spacing = 6, Style = Lang.T("Programmatori · il preferito di molti nerd") },
            new ScreenFontInfo { Id = "plex", Name = "IBM Plex Mono", Family = "IBM Plex Mono", Resource = "IBMPlexMono.ttf", Px = 15, Spacing = 6, Style = Lang.T("Programmatori · eredità IBM") },
            new ScreenFontInfo { Id = "cascadia", Name = "Cascadia Mono", Family = "Cascadia Mono", Resource = null, Px = 15, Spacing = 6, Style = Lang.T("Programmatori · quello del Terminale di Windows") },
            new ScreenFontInfo { Id = "consolas", Name = "Consolas", Family = "Consolas", Resource = null, Px = 16, Spacing = 3, Style = Lang.T("Classico · il carattere di sistema") },
        };

        static PrivateFontCollection collection;
        static bool loaded;
        static readonly List<IntPtr> memory = new List<IntPtr>();

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            collection = new PrivateFontCollection();
            HashSet<string> done = new HashSet<string>();
            foreach (ScreenFontInfo f in All)
            {
                if (f.Resource == null || !done.Add(f.Resource)) continue;
                try
                {
                    using (Stream s = typeof(ScreenFonts).Assembly.GetManifestResourceStream(f.Resource))
                    {
                        if (s == null) continue;
                        byte[] data = new byte[s.Length];
                        s.Read(data, 0, data.Length);
                        IntPtr p = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(data.Length);
                        System.Runtime.InteropServices.Marshal.Copy(data, 0, p, data.Length);
                        memory.Add(p);
                        // solo GDI+: registrandolo anche in GDI, Windows userebbe la variante predefinita
                        // dei caratteri variabili (per Sixtyfour quella con le righe CRT)
                        collection.AddMemoryFont(p, data.Length);
                    }
                }
                catch { }
            }
        }

        static readonly Dictionary<string, FontFamily> families = new Dictionary<string, FontFamily>();

        /// <summary>Cerca una famiglia (prima tra quelle incluse, poi tra quelle di Windows); il risultato resta in memoria.</summary>
        static FontFamily FindFamily(string name)
        {
            FontFamily found;
            if (families.TryGetValue(name, out found)) return found;
            Load();
            foreach (FontFamily f in collection.Families) if (f.Name == name) { found = f; break; }
            if (found == null)
            {
                try { found = new FontFamily(name); } catch { found = null; }
            }
            families[name] = found;
            return found;
        }

        public static bool Available(ScreenFontInfo f) { return FindFamily(f.Family) != null; }

        public static ScreenFontInfo Current
        {
            get
            {
                string id = Settings.Get("screenfont", "sixtyfour");
                if (id == "retro") id = "sixtyfour"; // impostazione delle versioni precedenti
                foreach (ScreenFontInfo f in All) if (f.Id == id && Available(f)) return f;
                return All[All.Length - 1];
            }
        }

        public static Font Create(ScreenFontInfo f)
        {
            FontFamily fam = FindFamily(f.Family) ?? new FontFamily("Consolas");
            return new Font(fam, Math.Max(10, (int)Math.Round(f.Px * Theme.Scale)), FontStyle.Regular, GraphicsUnit.Pixel);
        }

        /// <summary>Carattere scelto per lo schermo della directory.</summary>
        public static Font Screen() { return Create(Current); }

        static readonly StringFormat Format = MakeFormat();

        static StringFormat MakeFormat()
        {
            StringFormat f = (StringFormat)StringFormat.GenericTypographic.Clone();
            f.FormatFlags |= StringFormatFlags.NoWrap | StringFormatFlags.NoClip | StringFormatFlags.MeasureTrailingSpaces;
            f.LineAlignment = StringAlignment.Center;
            f.Trimming = StringTrimming.None;
            return f;
        }

        /// <summary>
        /// Disegna testo con GDI+: a differenza di GDI gestisce le varianti dei caratteri variabili
        /// (es. "Sixtyfour Close" senza le righe CRT).
        /// </summary>
        public static void Draw(Graphics g, string text, Font font, Color color, Rectangle r)
        {
            System.Drawing.Text.TextRenderingHint old = g.TextRenderingHint;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            using (SolidBrush b = new SolidBrush(color))
                g.DrawString(text, font, b, r, Format);
            g.TextRenderingHint = old;
        }

        public static SizeF Measure(Graphics g, string text, Font font)
        {
            return g.MeasureString(text, font, int.MaxValue, Format);
        }
    }

    /// <summary>Ribbon = barra a nastro: icona sopra, didascalia piccola (facoltativa) sotto.</summary>
    enum ButtonKind { Toolbar, Header, Accent, Ribbon }

    /// <summary>Pulsante piatto con icona (glifo) e testo.</summary>
    class IconButton : Button
    {
        public string Glyph;
        public ButtonKind Kind;
        public Color IconColor = Color.Empty;   // vuoto = Theme.Icon
        public Color HoverColor = Color.Empty;  // vuoto = derivato dal tema
        public Color PressedColor = Color.Empty;
        public string LongText;                 // testo completo (la barra a nastro toglie le didascalie quando stringe)
        public bool BoltBadge;                  // fulmine sovrapposto all'icona (copia veloce)
        public Color BadgeColor = Color.FromArgb(0xF5, 0xB8, 0x00);
        // barra a nastro: fascia col nome del comando dentro il pulsante (tutto il tassello è cliccabile)
        public Color BandColor = Color.Empty, BodyColor = Color.Empty;
        string header;
        public string Header
        {
            get { return header; }
            set { header = value; if (AutoSize) Size = GetPreferredSize(Size.Empty); Invalidate(); }
        }
        public static int BandHeight { get { return Theme.S(20); } }
        public static int RibbonBodyHeight { get { return Theme.S(40); } }
        static Font bandFont;
        public static Font BandFont { get { if (bandFont == null) bandFont = new Font("Segoe UI", 8.5f); return bandFont; } }
        bool hover, down, compact;
        static ToolTip tips;

        /// <summary>Suggerimenti condivisi (pulsanti compatti, LED di stato…).</summary>
        public static ToolTip Tips
        {
            get { if (tips == null) tips = new ToolTip { InitialDelay = 300, ReshowDelay = 100 }; return tips; }
        }

        /// <summary>Solo icona (quadrato), con il testo come suggerimento al passaggio del mouse.</summary>
        public bool Compact
        {
            get { return compact; }
            set
            {
                if (compact == value) return;
                compact = value;
                Tips.SetToolTip(this, compact ? Text : null);
                if (AutoSize) Size = GetPreferredSize(Size.Empty);
                Invalidate();
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            if (compact) Tips.SetToolTip(this, Text);
        }

        public IconButton(string glyph, string text, ButtonKind kind)
        {
            Glyph = glyph;
            Text = text;
            Kind = kind;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 9.5f, kind == ButtonKind.Accent ? FontStyle.Bold : FontStyle.Regular);
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Margin = new Padding(Theme.S(2));
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        bool HasIcon { get { return !string.IsNullOrEmpty(Glyph) && Theme.IconFont != null; } }

        static Font ribbonFont;
        static Font RibbonFont { get { if (ribbonFont == null) ribbonFont = new Font("Segoe UI", 7.5f); return ribbonFont; } }

        public override Size GetPreferredSize(Size proposed)
        {
            if (Kind == ButtonKind.Ribbon)
            {
                // icona sopra, didascalia sotto (se c'è); stessa altezza per tutti i tasselli, la fascia del nome (se c'è) sta sopra
                int rw = Theme.S(44);
                if (!string.IsNullOrEmpty(Text)) rw = Math.Max(rw, TextRenderer.MeasureText(Text, RibbonFont, Size.Empty, TextFormatFlags.NoPadding).Width + Theme.S(14));
                if (!string.IsNullOrEmpty(header)) rw = Math.Max(rw, TextRenderer.MeasureText(header, BandFont, Size.Empty, TextFormatFlags.NoPadding).Width + Theme.S(32));
                return new Size(rw, (string.IsNullOrEmpty(header) ? 0 : BandHeight) + RibbonBodyHeight);
            }
            Size t = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPadding);
            if (compact && HasIcon)
            {
                int side = Math.Max(Theme.S(36), t.Height + Theme.S(14));
                return new Size(side, side);
            }
            int w = Theme.S(10) * 2 + t.Width;
            if (HasIcon) w += Theme.S(16) + Theme.S(7);
            return new Size(w, Math.Max(Theme.S(34), t.Height + Theme.S(14)));
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color parentBack = Parent != null ? Parent.BackColor : Theme.Surface;
            g.Clear(parentBack);

            Color back, fore, icon;
            switch (Kind)
            {
                case ButtonKind.Header:
                    back = down || hover ? Theme.HeaderButtonHover : Theme.HeaderButton;
                    fore = Theme.HeaderText; icon = Theme.HeaderText;
                    break;
                case ButtonKind.Accent:
                    back = down || hover ? Theme.AccentHover : Theme.Accent;
                    fore = Theme.AccentText; icon = Theme.AccentText;
                    break;
                default:
                    Color hov = HoverColor.IsEmpty ? Theme.Blend(parentBack, Theme.Icon, 0.12f) : HoverColor;
                    Color prs = PressedColor.IsEmpty ? Theme.Blend(parentBack, Theme.Icon, 0.22f) : PressedColor;
                    back = down ? prs : hover ? hov : parentBack;
                    fore = Theme.Text; icon = IconColor.IsEmpty ? Theme.Icon : IconColor;
                    break;
            }
            if (!Enabled)
            {
                if (Kind == ButtonKind.Toolbar || Kind == ButtonKind.Ribbon)
                {
                    fore = Theme.Blend(Theme.Text, parentBack, 0.6f);
                    icon = fore;
                }
                else
                {
                    back = Theme.Blend(back, parentBack, 0.5f);
                    fore = Theme.Blend(fore, back, 0.5f);
                    icon = fore;
                }
            }

            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            if (Kind == ButtonKind.Ribbon)
            {
                // tassello squadrato: corpo nel colore tenue della sezione (più scuro al passaggio del mouse)
                if (!BodyColor.IsEmpty && !down && !hover) back = BodyColor;
                using (SolidBrush b = new SolidBrush(back)) g.FillRectangle(b, 0, 0, Width, Height);
                if (!string.IsNullOrEmpty(header))
                {
                    Color band = BandColor.IsEmpty ? back : BandColor;
                    if (hover || down) band = Theme.Blend(band, Theme.Text, down ? 0.16f : 0.08f);
                    using (SolidBrush b = new SolidBrush(band)) g.FillRectangle(b, 0, 0, Width, BandHeight);
                    TextRenderer.DrawText(g, header, BandFont, new Rectangle(0, 0, Width, BandHeight), Enabled ? Theme.Text : fore,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                }
            }
            else
                using (GraphicsPath p = Theme.Rounded(r, Theme.S(6)))
                using (SolidBrush b = new SolidBrush(back))
                    g.FillPath(b, p);

            int x = Theme.S(10);
            Rectangle iconRect = Rectangle.Empty;
            if (Kind == ButtonKind.Ribbon)
            {
                bool caption = !string.IsNullOrEmpty(Text);
                // icona (e didascalia, se c'è) centrate in verticale nel corpo del tassello
                int bodyTop = string.IsNullOrEmpty(header) ? 0 : BandHeight;
                int block = Theme.S(18) + (caption ? Theme.S(13) : 0);
                int top = bodyTop + (Height - bodyTop - block) / 2;
                iconRect = new Rectangle((Width - Theme.S(16)) / 2, top + Theme.S(1), Theme.S(16), Theme.S(16));
                if (HasIcon)
                    TextRenderer.DrawText(g, Glyph, Theme.IconFont, new Rectangle(0, top, Width, Theme.S(18)), icon,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                if (caption)
                    TextRenderer.DrawText(g, Text, RibbonFont, new Rectangle(0, top + Theme.S(19), Width, Height - top - Theme.S(20)), fore,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.NoPadding);
            }
            else if (compact && HasIcon)
            {
                TextRenderer.DrawText(g, Glyph, Theme.IconFont, new Rectangle(0, 0, Width, Height), icon,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                iconRect = new Rectangle((Width - Theme.S(16)) / 2, (Height - Theme.S(16)) / 2, Theme.S(16), Theme.S(16));
            }
            else if (HasIcon)
            {
                Rectangle ir = new Rectangle(x, 0, Theme.S(16), Height);
                TextRenderer.DrawText(g, Glyph, Theme.IconFont, ir, icon, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                iconRect = new Rectangle(x, (Height - Theme.S(16)) / 2, Theme.S(16), Theme.S(16));
                x += Theme.S(16) + Theme.S(7);
            }
            if (BoltBadge && HasIcon)
            {
                // fulmine pieno in alto a destra dell'icona (copia veloce), con un bordino del colore di fondo
                float s = Theme.S(13);
                float bx = iconRect.Right - s * 0.55f, by = iconRect.Top - s * 0.35f;
                PointF[] bolt =
                {
                    new PointF(bx + s * 0.62f, by), new PointF(bx + s * 0.12f, by + s * 0.58f), new PointF(bx + s * 0.44f, by + s * 0.58f),
                    new PointF(bx + s * 0.30f, by + s), new PointF(bx + s * 0.88f, by + s * 0.38f), new PointF(bx + s * 0.55f, by + s * 0.38f),
                    new PointF(bx + s * 0.74f, by)
                };
                using (Pen edge = new Pen(back, Math.Max(2f, Theme.S(2))) { LineJoin = LineJoin.Round }) g.DrawPolygon(edge, bolt);
                using (SolidBrush fill = new SolidBrush(Enabled ? BadgeColor : icon)) g.FillPolygon(fill, bolt);
            }
            if (!(compact && HasIcon) && Kind != ButtonKind.Ribbon)
            {
                Rectangle tr = new Rectangle(x, 0, Width - x - Theme.S(6), Height);
                TextRenderer.DrawText(g, Text, Font, tr, fore, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }

            if (Focused && ShowFocusCues)
            {
                using (Pen pen = new Pen(Kind == ButtonKind.Toolbar ? icon : fore))
                using (GraphicsPath p = Theme.Rounded(new Rectangle(1, 1, Width - 3, Height - 3), Theme.S(5)))
                {
                    pen.DashStyle = DashStyle.Dot;
                    g.DrawPath(pen, p);
                }
            }
        }
    }

    /// <summary>LED tondo con alone.</summary>
    class Led : Control
    {
        Color color = Color.Red;
        public Color Color { get { return color; } set { color = value; Invalidate(); } }

        public Led()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(Theme.S(16), Theme.S(16));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Header);
            using (SolidBrush glow = new SolidBrush(Color.FromArgb(70, color)))
                g.FillEllipse(glow, 0, 0, Width - 1, Height - 1);
            int pad = Width / 4;
            using (SolidBrush b = new SolidBrush(color))
                g.FillEllipse(b, pad, pad, Width - 2 * pad - 1, Height - 2 * pad - 1);
        }
    }

    /// <summary>
    /// Barra di avanzamento a blocchi in stile PETSCII: celle quadrate piene, la cella in corso
    /// riempita a quarti (come i caratteri a blocchi del C64), quelle vuote a scacchiera.
    /// </summary>
    class SlimProgress : Control
    {
        int max = 100, val;
        public int Maximum { get { return max; } set { max = Math.Max(1, value); Invalidate(); } }
        public int Value { get { return val; } set { val = Math.Max(0, Math.Min(value, max)); Invalidate(); } }

        public SlimProgress()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Color back = Parent != null ? Parent.BackColor : Theme.Surface;
            g.Clear(back);
            g.SmoothingMode = SmoothingMode.None;
            int cell = Math.Max(4, Math.Min(Height, Theme.S(10)));
            int gap = Math.Max(1, Theme.S(2));
            int n = Math.Max(1, (Width + gap) / (cell + gap));
            int y = (Height - cell) / 2;
            // avanzamento in quarti di cella
            int quarters = (int)((long)n * 4 * val / max);
            int dot = Math.Max(1, cell / 5);   // lato dei quadratini della scacchiera
            using (SolidBrush fill = new SolidBrush(Theme.Accent))
            using (SolidBrush empty = new SolidBrush(Theme.Blend(back, Theme.Border, 0.9f)))
            using (SolidBrush shade = new SolidBrush(Theme.Blend(back, Theme.Accent, 0.22f)))
            {
                for (int i = 0; i < n; i++)
                {
                    int x = i * (cell + gap);
                    int q = Math.Max(0, Math.Min(4, quarters - i * 4));
                    if (q == 4) { g.FillRectangle(fill, x, y, cell, cell); continue; }
                    if (q > 0)
                    {
                        // cella in corso: fondo tenue + parte sinistra piena (1/4, 1/2, 3/4)
                        g.FillRectangle(shade, x, y, cell, cell);
                        g.FillRectangle(fill, x, y, cell * q / 4, cell);
                        continue;
                    }
                    // cella vuota: scacchiera come il carattere PETSCII a retino
                    for (int yy = 0; yy < cell; yy += dot)
                        for (int xx = ((yy / dot) % 2) * dot; xx < cell; xx += dot * 2)
                            g.FillRectangle(empty, x + xx, y + yy, Math.Min(dot, cell - xx), Math.Min(dot, cell - yy));
                }
            }
        }
    }

    /// <summary>Selettore a segmenti ("pillole"), es. numero del drive 8 · 9 · 10 · 11.</summary>
    class Segmented : Control
    {
        readonly string[] items;
        int selected, hover = -1;
        public event EventHandler SelectedIndexChanged;

        public int SelectedIndex
        {
            get { return selected; }
            set
            {
                int v = Math.Max(0, Math.Min(value, items.Length - 1));
                if (v == selected) return;
                selected = v; Invalidate();
                if (SelectedIndexChanged != null) SelectedIndexChanged(this, EventArgs.Empty);
            }
        }

        readonly int segWidth;
        int SegW { get { return segWidth > 0 ? segWidth : Theme.S(34); } }

        public Segmented(params string[] items) : this(0, items) { }

        /// <summary>Con segWidth &gt; 0 i segmenti hanno quella larghezza (per etichette di testo).</summary>
        public Segmented(int segWidth, params string[] items)
        {
            this.segWidth = segWidth;
            this.items = items;
            Font = new Font("Segoe UI Semibold", 9.5f);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
            Size = new Size(SegW * items.Length + Theme.S(6), Theme.S(34));
            TabStop = true;
        }

        int IndexAt(Point p)
        {
            int i = (p.X - Theme.S(3)) / SegW;
            return i >= 0 && i < items.Length ? i : -1;
        }

        protected override void OnMouseMove(MouseEventArgs e) { int h = IndexAt(e.Location); if (h != hover) { hover = h; Invalidate(); } base.OnMouseMove(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = -1; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { Focus(); int i = IndexAt(e.Location); if (i >= 0 && Enabled) SelectedIndex = i; base.OnMouseDown(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override bool IsInputKey(Keys k) { return k == Keys.Left || k == Keys.Right || base.IsInputKey(k); }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left) SelectedIndex = selected - 1;
            else if (e.KeyCode == Keys.Right) SelectedIndex = selected + 1;
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color parentBack = Parent != null ? Parent.BackColor : Theme.Header;
            g.Clear(parentBack);
            Color track = Enabled ? Theme.HeaderButton : Theme.Blend(Theme.HeaderButton, parentBack, 0.5f);
            using (GraphicsPath p = Theme.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), Theme.S(8)))
            using (SolidBrush b = new SolidBrush(track)) g.FillPath(b, p);

            for (int i = 0; i < items.Length; i++)
            {
                Rectangle r = new Rectangle(Theme.S(3) + i * SegW, Theme.S(3), SegW, Height - Theme.S(6) - 1);
                Color fore = Theme.HeaderMuted;
                if (i == selected)
                {
                    Color fill = Enabled ? Theme.Accent : Theme.Blend(Theme.Accent, track, 0.5f);
                    using (GraphicsPath p = Theme.Rounded(r, Theme.S(6)))
                    using (SolidBrush b = new SolidBrush(fill)) g.FillPath(b, p);
                    fore = Theme.AccentText;
                }
                else if (i == hover && Enabled)
                {
                    using (GraphicsPath p = Theme.Rounded(r, Theme.S(6)))
                    using (SolidBrush b = new SolidBrush(Theme.HeaderButtonHover)) g.FillPath(b, p);
                    fore = Theme.HeaderText;
                }
                if (!Enabled) fore = Theme.Blend(fore, track, 0.4f);
                TextRenderer.DrawText(g, items[i], Font, r, fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
            if (Focused && ShowFocusCues)
                using (Pen pen = new Pen(Theme.HeaderText) { DashStyle = DashStyle.Dot })
                using (GraphicsPath p = Theme.Rounded(new Rectangle(1, 1, Width - 3, Height - 3), Theme.S(7)))
                    g.DrawPath(pen, p);
        }
    }

    /// <summary>Scheda colorata: piena del suo colore quando è attiva, tenue quando non lo è.</summary>
    class TabButton : Control
    {
        bool selected, hover;
        public readonly Color Accent;
        public bool Selected { get { return selected; } set { selected = value; Invalidate(); } }

        readonly string glyph;

        public TabButton(string text, Color accent) : this(text, accent, null) { }

        public TabButton(string text, Color accent, string glyph)
        {
            Text = text;
            Accent = accent;
            this.glyph = Theme.IconFont != null ? glyph : null;
            Font = new Font("Segoe UI Semibold", 9.5f);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Size t = TextRenderer.MeasureText(text, Font, Size.Empty, TextFormatFlags.NoPadding);
            Size = new Size(t.Width + Theme.S(32) + (this.glyph != null ? Theme.S(22) : 0), Theme.S(32));
            Margin = new Padding(0, Theme.S(6), Theme.S(6), 0);
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color parentBack = Parent != null ? Parent.BackColor : Theme.LogBack;
            g.Clear(parentBack);
            Color back = selected ? Accent : Theme.Blend(Accent, parentBack, hover ? 0.70f : 0.86f);
            Color fore = selected ? Theme.LogBack : Accent;
            // angoli arrotondati solo in alto: la scheda "poggia" sulla linea sottostante
            using (GraphicsPath p = Theme.Rounded(new Rectangle(0, 0, Width - 1, Height + Theme.S(8)), Theme.S(7)))
            using (SolidBrush b = new SolidBrush(back))
                g.FillPath(b, p);
            int x = 0;
            if (glyph != null)
            {
                TextRenderer.DrawText(g, glyph, Theme.IconFont, new Rectangle(Theme.S(14), 0, Theme.S(18), Height), fore,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                x = Theme.S(22);
            }
            TextRenderer.DrawText(g, Text, Font, new Rectangle(x, 0, Width - x, Height), fore,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    /// <summary>Colori dei menu a tendina secondo il tema.</summary>
    class ThemedColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Theme.Surface; } }
        public override Color MenuBorder { get { return Theme.Border; } }
        public override Color MenuItemBorder { get { return Theme.Blend(Theme.Surface, Theme.Accent, 0.4f); } }
        public override Color MenuItemSelected { get { return Theme.Blend(Theme.Surface, Theme.Accent, 0.18f); } }
        public override Color MenuItemSelectedGradientBegin { get { return MenuItemSelected; } }
        public override Color MenuItemSelectedGradientEnd { get { return MenuItemSelected; } }
        public override Color ImageMarginGradientBegin { get { return Theme.Surface; } }
        public override Color ImageMarginGradientMiddle { get { return Theme.Surface; } }
        public override Color ImageMarginGradientEnd { get { return Theme.Surface; } }
        public override Color CheckBackground { get { return Theme.Blend(Theme.Surface, Theme.Accent, 0.25f); } }
        public override Color CheckSelectedBackground { get { return Theme.Blend(Theme.Surface, Theme.Accent, 0.35f); } }
        public override Color CheckPressedBackground { get { return CheckSelectedBackground; } }
        public override Color SeparatorDark { get { return Theme.Border; } }
        public override Color SeparatorLight { get { return Theme.Border; } }
    }

    class ThemedRenderer : ToolStripProfessionalRenderer
    {
        static readonly Font CheckFont = new Font("Segoe UI", 9f, FontStyle.Bold);

        public ThemedRenderer() : base(new ThemedColors()) { RoundedEdges = false; }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? Theme.Text : Theme.Muted;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            Rectangle r = new Rectangle(e.ImageRectangle.X - 2, e.ImageRectangle.Y - 2, e.ImageRectangle.Width + 4, e.ImageRectangle.Height + 4);
            using (SolidBrush b = new SolidBrush(Theme.Blend(Theme.Surface, Theme.Accent, 0.25f))) e.Graphics.FillRectangle(b, r);
            TextRenderer.DrawText(e.Graphics, "✓", CheckFont, r, Theme.Accent,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}
