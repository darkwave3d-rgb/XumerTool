// Strumento per le traduzioni di XUMER TOOL.
//   LangTool.exe scan  file.cs ...          elenca i testi che verrebbero tradotti (nessuna modifica)
//   LangTool.exe wrap  file.cs ...          racchiude quei testi in Lang.T("...")
//   LangTool.exe keys  lang\en.txt file.cs ... elenca i testi Lang.T/Lang.F che mancano nel file della lingua
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

static class LangTool
{
    // testi che non sono interfaccia: nomi di temi e caratteri, chiavi, nomi di file
    static readonly HashSet<string> Skip = new HashSet<string>
    {
        "Chiaro", "Scuro", "Commodore", "Rosé Pine", "Tokyo Night", "Catppuccin", "Everforest", "Gruvbox", "Nord", "Lunar Tear",
        "Segoe UI", "Segoe UI Semibold", "Segoe UI Symbol", "Segoe UI Emoji", "Segoe Fluent Icons", "Segoe MDL2 Assets", "Consolas", "Courier New",
        "Sixtyfour", "Press Start 2P", "VT323", "Share Tech Mono", "JetBrains Mono", "Fira Code", "IBM Plex Mono", "Sixtyfour CRT",
        "DarkMode_Explorer", "Explorer", "XUMER TOOL", "XumerTool", "XUMERTool", "Italiano", "English", "Microsoft Sans Serif",
        // nomi di cartelle e valori di sistema, comandi del drive
        "Service", "Downloads", "Programs", "Publisher", "Description", "Save", "B-P 2 0", "XUM1571 Tool.lnk",
        "Sixtyfour Overlapping", "Sixtyfour Evenly", "Cascadia Mono",
    };

    static readonly Regex Capitalized = new Regex(@"^[A-ZÀ-Ý][a-zà-ÿ']+[.!?:…]*$");

    static bool IsUiText(string s)
    {
        if (Skip.Contains(s)) return false;
        int letters = 0;
        foreach (char c in s) if (char.IsLetter(c)) letters++;
        if (letters < 2) return false;
        if (s.StartsWith("-") || s.StartsWith("/") || s.StartsWith("\" ")) return false;   // parametri della riga di comando
        if (s.StartsWith("http") || s.Contains(":\\") || s.Contains("\\\\") || s.StartsWith("payload/") || s.StartsWith("lang.")) return false;
        if (Regex.IsMatch(s, @"^[\w\-]+\.(png|ico|txt|hex|exe|ini|ttf|a65|lnk|d64|d71|d81|prg|seq|p00|zip|cs|xml)$", RegexOptions.IgnoreCase)) return false;
        if (s.IndexOf(' ') >= 0 || s.IndexOf('…') >= 0 || s.IndexOf('\n') >= 0) return true;
        foreach (char c in s) if ("àèéìòùÀÈÉÌÒÙ".IndexOf(c) >= 0) return true;
        return Capitalized.IsMatch(s);
    }

    static string Unescape(string body)
    {
        StringBuilder b = new StringBuilder();
        for (int i = 0; i < body.Length; i++)
        {
            char c = body[i];
            if (c != '\\' || i + 1 >= body.Length) { b.Append(c); continue; }
            char n = body[++i];
            switch (n)
            {
                case 'n': b.Append('\n'); break;
                case 't': b.Append('\t'); break;
                case 'r': b.Append('\r'); break;
                case '0': b.Append('\0'); break;
                case 'u': b.Append((char)Convert.ToInt32(body.Substring(i + 1, 4), 16)); i += 4; break;
                default: b.Append(n); break;
            }
        }
        return b.ToString();
    }

    class Lit { public int Start, End; public string Value; public int Line; }

    /// <summary>Trova le stringhe normali ("...") nel codice, saltando commenti, caratteri e stringhe @"...".</summary>
    static List<Lit> Literals(string src)
    {
        List<Lit> res = new List<Lit>();
        int line = 1;
        for (int i = 0; i < src.Length; i++)
        {
            char c = src[i];
            if (c == '\n') { line++; continue; }
            if (c == '/' && i + 1 < src.Length && src[i + 1] == '/') { while (i < src.Length && src[i] != '\n') i++; i--; continue; }
            if (c == '/' && i + 1 < src.Length && src[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < src.Length && !(src[i] == '*' && src[i + 1] == '/')) { if (src[i] == '\n') line++; i++; }
                i++; continue;
            }
            if (c == '\'')
            {
                i++;
                while (i < src.Length && src[i] != '\'') { if (src[i] == '\\') i++; i++; }
                continue;
            }
            if (c == '@' && i + 1 < src.Length && src[i + 1] == '"')
            {
                i += 2;
                while (i < src.Length) { if (src[i] == '"') { if (i + 1 < src.Length && src[i + 1] == '"') { i += 2; continue; } break; } if (src[i] == '\n') line++; i++; }
                continue;
            }
            if (c == '"')
            {
                int start = i; i++;
                while (i < src.Length && src[i] != '"') { if (src[i] == '\\') i++; i++; }
                res.Add(new Lit { Start = start, End = i, Value = Unescape(src.Substring(start + 1, i - start - 1)), Line = line });
            }
        }
        return res;
    }

    static string LineOf(string src, int pos)
    {
        int a = src.LastIndexOf('\n', Math.Max(0, pos - 1)) + 1;
        int b = src.IndexOf('\n', pos); if (b < 0) b = src.Length;
        return src.Substring(a, b - a);
    }

    static bool Context(string src, Lit l)
    {
        string line = LineOf(src, l.Start).Trim();
        if (line.StartsWith("[") || line.StartsWith("#")) return false;
        int lineStart = src.LastIndexOf('\n', Math.Max(0, l.Start - 1)) + 1;
        string before = src.Substring(lineStart, l.Start - lineStart);
        if (before.Contains("const ") || Regex.IsMatch(before, @"\bcase\s")) return false;
        if (before.Contains("Settings.")) return false;
        string tail = before.TrimEnd();
        if (tail.EndsWith("Lang.T(") || tail.EndsWith("Lang.F(") || tail.EndsWith("GetManifestResourceStream(") || tail.EndsWith("new Font(")
            || tail.EndsWith("FontFamily(") || tail.EndsWith("GetProcessesByName(")) return false;
        return true;
    }

    static int Main(string[] args)
    {
        string mode = args[0];
        if (mode == "keys")
        {
            HashSet<string> have = new HashSet<string>();
            foreach (string l in File.ReadAllLines(args[1], Encoding.UTF8))
                if (l.StartsWith("it: ")) have.Add(l.Substring(4));
            HashSet<string> seen = new HashSet<string>();
            for (int k = 2; k < args.Length; k++)
            {
                string src = File.ReadAllText(args[k], Encoding.UTF8);
                foreach (Lit l in Literals(src))
                {
                    int ls = src.LastIndexOf('\n', Math.Max(0, l.Start - 1)) + 1;
                    string before = src.Substring(ls, l.Start - ls).TrimEnd();
                    if (!(before.EndsWith("Lang.T(") || before.EndsWith("Lang.F("))) continue;
                    string esc = l.Value.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\t", "\\t").Replace("\r", "\\r");
                    if (!have.Contains(esc) && seen.Add(esc)) Console.WriteLine("it: " + esc + "\nen: \n");
                }
            }
            return 0;
        }
        int total = 0;
        for (int k = 1; k < args.Length; k++)
        {
            string path = args[k];
            byte[] raw = File.ReadAllBytes(path);
            bool bom = raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF;
            string src = File.ReadAllText(path, Encoding.UTF8);
            List<Lit> lits = Literals(src);
            List<Lit> pick = new List<Lit>();
            foreach (Lit l in lits) if (IsUiText(l.Value) && Context(src, l)) pick.Add(l);
            total += pick.Count;
            if (mode == "scan")
                foreach (Lit l in pick) Console.WriteLine(Path.GetFileName(path) + ":" + l.Line + ": " + l.Value.Replace("\n", "\\n"));
            else if (mode == "wrap")
            {
                StringBuilder b = new StringBuilder(src);
                for (int i = pick.Count - 1; i >= 0; i--)
                {
                    b.Insert(pick[i].End + 1, ")");
                    b.Insert(pick[i].Start, "Lang.T(");
                }
                File.WriteAllText(path, b.ToString(), new UTF8Encoding(bom));
                Console.WriteLine(Path.GetFileName(path) + ": " + pick.Count);
            }
        }
        Console.Error.WriteLine("totale: " + total);
        return 0;
    }
}
