// Associazione dei file .d64/.d71/.d81 a XUMER TOOL in Esplora risorse, solo per l'utente corrente
// (HKCU\Software\Classes: niente permessi di amministratore). Si fa "con educazione":
// XUMER TOOL entra nella lista "Apri con" e diventa il programma predefinito solo se nessun altro
// lo è già; se Windows usa già un altro programma, la scelta resta all'utente (Apri con → Usa sempre).
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace XumerTool
{
    static class FileAssoc
    {
        public static readonly string[] Extensions = { ".d64", ".d71", ".d81" };
        const string ProgId = "XumerTool.DiskImage";
        const string Classes = @"Software\Classes\";

        [DllImport("shell32.dll")]
        static extern void SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);
        const int SHCNE_ASSOCCHANGED = 0x08000000, SHCNF_IDLIST = 0;

        public enum State { None, Here, Elsewhere }

        static string Exe { get { return Application.ExecutablePath; } }

        /// <summary>Associati a questa copia del programma, a un'altra (es. la cartella portatile è stata spostata) o per niente.</summary>
        public static State Get()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(Classes + ProgId + @"\shell\open\command"))
                {
                    if (k == null) return State.None;
                    string cmd = k.GetValue("") as string ?? "";
                    return cmd.IndexOf(Exe, StringComparison.OrdinalIgnoreCase) >= 0 ? State.Here : State.Elsewhere;
                }
            }
            catch { return State.None; }
        }

        public static void Register()
        {
            using (RegistryKey p = Registry.CurrentUser.CreateSubKey(Classes + ProgId))
            {
                p.SetValue("", Lang.T("Immagine disco Commodore"));
                using (RegistryKey icon = p.CreateSubKey("DefaultIcon")) icon.SetValue("", "\"" + Exe + "\",0");
                using (RegistryKey cmd = p.CreateSubKey(@"shell\open\command")) cmd.SetValue("", "\"" + Exe + "\" \"%1\"");
            }
            foreach (string ext in Extensions)
                using (RegistryKey e = Registry.CurrentUser.CreateSubKey(Classes + ext))
                {
                    using (RegistryKey open = e.CreateSubKey("OpenWithProgids")) open.SetValue(ProgId, new byte[0], RegistryValueKind.None);
                    if (string.IsNullOrEmpty(e.GetValue("") as string)) e.SetValue("", ProgId);   // nessun altro programma: diventa il predefinito
                }
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);   // Esplora risorse aggiorna le icone
        }

        public static void Unregister()
        {
            foreach (string ext in Extensions)
                using (RegistryKey e = Registry.CurrentUser.OpenSubKey(Classes + ext, true))
                {
                    if (e == null) continue;
                    using (RegistryKey open = e.OpenSubKey("OpenWithProgids", true))
                        if (open != null && open.GetValue(ProgId) != null) open.DeleteValue(ProgId, false);
                    if ((e.GetValue("") as string) == ProgId) e.DeleteValue("", false);   // tolto solo se era nostro
                }
            Registry.CurrentUser.DeleteSubKeyTree(Classes + ProgId, false);
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
        }
    }
}
