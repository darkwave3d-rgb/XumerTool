// Lingue dell'interfaccia. Il codice è scritto in italiano; ogni testo passa da Lang.T("...")
// che lo cerca nel file della lingua scelta (lang\en.txt, incluso nell'eseguibile).
// Se una traduzione manca resta il testo italiano.
//
// Formato del file: coppie di righe
//   it: testo italiano
//   en: testo tradotto
// Le righe vuote e quelle che iniziano con # sono ignorate. Sequenze: \n (a capo), \t, \\.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace XumerTool
{
    static class Lang
    {
        /// <summary>Lingue disponibili: codice e nome nella lingua stessa.</summary>
        public static readonly string[] Codes = { "it", "en" };
        public static readonly string[] Names = { "Italiano", "English" };

        public static string Current = "it";
        static Dictionary<string, string> map = new Dictionary<string, string>();

        /// <summary>Lingua di Windows: italiano se Windows è in italiano, altrimenti inglese.</summary>
        public static string SystemDefault()
        {
            try { return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "it" ? "it" : "en"; }
            catch { return "it"; }
        }

        public static void Load(string code)
        {
            map = new Dictionary<string, string>();
            Current = Array.IndexOf(Codes, code) >= 0 ? code : "it";
            if (Current == "it") return;
            try
            {
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("lang." + Current + ".txt"))
                {
                    if (s == null) return;
                    using (StreamReader r = new StreamReader(s, Encoding.UTF8))
                    {
                        string key = null, line;
                        while ((line = r.ReadLine()) != null)
                        {
                            if (line.StartsWith("it: ")) key = Unescape(line.Substring(4));
                            else if (line.StartsWith("it:") && line.Length == 3) key = "";
                            else if (key != null && line.StartsWith(Current + ": "))
                            {
                                map[key] = Unescape(line.Substring(Current.Length + 2));
                                key = null;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        static string Unescape(string s)
        {
            if (s.IndexOf('\\') < 0) return s;
            StringBuilder b = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    char n = s[++i];
                    b.Append(n == 'n' ? '\n' : n == 't' ? '\t' : n == 'r' ? '\r' : n);
                }
                else b.Append(c);
            }
            return b.ToString();
        }

        /// <summary>Traduce un testo italiano nella lingua corrente.</summary>
        public static string T(string it)
        {
            if (it == null) return null;
            string t;
            return map.TryGetValue(it, out t) ? t : it;
        }

        /// <summary>Traduce e formatta (come string.Format).</summary>
        public static string F(string it, params object[] args)
        {
            return string.Format(T(it), args);
        }
    }
}
