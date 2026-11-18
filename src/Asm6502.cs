// Assemblatore 6502 minimo (due passate), sufficiente per i programmi drive in drivecode\*.a65.
// Supporta: etichette (con o senza ':'), SIMBOLO = espressione, *=origine,
// espressioni con + - < > * $hex %bin decimali, tutti i modi d'indirizzamento NMOS.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace XumerTool
{
    public static class Asm6502
    {
        static readonly Dictionary<string, Dictionary<string, byte>> Ops = BuildOps();

        static Dictionary<string, Dictionary<string, byte>> BuildOps()
        {
            string table =
                "ADC imm 69 zp 65 zpx 75 abs 6D absx 7D absy 79 indx 61 indy 71|" +
                "AND imm 29 zp 25 zpx 35 abs 2D absx 3D absy 39 indx 21 indy 31|" +
                "ASL acc 0A zp 06 zpx 16 abs 0E absx 1E|" +
                "BCC rel 90|BCS rel B0|BEQ rel F0|BMI rel 30|BNE rel D0|BPL rel 10|BVC rel 50|BVS rel 70|" +
                "BIT zp 24 abs 2C|BRK imp 00|CLC imp 18|CLD imp D8|CLI imp 58|CLV imp B8|" +
                "CMP imm C9 zp C5 zpx D5 abs CD absx DD absy D9 indx C1 indy D1|" +
                "CPX imm E0 zp E4 abs EC|CPY imm C0 zp C4 abs CC|" +
                "DEC zp C6 zpx D6 abs CE absx DE|DEX imp CA|DEY imp 88|" +
                "EOR imm 49 zp 45 zpx 55 abs 4D absx 5D absy 59 indx 41 indy 51|" +
                "INC zp E6 zpx F6 abs EE absx FE|INX imp E8|INY imp C8|" +
                "JMP abs 4C ind 6C|JSR abs 20|" +
                "LDA imm A9 zp A5 zpx B5 abs AD absx BD absy B9 indx A1 indy B1|" +
                "LDX imm A2 zp A6 zpy B6 abs AE absy BE|LDY imm A0 zp A4 zpx B4 abs AC absx BC|" +
                "LSR acc 4A zp 46 zpx 56 abs 4E absx 5E|NOP imp EA|" +
                "ORA imm 09 zp 05 zpx 15 abs 0D absx 1D absy 19 indx 01 indy 11|" +
                "PHA imp 48|PHP imp 08|PLA imp 68|PLP imp 28|" +
                "ROL acc 2A zp 26 zpx 36 abs 2E absx 3E|ROR acc 6A zp 66 zpx 76 abs 6E absx 7E|" +
                "RTI imp 40|RTS imp 60|" +
                "SBC imm E9 zp E5 zpx F5 abs ED absx FD absy F9 indx E1 indy F1|" +
                "SEC imp 38|SED imp F8|SEI imp 78|" +
                "STA zp 85 zpx 95 abs 8D absx 9D absy 99 indx 81 indy 91|" +
                "STX zp 86 zpy 96 abs 8E|STY zp 84 zpx 94 abs 8C|" +
                "TAX imp AA|TAY imp A8|TSX imp BA|TXA imp 8A|TXS imp 9A|TYA imp 98";
            Dictionary<string, Dictionary<string, byte>> d = new Dictionary<string, Dictionary<string, byte>>();
            foreach (string entry in table.Split('|'))
            {
                string[] p = entry.Split(' ');
                Dictionary<string, byte> modes = new Dictionary<string, byte>();
                for (int i = 1; i + 1 < p.Length; i += 2)
                    modes[p[i]] = byte.Parse(p[i + 1], NumberStyles.HexNumber);
                d[p[0]] = modes;
            }
            return d;
        }

        class Ctx
        {
            public Dictionary<string, int> Sym = new Dictionary<string, int>();
            public int Pc;
            public bool Final;
        }

        static readonly Regex ReOrigin = new Regex(@"^\*\s*=\s*(.+)$");
        static readonly Regex ReSymDef = new Regex(@"^([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.+)$");
        static readonly Regex ReLabel = new Regex(@"^([A-Za-z_][A-Za-z0-9_]*):?\s*(.*)$");

        /// <summary>Valuta un'espressione. known=false se contiene simboli non ancora definiti.</summary>
        static int Eval(string expr, Ctx c, out bool known)
        {
            known = true;
            string e = expr.Trim();
            int sel = 0;
            if (e.StartsWith("<")) { sel = 1; e = e.Substring(1).Trim(); }
            else if (e.StartsWith(">")) { sel = 2; e = e.Substring(1).Trim(); }
            int total = 0, sign = 1, i = 0;
            bool expectTerm = true;
            while (i < e.Length)
            {
                char ch = e[i];
                if (char.IsWhiteSpace(ch)) { i++; continue; }
                if (!expectTerm)
                {
                    if (ch == '+') sign = 1;
                    else if (ch == '-') sign = -1;
                    else throw new FormatException("operatore inatteso in '" + expr + "'");
                    i++; expectTerm = true; continue;
                }
                if (ch == '-') { sign = -sign; i++; continue; }
                int v;
                if (ch == '$')
                {
                    int s = ++i;
                    while (i < e.Length && Uri.IsHexDigit(e[i])) i++;
                    v = int.Parse(e.Substring(s, i - s), NumberStyles.HexNumber);
                }
                else if (ch == '%')
                {
                    int s = ++i;
                    while (i < e.Length && (e[i] == '0' || e[i] == '1')) i++;
                    v = Convert.ToInt32(e.Substring(s, i - s), 2);
                }
                else if (char.IsDigit(ch))
                {
                    int s = i;
                    while (i < e.Length && char.IsDigit(e[i])) i++;
                    v = int.Parse(e.Substring(s, i - s));
                }
                else if (ch == '*') { v = c.Pc; i++; }
                else if (char.IsLetter(ch) || ch == '_')
                {
                    int s = i;
                    while (i < e.Length && (char.IsLetterOrDigit(e[i]) || e[i] == '_')) i++;
                    string name = e.Substring(s, i - s);
                    if (!c.Sym.TryGetValue(name, out v))
                    {
                        if (c.Final) throw new FormatException("simbolo non definito: " + name);
                        known = false; v = 0;
                    }
                }
                else throw new FormatException("carattere inatteso in '" + expr + "'");
                total += sign * v;
                sign = 1;
                expectTerm = false;
            }
            if (sel == 1) total &= 0xFF;
            else if (sel == 2) total = (total >> 8) & 0xFF;
            return total;
        }

        public static byte[] Assemble(string source, out int origin)
        {
            string[] lines = source.Replace("\r", "").Split('\n');
            Dictionary<int, string> modeOfLine = new Dictionary<int, string>();
            Ctx c = new Ctx();
            List<byte> output = null;
            origin = -1;

            for (int pass = 1; pass <= 2; pass++)
            {
                c.Final = pass == 2;
                c.Pc = 0;
                output = new List<byte>();
                for (int ln = 0; ln < lines.Length; ln++)
                {
                    string raw = lines[ln];
                    int sc = raw.IndexOf(';');
                    if (sc >= 0) raw = raw.Substring(0, sc);
                    string t = raw.Trim();
                    if (t.Length == 0) continue;
                    try
                    {
                        Match m = ReOrigin.Match(t);
                        if (m.Success)
                        {
                            bool k;
                            c.Pc = Eval(m.Groups[1].Value, c, out k);
                            if (origin < 0) origin = c.Pc;
                            continue;
                        }
                        m = ReSymDef.Match(t);
                        if (m.Success)
                        {
                            bool k;
                            int v = Eval(m.Groups[2].Value, c, out k);
                            if (k) c.Sym[m.Groups[1].Value] = v;
                            continue;
                        }
                        // etichetta: in colonna 0, oppure seguita da ':'
                        string stmt = t;
                        if (!char.IsWhiteSpace(raw[0]) || Regex.IsMatch(t, @"^[A-Za-z_][A-Za-z0-9_]*:"))
                        {
                            m = ReLabel.Match(t);
                            string first = m.Groups[1].Value;
                            if (m.Success && !Ops.ContainsKey(first.ToUpperInvariant()))
                            {
                                if (pass == 1)
                                {
                                    if (c.Sym.ContainsKey(first)) throw new FormatException("etichetta duplicata " + first);
                                    c.Sym[first] = c.Pc;
                                }
                                else if (c.Sym[first] != c.Pc) throw new FormatException("fase errata per " + first);
                                stmt = m.Groups[2].Value.Trim();
                                if (stmt.Length == 0) continue;
                            }
                        }
                        Emit(stmt, c, pass, ln, modeOfLine, output);
                    }
                    catch (FormatException ex)
                    {
                        throw new FormatException("riga " + (ln + 1) + ": " + ex.Message + "  [" + lines[ln].Trim() + "]");
                    }
                }
            }
            return output.ToArray();
        }

        static void Emit(string stmt, Ctx c, int pass, int ln, Dictionary<int, string> modeOfLine, List<byte> output)
        {
            int sp = 0;
            while (sp < stmt.Length && !char.IsWhiteSpace(stmt[sp])) sp++;
            string mn = stmt.Substring(0, sp).ToUpperInvariant();
            string opnd = stmt.Substring(sp).Trim();
            Dictionary<string, byte> modes;
            if (!Ops.TryGetValue(mn, out modes)) throw new FormatException("istruzione sconosciuta " + mn);

            string mode;
            string expr = null;
            string lo = opnd.ToLowerInvariant().Replace(" ", "");
            if (lo.Length == 0) mode = modes.ContainsKey("acc") ? "acc" : "imp";
            else if (lo == "a" && modes.ContainsKey("acc")) mode = "acc";
            else if (modes.ContainsKey("rel")) { mode = "rel"; expr = opnd; }
            else if (lo.StartsWith("#")) { mode = "imm"; expr = opnd.Trim().Substring(1); }
            else if (lo.StartsWith("(") && lo.EndsWith("),y")) { mode = "indy"; expr = opnd.Substring(opnd.IndexOf('(') + 1, opnd.LastIndexOf(')') - opnd.IndexOf('(') - 1); }
            else if (lo.StartsWith("(") && lo.EndsWith(",x)")) { mode = "indx"; expr = opnd.Substring(opnd.IndexOf('(') + 1, opnd.LastIndexOf(',') - opnd.IndexOf('(') - 1); }
            else if (lo.StartsWith("(") && lo.EndsWith(")") && modes.ContainsKey("ind")) { mode = "ind"; expr = opnd.Substring(opnd.IndexOf('(') + 1, opnd.LastIndexOf(')') - opnd.IndexOf('(') - 1); }
            else
            {
                string idx = "";
                expr = opnd;
                if (lo.EndsWith(",x") || lo.EndsWith(",y"))
                {
                    idx = lo.Substring(lo.Length - 1);
                    expr = opnd.Substring(0, opnd.LastIndexOf(','));
                }
                string chosen;
                if (pass == 1)
                {
                    bool known;
                    int v = Eval(expr, c, out known);
                    bool zpOk = modes.ContainsKey("zp" + idx);
                    bool absOk = modes.ContainsKey("abs" + idx);
                    if (zpOk && (!absOk || (known && v >= 0 && v < 256))) chosen = "zp" + idx;
                    else if (absOk) chosen = "abs" + idx;
                    else throw new FormatException("modo non valido per " + mn);
                    modeOfLine[ln] = chosen;
                }
                else chosen = modeOfLine[ln];
                mode = chosen;
            }
            if (!modes.ContainsKey(mode)) throw new FormatException("modo " + mode + " non valido per " + mn);

            int size = (mode == "imp" || mode == "acc") ? 1 :
                       (mode == "abs" || mode == "absx" || mode == "absy" || mode == "ind") ? 3 : 2;
            if (pass == 2)
            {
                output.Add(modes[mode]);
                if (size > 1)
                {
                    bool known;
                    int v = Eval(expr, c, out known);
                    if (mode == "rel")
                    {
                        int off = v - (c.Pc + 2);
                        if (off < -128 || off > 127) throw new FormatException("salto troppo lontano");
                        output.Add((byte)(off & 0xFF));
                    }
                    else if (size == 2)
                    {
                        if (v < 0 || v > 255) throw new FormatException("valore fuori intervallo: " + v);
                        output.Add((byte)v);
                    }
                    else
                    {
                        output.Add((byte)(v & 0xFF));
                        output.Add((byte)((v >> 8) & 0xFF));
                    }
                }
            }
            c.Pc += size;
        }
    }
}
