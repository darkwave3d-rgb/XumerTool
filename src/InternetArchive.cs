// Collegamento all'account di Internet Archive (archive.org).
// Il sito dà a ogni account due "chiavi S3" (accesso + segreta) per caricare file dai programmi:
// si ottengono con email e password (servizio xauthn, lo stesso del programma ufficiale "ia")
// oppure si copiano dalla pagina https://archive.org/account/s3.php.
// La password non viene mai salvata; le chiavi restano su questo PC, cifrate con DPAPI
// (leggibili solo dall'utente Windows che le ha salvate).
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace XumerTool
{
    static class InternetArchive
    {
        public const string KeysPage = "https://archive.org/account/s3.php";
        public const string SignupPage = "https://archive.org/account/signup";
        const string LoginUrl = "https://archive.org/services/xauthn/?op=login";
        const string S3Url = "https://s3.us.archive.org/";

        public static string Access, Secret, ScreenName;
        public static bool Connected { get { return !string.IsNullOrEmpty(Access) && !string.IsNullOrEmpty(Secret); } }

        /// <summary>Pagina pubblica dei caricamenti dell'account (se il nome è noto).</summary>
        public static string ProfilePage
        {
            get { return string.IsNullOrEmpty(ScreenName) ? "https://archive.org/" : "https://archive.org/details/@" + Uri.EscapeDataString(ScreenName.ToLowerInvariant()); }
        }

        // ---------- chiavi salvate ----------

        static readonly byte[] Entropy = Encoding.UTF8.GetBytes("XUMER TOOL · Internet Archive");

        static string Protect(string s)
        {
            return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(s), Entropy, DataProtectionScope.CurrentUser));
        }

        static string Unprotect(string b64)
        {
            try { return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(b64), Entropy, DataProtectionScope.CurrentUser)); }
            catch { return null; }   // salvate da un altro utente o rovinate: come se non ci fossero
        }

        public static void Load()
        {
            string a = Settings.Get("ia_access", ""), s = Settings.Get("ia_secret", "");
            Access = a.Length > 0 ? Unprotect(a) : null;
            Secret = s.Length > 0 ? Unprotect(s) : null;
            ScreenName = Settings.Get("ia_name", "");
            if (!Connected) { Access = Secret = null; }
        }

        public static void Save(string access, string secret, string screenName)
        {
            Access = access; Secret = secret; ScreenName = screenName ?? "";
            Settings.Set("ia_access", Protect(access));
            Settings.Set("ia_secret", Protect(secret));
            Settings.Set("ia_name", ScreenName);
        }

        public static void Disconnect()
        {
            Access = Secret = null; ScreenName = "";
            Settings.Set("ia_access", ""); Settings.Set("ia_secret", ""); Settings.Set("ia_name", "");
        }

        // ---------- collegamento ----------

        static void Tls() { ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; }   // TLS 1.2

        static HttpWebRequest Request(string url, string method)
        {
            Tls();
            HttpWebRequest r = (HttpWebRequest)WebRequest.Create(url);
            r.Method = method;
            r.UserAgent = "XUMER TOOL/" + AboutDialog.AppVersion;
            r.Timeout = 20000;
            return r;
        }

        static string ReadBody(WebResponse resp)
        {
            using (Stream s = resp.GetResponseStream())
            using (StreamReader rd = new StreamReader(s, Encoding.UTF8)) return rd.ReadToEnd();
        }

        static string JsonString(string json, string key)
        {
            Match m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"");
            return m.Success ? Regex.Unescape(m.Groups[1].Value) : null;
        }

        /// <summary>
        /// Accesso con email e password: il sito restituisce le chiavi S3 e il nome dell'account.
        /// La password viene usata solo per questa richiesta.
        /// </summary>
        public static void Login(string email, string password, out string access, out string secret, out string screenName)
        {
            HttpWebRequest r = Request(LoginUrl, "POST");
            r.ContentType = "application/x-www-form-urlencoded";
            byte[] body = Encoding.UTF8.GetBytes("email=" + Uri.EscapeDataString(email) + "&password=" + Uri.EscapeDataString(password));
            using (Stream s = r.GetRequestStream()) s.Write(body, 0, body.Length);
            string json;
            try { using (WebResponse resp = r.GetResponse()) json = ReadBody(resp); }
            catch (WebException ex)
            {
                // anche gli errori di accesso arrivano come JSON con il motivo
                if (ex.Response == null) throw new Exception(Lang.T("archive.org non risponde: controlla la connessione a internet.") + " (" + ex.Message + ")");
                using (WebResponse resp = ex.Response) json = ReadBody(resp);
            }
            if (!Regex.IsMatch(json, "\"success\"\\s*:\\s*true"))
            {
                string reason = JsonString(json, "reason") ?? "";
                switch (reason)
                {
                    case "account_not_found": throw new Exception(Lang.T("Nessun account archive.org con questa email."));
                    case "account_bad_password": throw new Exception(Lang.T("Password errata."));
                    case "account_not_verified": throw new Exception(Lang.T("L'account non è ancora verificato: apri l'email di conferma di archive.org e riprova."));
                    case "account_locked": throw new Exception(Lang.T("L'account è bloccato: controlla sul sito archive.org."));
                    default: throw new Exception(Lang.T("Accesso non riuscito") + (reason.Length > 0 ? ": " + reason : "."));
                }
            }
            access = JsonString(json, "access");
            secret = JsonString(json, "secret");
            screenName = JsonString(json, "screenname") ?? "";
            if (string.IsNullOrEmpty(access) || string.IsNullOrEmpty(secret))
                throw new Exception(Lang.T("Il sito non ha restituito le chiavi di caricamento: copiale dalla pagina delle chiavi."));
        }

        /// <summary>
        /// Controlla delle chiavi copiate a mano chiedendo al servizio S3 l'elenco degli elementi dell'account.
        /// Restituisce il nome dell'account se il servizio lo indica (altrimenti stringa vuota).
        /// </summary>
        public static string CheckKeys(string access, string secret)
        {
            HttpWebRequest r = Request(S3Url, "GET");
            r.Headers["authorization"] = "LOW " + access + ":" + secret;
            try
            {
                using (WebResponse resp = r.GetResponse())
                {
                    string xml = ReadBody(resp);
                    Match m = Regex.Match(xml, "<DisplayName>([^<]*)</DisplayName>");
                    return m.Success ? WebUtility.HtmlDecode(m.Groups[1].Value) : "";
                }
            }
            catch (WebException ex)
            {
                HttpWebResponse h = ex.Response as HttpWebResponse;
                if (h != null && (h.StatusCode == HttpStatusCode.Forbidden || h.StatusCode == HttpStatusCode.Unauthorized))
                    throw new Exception(Lang.T("Chiavi non valide: ricopiale dalla pagina delle chiavi di archive.org."));
                throw new Exception(Lang.T("archive.org non risponde: controlla la connessione a internet.") + " (" + ex.Message + ")");
            }
        }
    }
}
