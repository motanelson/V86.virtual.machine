
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace X86Studio
{
    class CPU8086
    {
        public byte[] Memory = new byte[1024 * 1024];

        public ushort AX, BX, CX, DX;
        public ushort SI, DI, BP, SP;
        public ushort CS, DS, ES, SS, IP;

        public bool ZF, CF, SF;
        public bool Halted;

        public int Cycles;

        public StringBuilder Output = new StringBuilder();

        public CPU8086()
        {
            Reset();
        }

        public void Reset()
        {
            Array.Clear(Memory, 0, Memory.Length);

            AX = BX = CX = DX = 0;
            SI = DI = BP = 0;

            SP = 0xFFFE;

            CS = DS = ES = SS = 0x0700;
            IP = 0x0100;

            ZF = CF = SF = false;

            Halted = false;
            Cycles = 0;

            Output.Clear();
        }

        public int Linear(ushort seg, int offset)
        {
            return (((seg & 0xFFFF) << 4) +
                    (offset & 0xFFFF)) & 0xFFFFF;
        }

        public byte Read8(ushort seg, int offset)
        {
            return Memory[Linear(seg, offset)];
        }

        public ushort Read16(ushort seg, int offset)
        {
            int lo = Read8(seg, offset);
            int hi = Read8(seg, offset + 1);

            return (ushort)(lo | (hi << 8));
        }

        public void Write8(ushort seg, int offset, byte value)
        {
            Memory[Linear(seg, offset)] = value;
        }

        public void Write16(ushort seg, int offset, ushort value)
        {
            Write8(seg, offset, (byte)value);
            Write8(seg, offset + 1, (byte)(value >> 8));
        }

        public byte Fetch8()
        {
            return Read8(CS, IP++);
        }

        public ushort Fetch16()
        {
            ushort value = Read16(CS, IP);
            IP += 2;
            return value;
        }

        public sbyte FetchSigned8()
        {
            return unchecked((sbyte)Fetch8());
        }

        public void LoadCOM(byte[] binary)
        {
            Reset();

            int address = Linear(CS, 0x100);

            for (int i = 0; i < binary.Length; i++)
                Memory[address + i] = binary[i];

            IP = 0x100;
        }

        public void Print(string text)
        {
            Output.Append(text);
        }

        public void HandleINT20()
        {
            Halted = true;
            Print("\n[Program terminated (INT 20h)]\n");
        }

        public void HandleINT21()
        {
            byte ah = (byte)(AX >> 8);

            if (ah == 9)
            {
                int offset = DX;
                int safety = 0;

                while (safety++ < 65535)
                {
                    byte c = Read8(DS, offset++);

                    if (c == '$')
                        break;

                    Print(((char)c).ToString());
                }
            }
            else if (ah == 2)
            {
                Print(((char)(DX & 0xFF)).ToString());
            }
            else if (ah == 0x4C)
            {
                Halted = true;

                Print("\n[Program finished with return code " +
                      (AX & 0xFF) + "]\n");
            }
        }

        public ushort GetReg16(int index)
        {
            switch (index)
            {
                case 0: return AX;
                case 1: return CX;
                case 2: return DX;
                case 3: return BX;
                case 4: return SP;
                case 5: return BP;
                case 6: return SI;
                default: return DI;
            }
        }

        public void SetReg16(int index, ushort value)
        {
            switch (index)
            {
                case 0: AX = value; break;
                case 1: CX = value; break;
                case 2: DX = value; break;
                case 3: BX = value; break;
                case 4: SP = value; break;
                case 5: BP = value; break;
                case 6: SI = value; break;
                case 7: DI = value; break;
            }
        }

        public byte GetReg8(int index)
        {
            switch (index)
            {
                case 0: return (byte)AX;
                case 1: return (byte)CX;
                case 2: return (byte)DX;
                case 3: return (byte)BX;
                case 4: return (byte)(AX >> 8);
                case 5: return (byte)(CX >> 8);
                case 6: return (byte)(DX >> 8);
                default: return (byte)(BX >> 8);
            }
        }

        public void SetReg8(int index, byte value)
        {
            switch (index)
            {
                case 0: AX = (ushort)((AX & 0xFF00) | value); break;
                case 1: CX = (ushort)((CX & 0xFF00) | value); break;
                case 2: DX = (ushort)((DX & 0xFF00) | value); break;
                case 3: BX = (ushort)((BX & 0xFF00) | value); break;
                case 4: AX = (ushort)((AX & 0x00FF) | (value << 8)); break;
                case 5: CX = (ushort)((CX & 0x00FF) | (value << 8)); break;
                case 6: DX = (ushort)((DX & 0x00FF) | (value << 8)); break;
                case 7: BX = (ushort)((BX & 0x00FF) | (value << 8)); break;
            }
        }

        public void Step()
        {
            if (Halted)
                return;

            byte op = Fetch8();

            Cycles++;

            switch (op)
            {
                case 0x90:
                    break;

                case 0xCD:
                {
                    byte num = Fetch8();

                    if (num == 0x20)
                        HandleINT20();

                    else if (num == 0x21)
                        HandleINT21();

                    else if (num == 0x10)
                    {
                        if ((AX >> 8) == 0x0E)
                            Print(((char)(AX & 0xFF)).ToString());
                    }
                    else
                    {
                        Print("\n[INT " + num.ToString("X2") + "]\n");
                    }

                    break;
                }

                // MOV register16, immediate16

                case 0xB8: AX = Fetch16(); break;
                case 0xB9: CX = Fetch16(); break;
                case 0xBA: DX = Fetch16(); break;
                case 0xBB: BX = Fetch16(); break;
                case 0xBC: SP = Fetch16(); break;
                case 0xBD: BP = Fetch16(); break;
                case 0xBE: SI = Fetch16(); break;
                case 0xBF: DI = Fetch16(); break;

                // MOV register8, immediate8

                case 0xB0: SetReg8(0, Fetch8()); break;
                case 0xB1: SetReg8(1, Fetch8()); break;
                case 0xB2: SetReg8(2, Fetch8()); break;
                case 0xB3: SetReg8(3, Fetch8()); break;
                case 0xB4: SetReg8(4, Fetch8()); break;
                case 0xB5: SetReg8(5, Fetch8()); break;
                case 0xB6: SetReg8(6, Fetch8()); break;
                case 0xB7: SetReg8(7, Fetch8()); break;

                // INC register16

                case 0x40:
                case 0x41:
                case 0x42:
                case 0x43:
                {
                    int r = op - 0x40;
                    SetReg16(r, (ushort)(GetReg16(r) + 1));
                    break;
                }

                // DEC register16

                case 0x48:
                case 0x49:
                case 0x4A:
                case 0x4B:
                {
                    int r = op - 0x48;
                    SetReg16(r, (ushort)(GetReg16(r) - 1));
                    break;
                }

                // INC register8

                case 0xFE:
                {
                    byte modrm = Fetch8();

                    if ((modrm & 0xF8) == 0xC0)
                    {
                        int r = modrm & 7;

                        byte value = (byte)(GetReg8(r) + 1);

                        SetReg8(r, value);
                        ZF = value == 0;
                    }

                    break;
                }

                // LOOP

                case 0xE2:
                {
                    sbyte rel = FetchSigned8();

                    CX--;

                    if (CX != 0)
                        IP = (ushort)(IP + rel);

                    break;
                }

                // JMP

                case 0xEB:
                {
                    sbyte rel = FetchSigned8();
                    IP = (ushort)(IP + rel);
                    break;
                }

                // JZ

                case 0x74:
                {
                    sbyte rel = FetchSigned8();

                    if (ZF)
                        IP = (ushort)(IP + rel);

                    break;
                }

                // JNZ

                case 0x75:
                {
                    sbyte rel = FetchSigned8();

                    if (!ZF)
                        IP = (ushort)(IP + rel);

                    break;
                }

                // MOV segment register

                case 0x8E:
                {
                    byte modrm = Fetch8();

                    int sreg = (modrm >> 3) & 7;
                    ushort value = GetReg16(modrm & 7);

                    if (sreg == 0) ES = value;
                    if (sreg == 1) CS = value;
                    if (sreg == 2) SS = value;
                    if (sreg == 3) DS = value;

                    break;
                }

                // Direct memory write

                case 0xC6:
                {
                    byte modrm = Fetch8();

                    int disp = 0;

                    if (modrm == 0x45)
                        disp = Fetch8();

                    byte value = Fetch8();

                    Write8(ES, DI + disp, value);

                    break;
                }

                // ADD register16, register16

                case 0x01:
                {
                    byte modrm = Fetch8();

                    int src = (modrm >> 3) & 7;
                    int dst = modrm & 7;

                    uint result = (uint)GetReg16(dst) +
                                  GetReg16(src);

                    SetReg16(dst, (ushort)result);

                    ZF = (ushort)result == 0;
                    CF = result > 0xFFFF;

                    break;
                }

                // CMP register16, immediate16

                case 0x81:
                {
                    byte modrm = Fetch8();

                    if ((modrm & 0xF8) == 0xF8)
                    {
                        int r = modrm & 7;

                        ushort imm = Fetch16();

                        int diff = GetReg16(r) - imm;

                        ZF = diff == 0;
                        SF = (diff & 0x8000) != 0;
                        CF = diff < 0;
                    }

                    break;
                }

                default:
                    Print("\n[Unsupported opcode: 0x" +
                          op.ToString("X2") + "]\n");

                    Halted = true;
                    break;
            }
        }

        public void ShowRegisters()
        {
            Console.WriteLine();
            Console.WriteLine("8086 REGISTER ARCHITECTURE");
            Console.WriteLine("---------------------------------------");

            Console.WriteLine("AX: {0:X4}  BX: {1:X4}",
                AX, BX);

            Console.WriteLine("CX: {0:X4}  DX: {1:X4}",
                CX, DX);

            Console.WriteLine("SI: {0:X4}  DI: {1:X4}",
                SI, DI);

            Console.WriteLine("BP: {0:X4}  SP: {1:X4}",
                BP, SP);

            Console.WriteLine("CS:IP {0:X4}:{1:X4}",
                CS, IP);

            Console.WriteLine("DS:DX {0:X4}:{1:X4}",
                DS, DX);

            Console.WriteLine("ES: {0:X4}  SS: {1:X4}",
                ES, SS);

            Console.WriteLine("FLAGS: {0} {1} {2}",
                ZF ? "ZF" : "--",
                CF ? "CF" : "--",
                SF ? "SF" : "--");

            Console.WriteLine("Cycles: " + Cycles);
            Console.WriteLine("---------------------------------------");
        }

        public void ShowVGA()
        {
            Console.WriteLine();
            Console.WriteLine("VGA 80x25 TEXT MODE");
            Console.WriteLine("---------------------------------------");

            int address = Linear(0xB800, 0);

            for (int row = 0; row < 25; row++)
            {
                StringBuilder line = new StringBuilder();

                for (int col = 0; col < 80; col++)
                {
                    byte c = Memory[address + (row * 80 + col) * 2];

                    line.Append(c >= 32 ? (char)c : ' ');
                }

                Console.WriteLine(line.ToString().TrimEnd());
            }
        }
    }

    class Assembler8086
    {
        readonly string[] R16 =
        {
            "ax", "cx", "dx", "bx",
            "sp", "bp", "si", "di"
        };

        readonly string[] R8 =
        {
            "al", "cl", "dl", "bl",
            "ah", "ch", "dh", "bh"
        };

        readonly string[] SREG =
        {
            "es", "cs", "ss", "ds"
        };

        Dictionary<string, int> labels =
            new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

        public int ParseNumber(string s)
        {
            s = s.Trim();

            if (s.Length == 0)
                return 0;

            if ((s.StartsWith("'") && s.EndsWith("'")) ||
                (s.StartsWith("\"") && s.EndsWith("\"")))
                return s[1];

            if (s.EndsWith("h", StringComparison.OrdinalIgnoreCase))
                return Convert.ToInt32(s.Substring(0, s.Length - 1), 16);

            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return Convert.ToInt32(s.Substring(2), 16);

            return int.Parse(s);
        }

        public int Resolve(string s)
        {
            s = s.Trim();

            if (s.StartsWith("offset ",
                StringComparison.OrdinalIgnoreCase))
                s = s.Substring(7).Trim();

            if (labels.TryGetValue(s, out int value))
                return value;

            return ParseNumber(s);
        }

        List<byte> ParseDB(string s)
        {
            List<byte> result = new List<byte>();

            int i = 0;

            while (i < s.Length)
            {
                while (i < s.Length &&
                       (s[i] == ' ' || s[i] == '\t' || s[i] == ','))
                    i++;

                if (i >= s.Length)
                    break;

                if (s[i] == '\'' || s[i] == '"')
                {
                    char quote = s[i++];

                    while (i < s.Length && s[i] != quote)
                        result.Add((byte)s[i++]);

                    if (i < s.Length)
                        i++;
                }
                else
                {
                    int start = i;

                    while (i < s.Length &&
                           s[i] != ',' &&
                           s[i] != ' ' &&
                           s[i] != '\t')
                        i++;

                    string token = s.Substring(start, i - start);

                    if (token.Length > 0)
                        result.Add((byte)ParseNumber(token));
                }
            }

            return result;
        }

        string RemoveComment(string line)
        {
            bool quoted = false;
            char quote = '\0';

            for (int i = 0; i < line.Length; i++)
            {
                if ((line[i] == '\'' || line[i] == '"'))
                {
                    if (!quoted)
                    {
                        quoted = true;
                        quote = line[i];
                    }
                    else if (line[i] == quote)
                        quoted = false;
                }

                if (line[i] == ';' && !quoted)
                    return line.Substring(0, i);
            }

            return line;
        }

        string[] SplitOperands(string s)
        {
            List<string> result = new List<string>();

            bool quoted = false;
            char quote = '\0';

            int start = 0;

            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\'' || s[i] == '"')
                {
                    if (!quoted)
                    {
                        quoted = true;
                        quote = s[i];
                    }
                    else if (s[i] == quote)
                        quoted = false;
                }

                if (s[i] == ',' && !quoted)
                {
                    result.Add(s.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }

            result.Add(s.Substring(start).Trim());

            return result.ToArray();
        }

        List<byte> Encode(string line, int addr, bool pass2)
        {
            line = RemoveComment(line).Trim();

            if (line.Length == 0)
                return new List<byte>();

            if (line.Contains(":"))
            {
                int p = line.IndexOf(':');
                line = line.Substring(p + 1).Trim();

                if (line.Length == 0)
                    return new List<byte>();
            }

            string[] first = line.Split(
                new char[] { ' ', '\t' },
                2,
                StringSplitOptions.RemoveEmptyEntries);

            string mnemonic = first[0].ToLower();

            string operands = first.Length > 1 ? first[1].Trim() : "";

            string[] args = SplitOperands(operands);

            string op1 = args.Length > 0 ? args[0].ToLower() : "";
            string op2 = args.Length > 1 ? args[1].ToLower() : "";

            List<byte> b = new List<byte>();

            if (mnemonic == "org")
                return b;

            if (mnemonic == "db")
                return ParseDB(operands);

            if (mnemonic == "int")
            {
                int n = ParseNumber(op1);

                if (n == 20) n = 0x20;
                if (n == 21) n = 0x21;
                if (n == 10) n = 0x10;

                return new List<byte>
                {
                    0xCD, (byte)n
                };
            }

            if (mnemonic == "mov")
            {
                // MOV byte [ES:DI], immediate

                if (line.ToLower().Contains("[es:di"))
                {
                    int disp = 0;

                    var match = System.Text.RegularExpressions.Regex.Match(
                        line, @"\+([0-9a-fA-F]+h?|[0-9]+)");

                    if (match.Success)
                        disp = ParseNumber(match.Groups[1].Value);

                    int comma = operands.LastIndexOf(',');

                    int imm = ParseNumber(
                        operands.Substring(comma + 1).Trim());

                    if (disp > 0)
                        return new List<byte>
                        {
                            0xC6, 0x45, (byte)disp, (byte)imm
                        };

                    return new List<byte>
                    {
                        0xC6, 0x05, (byte)imm
                    };
                }

                int sr = Array.IndexOf(SREG, op1);
                int rr = Array.IndexOf(R16, op2);

                if (sr >= 0 && rr >= 0)
                {
                    return new List<byte>
                    {
                        0x8E, (byte)(0xC0 | (sr << 3) | rr)
                    };
                }

                int r16 = Array.IndexOf(R16, op1);

                if (r16 >= 0)
                {
                    int value = pass2 ? Resolve(op2) : 0;

                    return new List<byte>
                    {
                        (byte)(0xB8 + r16),
                        (byte)value,
                        (byte)(value >> 8)
                    };
                }

                int r8 = Array.IndexOf(R8, op1);

                if (r8 >= 0)
                {
                    int value = pass2 ? Resolve(op2) : 0;

                    return new List<byte>
                    {
                        (byte)(0xB0 + r8),
                        (byte)value
                    };
                }
            }

            if (mnemonic == "inc")
            {
                int r = Array.IndexOf(R16, op1);

                if (r >= 0)
                    return new List<byte> { (byte)(0x40 + r) };

                r = Array.IndexOf(R8, op1);

                if (r >= 0)
                    return new List<byte> { 0xFE, (byte)(0xC0 + r) };
            }

            if (mnemonic == "dec")
            {
                int r = Array.IndexOf(R16, op1);

                if (r >= 0)
                    return new List<byte> { (byte)(0x48 + r) };
            }

            if (mnemonic == "loop" ||
                mnemonic == "jmp" ||
                mnemonic == "jz" ||
                mnemonic == "jnz")
            {
                int target = pass2
                    ? Resolve(op1)
                    : addr;

                int rel = (target - (addr + 2)) & 0xFF;

                byte opcode = 0xE2;

                if (mnemonic == "jmp") opcode = 0xEB;
                if (mnemonic == "jz") opcode = 0x74;
                if (mnemonic == "jnz") opcode = 0x75;

                return new List<byte>
                {
                    opcode, (byte)rel
                };
            }

            if (mnemonic == "add")
            {
                int dst = Array.IndexOf(R16, op1);
                int src = Array.IndexOf(R16, op2);

                if (dst >= 0 && src >= 0)
                {
                    return new List<byte>
                    {
                        0x01,
                        (byte)(0xC0 | (src << 3) | dst)
                    };
                }
            }

            if (mnemonic == "cmp")
            {
                int r = Array.IndexOf(R16, op1);

                if (r >= 0)
                {
                    int value = pass2 ? Resolve(op2) : 0;

                    return new List<byte>
                    {
                        0x81,
                        (byte)(0xF8 | r),
                        (byte)value,
                        (byte)(value >> 8)
                    };
                }
            }

            throw new Exception("Unsupported instruction: " + line);
        }

        public byte[] Assemble(string source)
        {
            string[] lines = source.Replace("\r", "").Split('\n');

            labels.Clear();

            // PASS 1: Calculate label addresses

            int addr = 0x100;

            foreach (string raw in lines)
            {
                string line = RemoveComment(raw).Trim();

                if (line.Length == 0)
                    continue;

                if (line.Contains(":"))
                {
                    string label = line.Substring(
                        0, line.IndexOf(':')).Trim();

                    labels[label] = addr;
                }

                if (line.StartsWith("org ",
                    StringComparison.OrdinalIgnoreCase))
                {
                    addr = ParseNumber(line.Split(
                        new char[] { ' ', '\t' },
                        StringSplitOptions.RemoveEmptyEntries)[1]);

                    continue;
                }

                addr += Encode(line, addr, false).Count;
            }

            // PASS 2: Generate machine code

            addr = 0x100;

            List<byte> machineCode = new List<byte>();

            foreach (string raw in lines)
            {
                string line = RemoveComment(raw).Trim();

                if (line.Length == 0)
                    continue;

                if (line.StartsWith("org ",
                    StringComparison.OrdinalIgnoreCase))
                {
                    addr = ParseNumber(line.Split(
                        new char[] { ' ', '\t' },
                        StringSplitOptions.RemoveEmptyEntries)[1]);

                    continue;
                }

                List<byte> bytes = Encode(line, addr, true);

                machineCode.AddRange(bytes);

                addr += bytes.Count;
            }

            return machineCode.ToArray();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.BackgroundColor= ConsoleColor.White;
            Console.ForegroundColor= ConsoleColor.Black;
            Console.Clear();
            Console.Title = "x86 Virtual Machine & Assembly Studio";

            Console.WriteLine("========================================");
            Console.WriteLine("   x86 VIRTUAL MACHINE & ASSEMBLY STUDIO");
            Console.WriteLine("========================================");
            Console.WriteLine("16-bit 8086 Real Mode DOS Environment");
            Console.WriteLine();

            if (args.Length < 1)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("X86Studio program.asm [program.com]");
                return;
            }

            string inputFile = args[0];

            if (!File.Exists(inputFile))
            {
                Console.WriteLine("File not found: " + inputFile);
                return;
            }

            try
            {
                string source = File.ReadAllText(inputFile);

                Assembler8086 assembler = new Assembler8086();

                CPU8086 cpu = new CPU8086();

                Console.WriteLine("Source: " + inputFile);

                byte[] binary = assembler.Assemble(source);

                if (binary.Length == 0)
                    throw new Exception("No machine code generated.");

                Console.WriteLine("Assembled: " + binary.Length + " bytes");

                Console.WriteLine("Loading COM image at 0700:0100...");
                
                if (args.Length >= 2)
                {
                    File.WriteAllBytes(args[1], binary);

                    Console.WriteLine("COM saved: " + args[1]);
                }

                cpu.LoadCOM(binary);

                Console.WriteLine();
                Console.WriteLine("EXECUTION");
                Console.WriteLine("----------------------------------------");

                int maxInstructions = 1000000;

                while (!cpu.Halted && cpu.Cycles < maxInstructions)
                {
                    cpu.Step();
                }

                if (!cpu.Halted)
                    Console.WriteLine("\n[Execution limit reached]");

                Console.WriteLine();
                Console.WriteLine("DOS STANDARD OUTPUT");
                Console.WriteLine("----------------------------------------");

                Console.Write(cpu.Output.ToString());

                Console.WriteLine();
                Console.WriteLine();

                cpu.ShowRegisters();

                cpu.ShowVGA();

                Console.WriteLine();
                Console.WriteLine("Execution completed.");

            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("[ERROR]");
                Console.WriteLine(ex.Message);
            }
        }
    }
}
