// Controllo indipendente dell'assemblatore 6502 (src\Asm6502.cs) sui programmi del drive.
// Assembla ogni drivecode\*.a65, poi rilegge i byte con un disassemblatore che ha una sua tabella
// dei codici (scritta a parte) e confronta riga per riga istruzione, modo e valore con il sorgente.
// Uso: compilare con src\Asm6502.cs, poi  asmcheck.exe drivecode [listati]
//   (con una cartella "listati" salva anche il listato di ogni programma)
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using XumerTool;

static class AsmCheck
{
    // tabella NMOS ufficiale: codice -> "MNEMONICO modo"
    static readonly string[] Table = BuildTable();

    static string[] BuildTable()
    {
        string src =
            "00 BRK imp|01 ORA indx|05 ORA zp|06 ASL zp|08 PHP imp|09 ORA imm|0A ASL acc|0D ORA abs|0E ASL abs|" +
            "10 BPL rel|11 ORA indy|15 ORA zpx|16 ASL zpx|18 CLC imp|19 ORA absy|1D ORA absx|1E ASL absx|" +
            "20 JSR abs|21 AND indx|24 BIT zp|25 AND zp|26 ROL zp|28 PLP imp|29 AND imm|2A ROL acc|2C BIT abs|2D AND abs|2E ROL abs|" +
            "30 BMI rel|31 AND indy|35 AND zpx|36 ROL zpx|38 SEC imp|39 AND absy|3D AND absx|3E ROL absx|" +
            "40 RTI imp|41 EOR indx|45 EOR zp|46 LSR zp|48 PHA imp|49 EOR imm|4A LSR acc|4C JMP abs|4D EOR abs|4E LSR abs|" +
            "50 BVC rel|51 EOR indy|55 EOR zpx|56 LSR zpx|58 CLI imp|59 EOR absy|5D EOR absx|5E LSR absx|" +
            "60 RTS imp|61 ADC indx|65 ADC zp|66 ROR zp|68 PLA imp|69 ADC imm|6A ROR acc|6C JMP ind|6D ADC abs|6E ROR abs|" +
            "70 BVS rel|71 ADC indy|75 ADC zpx|76 ROR zpx|78 SEI imp|79 ADC absy|7D ADC absx|7E ROR absx|" +
            "81 STA indx|84 STY zp|85 STA zp|86 STX zp|88 DEY imp|8A TXA imp|8C STY abs|8D STA abs|8E STX abs|" +
            "90 BCC rel|91 STA indy|94 STY zpx|95 STA zpx|96 STX zpy|98 TYA imp|99 STA absy|9A TXS imp|9D STA absx|" +
            "A0 LDY imm|A1 LDA indx|A2 LDX imm|A4 LDY zp|A5 LDA zp|A6 LDX zp|A8 TAY imp|A9 LDA imm|AA TAX imp|AC LDY abs|AD LDA abs|AE LDX abs|" +
            "B0 BCS rel|B1 LDA indy|B4 LDY zpx|B5 LDA zpx|B6 LDX zpy|B8 CLV imp|B9 LDA absy|BA TSX imp|BC LDY absx|BD LDA absx|BE LDX absy|" +
            "C0 CPY imm|C1 CMP indx|C4 CPY zp|C5 CMP zp|C6 DEC zp|C8 INY imp|C9 CMP imm|CA DEX imp|CC CPY abs|CD CMP abs|CE DEC abs|" +
            "D0 BNE rel|D1 CMP indy|D5 CMP zpx|D6 DEC zpx|D8 CLD imp|D9 CMP absy|DD CMP absx|DE DEC absx|" +
            "E0 CPX imm|E1 SBC indx|E4 CPX zp|E5 SBC zp|E6 INC zp|E8 INX imp|E9 SBC imm|EA NOP imp|EC CPX abs|ED SBC abs|EE INC abs|" +
            "F0 BEQ rel|F1 SBC indy|F5 SBC zpx|F6 INC zpx|F8 SED imp|F9 SBC absy|FD SBC absx|FE INC absx";
        string[] t = new string[256];
        foreach (string e in src.Split('|'))
            t[int.Parse(e.Substring(0, 2), NumberStyles.HexNumber)] = e.Substring(3);
        return t;
    }

    static int Size(string mode)
    {
        if (mode == "imp" || mode == "acc") return 1;
        if (mode == "abs" || mode == "absx" || mode == "absy" || mode == "ind") return 3;
        return 2;
    }

    // ---- valutazione delle espressioni, scritta a parte (stesse regole del sorgente: + - < > * $ % decimali) ----
    static int Value(string expr, Dictionary<string, int> sym, int pc)
    {
        string e = expr.Trim();
        int sel = 0;
        if (e.StartsWith("<")) { sel = 1; e = e.Substring(1); }
        else if (e.StartsWith(">")) { sel = 2; e = e.Substring(1); }
        int total = 0, i = 0, sign = 1;
        bool term = true;
        while (i < e.Length)
        {
            char ch = e[i];
            if (ch == ' ' || ch == '\t') { i++; continue; }
            if (!term) { sign = ch == '-' ? -1 : 1; if (ch != '+' && ch != '-') throw new Exception("operatore? " + expr); i++; term = true; continue; }
            if (ch == '-') { sign = -sign; i++; continue; }
            int v, s = i;
            if (ch == '$') { i++; s = i; while (i < e.Length && Uri.IsHexDigit(e[i])) i++; v = int.Parse(e.Substring(s, i - s), NumberStyles.HexNumber); }
            else if (ch == '%') { i++; s = i; while (i < e.Length && (e[i] == '0' || e[i] == '1')) i++; v = Convert.ToInt32(e.Substring(s, i - s), 2); }
            else if (char.IsDigit(ch)) { while (i < e.Length && char.IsDigit(e[i])) i++; v = int.Parse(e.Substring(s, i - s)); }
            else if (ch == '*') { v = pc; i++; }
            else
            {
                while (i < e.Length && (char.IsLetterOrDigit(e[i]) || e[i] == '_')) i++;
                string name = e.Substring(s, i - s);
                if (!sym.TryGetValue(name, out v)) throw new Exception("simbolo sconosciuto " + name);
            }
            total += sign * v; sign = 1; term = false;
        }
        if (sel == 1) total &= 0xFF; else if (sel == 2) total = (total >> 8) & 0xFF;
        return total;
    }

    class Line { public int No; public string Label, Mn, Opnd, SymName, SymExpr, Org; public bool Instr; }

    static List<Line> Parse(string source)
    {
        List<Line> res = new List<Line>();
        string[] lines = source.Replace("\r", "").Split('\n');
        for (int n = 0; n < lines.Length; n++)
        {
            string raw = lines[n];
            int sc = raw.IndexOf(';');
            if (sc >= 0) raw = raw.Substring(0, sc);
            string t = raw.Trim();
            if (t.Length == 0) continue;
            Line l = new Line { No = n + 1 };
            if (t.StartsWith("*")) { l.Org = t.Substring(t.IndexOf('=') + 1); res.Add(l); continue; }
            int eq = t.IndexOf('=');
            if (eq > 0 && t.Substring(0, eq).Trim().IndexOf(' ') < 0 && t.Substring(0, eq).Trim().IndexOf('\t') < 0)
            { l.SymName = t.Substring(0, eq).Trim(); l.SymExpr = t.Substring(eq + 1); res.Add(l); continue; }
            // etichetta: in colonna 0 o con i due punti, e non un mnemonico
            string[] parts = t.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
            string first = parts[0].TrimEnd(':');
            bool isMn = Array.Exists(Table, x => x != null && x.StartsWith(first.ToUpperInvariant() + " "));
            if ((!char.IsWhiteSpace(raw[0]) || parts[0].EndsWith(":")) && !isMn)
            {
                l.Label = first;
                t = parts.Length > 1 ? parts[1].Trim() : "";
                if (t.Length == 0) { res.Add(l); continue; }
                parts = t.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
            }
            l.Instr = true;
            l.Mn = parts[0].ToUpperInvariant();
            l.Opnd = parts.Length > 1 ? parts[1].Trim() : "";
            res.Add(l);
        }
        return res;
    }

    static int Main(string[] args)
    {
        string dir = args[0];
        string listDir = args.Length > 1 ? args[1] : null;
        if (listDir != null) Directory.CreateDirectory(listDir);
        int errors = 0, warnings = 0;
        foreach (string file in Directory.GetFiles(dir, "*.a65"))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            string src = File.ReadAllText(file);
            int org;
            byte[] bin = Asm6502.Assemble(src, out org);
            List<Line> lines = Parse(src);

            // passata 1: indirizzi delle etichette ricavati camminando sui byte prodotti
            Dictionary<string, int> sym = new Dictionary<string, int>();
            int pc = 0, off = 0;
            foreach (Line l in lines)
            {
                if (l.Org != null) { pc = Value(l.Org, sym, pc); continue; }
                if (l.SymName != null) { try { sym[l.SymName] = Value(l.SymExpr, sym, pc); } catch { } continue; }
                if (l.Label != null) sym[l.Label] = pc;
                if (!l.Instr) continue;
                string d = Table[bin[off]];
                if (d == null) { Console.WriteLine("{0}: riga {1}: codice ${2:X2} non valido", name, l.No, bin[off]); errors++; break; }
                int sz = Size(d.Split(' ')[1]);
                off += sz; pc += sz;
            }
            // simboli definiti dopo (es. do_retry = * alla fine): seconda valutazione
            pc = 0;
            foreach (Line l in lines)
                if (l.SymName != null && !sym.ContainsKey(l.SymName))
                    try { sym[l.SymName] = Value(l.SymExpr, sym, org + bin.Length); } catch { }

            // passata 2: confronto istruzione per istruzione
            StringBuilder listing = new StringBuilder();
            pc = org; off = 0;
            int count = 0;
            foreach (Line l in lines)
            {
                if (l.Org != null) { pc = Value(l.Org, sym, pc); continue; }
                if (!l.Instr) continue;
                string[] d = Table[bin[off]].Split(' ');
                string mn = d[0], mode = d[1];
                int sz = Size(mode);
                int operand = sz == 2 ? bin[off + 1] : sz == 3 ? bin[off + 1] | (bin[off + 2] << 8) : 0;
                if (mode == "rel") operand = pc + 2 + (sbyte)bin[off + 1];

                // modo atteso dalla sintassi del sorgente
                string lo = l.Opnd.ToLowerInvariant().Replace(" ", "").Replace("\t", "");
                string expect, expr = null;
                if (lo.Length == 0 || lo == "a") expect = mode == "acc" ? "acc" : "imp";
                else if (mode == "rel") { expect = "rel"; expr = l.Opnd; }
                else if (lo.StartsWith("#")) { expect = "imm"; expr = l.Opnd.Substring(l.Opnd.IndexOf('#') + 1); }
                else if (lo.StartsWith("(") && lo.EndsWith("),y")) { expect = "indy"; expr = l.Opnd.Substring(l.Opnd.IndexOf('(') + 1, l.Opnd.IndexOf(')') - l.Opnd.IndexOf('(') - 1); }
                else if (lo.StartsWith("(") && lo.EndsWith(",x)")) { expect = "indx"; expr = l.Opnd.Substring(l.Opnd.IndexOf('(') + 1, l.Opnd.LastIndexOf(',') - l.Opnd.IndexOf('(') - 1); }
                else if (lo.StartsWith("(")) { expect = "ind"; expr = l.Opnd.Substring(l.Opnd.IndexOf('(') + 1, l.Opnd.LastIndexOf(')') - l.Opnd.IndexOf('(') - 1); }
                else
                {
                    string idx = lo.EndsWith(",x") ? "x" : lo.EndsWith(",y") ? "y" : "";
                    expr = idx.Length > 0 ? l.Opnd.Substring(0, l.Opnd.LastIndexOf(',')) : l.Opnd;
                    expect = (mode.StartsWith("zp") ? "zp" : "abs") + idx;   // zp o abs: si controlla sotto col valore
                }

                string problem = null;
                if (mn != l.Mn) problem = "istruzione " + mn + " invece di " + l.Mn;
                else if (mode != expect) problem = "modo " + mode + " invece di " + expect;
                else if (expr != null)
                {
                    int v = Value(expr, sym, pc);
                    int want = mode == "imm" || mode.StartsWith("zp") || mode == "indx" || mode == "indy" ? v & 0xFF : mode == "rel" ? v : v & 0xFFFF;
                    if (want != operand) problem = string.Format("valore ${0:X4} invece di ${1:X4}", operand, want);
                    else if (mode.StartsWith("abs") && v >= 0 && v < 256 && Array.IndexOf(Table, mn + " zp" + mode.Substring(3)) >= 0)
                    {
                        Console.WriteLine("{0}: riga {1}: ATTENZIONE {2} {3} usa l'indirizzo lungo per ${4:X2} (pagina zero)", name, l.No, l.Mn, l.Opnd, v);
                        warnings++;
                    }
                }
                StringBuilder hex = new StringBuilder();
                for (int k = 0; k < sz; k++) hex.AppendFormat("{0:X2} ", bin[off + k]);
                string dis = mn + (mode == "imp" ? "" : mode == "acc" ? " A" : mode == "imm" ? string.Format(" #${0:X2}", operand) :
                    mode == "rel" || sz == 3 ? string.Format(mode == "ind" ? " (${0:X4})" : " ${0:X4}", operand) + (mode.EndsWith("x") ? ",X" : mode.EndsWith("y") ? ",Y" : "") :
                    mode == "indx" ? string.Format(" (${0:X2},X)", operand) : mode == "indy" ? string.Format(" (${0:X2}),Y", operand) :
                    string.Format(" ${0:X2}", operand) + (mode.EndsWith("x") ? ",X" : mode.EndsWith("y") ? ",Y" : ""));
                listing.AppendFormat("{0:X4}  {1,-9} {2,-16} ; riga {3,3}: {4} {5}{6}\r\n", pc, hex, dis, l.No, l.Mn, l.Opnd, problem != null ? "   <<< " + problem : "");
                if (problem != null) { Console.WriteLine("{0}: riga {1}: ERRORE {2}  [{3} {4}]", name, l.No, problem, l.Mn, l.Opnd); errors++; }
                off += sz; pc += sz; count++;
            }
            if (off != bin.Length) { Console.WriteLine("{0}: {1} byte in più non spiegati dal sorgente", name, bin.Length - off); errors++; }
            Console.WriteLine("{0}: ${1:X4}-${2:X4}, {3} byte, {4} istruzioni controllate", name, org, org + bin.Length - 1, bin.Length, count);
            if (listDir != null) File.WriteAllText(Path.Combine(listDir, name + ".lst"), listing.ToString());
        }
        Console.WriteLine(errors == 0 ? "RISULTATO: nessun errore" : "RISULTATO: " + errors + " errori");
        if (warnings > 0) Console.WriteLine("Avvisi: " + warnings);
        return errors == 0 ? 0 : 1;
    }
}
