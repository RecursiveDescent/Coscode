using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Coscode {
    public enum Opcode {
        NOP = 0,
        CALL = 1,
        CALL_NATIVE = 2,
        PUSHUI32 = 3,
        PUSHUI64 = 4,
        PUSHSTR = 5,

        ADD = 6,
        SUB = 7,
        MUL = 8,
        DIV = 9,

        RETURN = 10,

        JE = 11,
        JNE = 12,
        JMP = 13,

        GT = 14,
        LT = 15,
        CMP = 16,

        // Bitwise operations
        BIT_NEGATE = 17,
        BIT_AND = 18,
        BIT_OR = 19,
        BIT_XOR = 20,
        BIT_SHL = 21,
        BIT_SHR = 22,
        
        // Range of load instructions
        LOAD = 100,

        LOAD_END = 110,

        LOAD_EXT = 111,

        // Same thing for store
        STORE = 112,

        STORE_END = 122,

        STORE_EXT = 123
    }

    public enum VType {
        Bool,
        U32,
        U64,
        String,
        Ref
    }

    // This is a union type used to save memory space.
    #pragma warning disable CS0660
    #pragma warning disable CS0661
    [StructLayout(LayoutKind.Explicit)]
	public struct ValueUnion {
        [FieldOffset(0)]
		public bool Bool = false;
		[FieldOffset(0)]
		public float Float = 0;
		[FieldOffset(0)]
		public uint UInt = 0;
        [FieldOffset(0)]
		public ulong ULong = 0;

        public static bool operator ==(ValueUnion left, ValueUnion right) {
            return left.ULong == right.ULong;
        }

        public static bool operator !=(ValueUnion left, ValueUnion right) {
            return left.ULong != right.ULong;
        }

        public override string ToString() {
            return $"ValueUnion{{ Bool = {Bool}, Float = {Float}, UInt = {UInt}, ULong = {ULong} }}";
        }

        public ValueUnion(bool b) {
            Bool = b;
        }

        public ValueUnion(float f) {
            Float = f;
        }

        public ValueUnion(uint u) {
            UInt = u;
        }

        public ValueUnion(ulong ul) {
            ULong = ul;
        }
	}

    public class Value {
        public VType Type;

        // Strings are represented as a list to avoid allocations.
        public List<char> StrData = null;

        public ValueUnion Data;

        public Value Ref = null;

        public override string ToString()
        {
            return $"{Type}({(StrData != null ? '"' + new string(StrData.ToArray()) + '"' : Data)})";
        }

        public bool Equals(Value right) {
            if (right == null)
                return false;

            if (Type == VType.String) {
                if (StrData.Count != right.StrData.Count)
                    return false;
                
                for (int i = 0; i < StrData.Count; i++) {
                    if (StrData[i] != right.StrData[i])
                        return false;
                }

                return true;
            }

            return Data == right.Data && Ref == right.Ref;
        }

        public Value Add(Value right) {
            switch (Type) {
                case VType.U32:
                    return new Value(VType.U32, new ValueUnion(Data.UInt + right.Data.UInt));
                case VType.U64:
                    return new Value(VType.U64, new ValueUnion(Data.ULong + right.Data.ULong));
                case VType.String:
                    List<char> newStr = new List<char>(StrData);

                    newStr.AddRange(right.StrData);

                    return new Value(VType.String, newStr);
                default:
                    throw new Exception("Invalid type for addition");
            }
        }

        public Value Sub(Value right) {
            switch (Type) {
                case VType.U32:
                    return new Value(VType.U32, new ValueUnion(Data.UInt - right.Data.UInt));
                case VType.U64:
                    return new Value(VType.U64, new ValueUnion(Data.ULong - right.Data.ULong));
                default:
                    throw new Exception("Invalid type for subtraction");
            }
        }

        public Value Mul(Value right) {
            switch (Type) {
                case VType.U32:
                    return new Value(VType.U32, new ValueUnion(Data.UInt * right.Data.UInt));
                case VType.U64:
                    return new Value(VType.U64, new ValueUnion(Data.ULong * right.Data.ULong));
                default:
                    throw new Exception("Invalid type for multiplication");
            }
        }

        public Value Div(Value right) {
            switch (Type) {
                case VType.U32:
                    return new Value(VType.U32, new ValueUnion(Data.UInt / right.Data.UInt));
                case VType.U64:
                    return new Value(VType.U64, new ValueUnion(Data.ULong / right.Data.ULong));
                default:
                    throw new Exception("Invalid type for division");
            }
        }

        public Value GT(Value right) {
            switch (Type) {
                case VType.U32:
                    return new Value(VType.Bool, new ValueUnion(Data.UInt > right.Data.UInt));
                case VType.U64:
                    return new Value(VType.Bool, new ValueUnion(Data.ULong > right.Data.ULong));
                default:
                    throw new Exception("Invalid type for >");
            }
        }

        public Value LT(Value right) {
            switch (Type) {
                case VType.U32:
                    return new Value(VType.Bool, new ValueUnion(Data.UInt < right.Data.UInt));
                case VType.U64:
                    return new Value(VType.Bool, new ValueUnion(Data.ULong < right.Data.ULong));
                default:
                    throw new Exception("Invalid type for <division>");
            }
        }

        public Value(VType t, ValueUnion value) {
            Type = t;

            Data = value;
        }

        public Value(VType t, List<char> value) {
            Type = t;

            StrData = value;
        }
    }

    /// <summary>
    /// VM stack frame representation
    /// </summary>
    public class Frame {
        /// <summary>
        /// Stack slots for storing variables.
        /// </summary>
        public Value[] Slots = new Value[20];

        /// <summary>
        /// The return address used to jump back to the caller.
        /// </summary>
        public long Return = 0;

        public Frame() {}

        public Frame(long ret) {
            Return = ret;
        }
    }

    public class CCVM {
        private long PC = 0;

        private long Code = 0;

        private long CodeSize = 0;

        private long Strings = 0;

        private long Funcs = 0;

        private BinaryReader Data;

        public Stack<Frame> FrameStack = new Stack<Frame>();

        /// <summary>
        /// The main stack used for VM operations.
        /// </summary>
        public Stack<Value> Stack = new Stack<Value>();

        /// <summary>
        /// Table of function names to their respective offsets.
        /// </summary>
        public Dictionary<string, long> FuncTable = new Dictionary<string, long>();

        /// <summary>
        /// List of native functions available to the VM.
        /// 
        /// CALL_NATIVE will index into this list to find the native function to call.
        /// </summary>
        public List<Action<CCVM>> NativeFuncs = new List<Action<CCVM>>();

        private void Jump(long offset) {
            PC = Code + offset;

            Data.BaseStream.Seek(PC, SeekOrigin.Begin);
        }

        private List<char> ReadString(long offset) {
            long pos = Data.BaseStream.Position;

            Data.BaseStream.Seek(Strings + offset, SeekOrigin.Begin);

            // Save memory by using a list
            List<char> chars = new List<char>();

            while (Data.PeekChar() != 0) {
                chars.Add(Data.ReadChar());
            }

            Data.BaseStream.Seek(pos, SeekOrigin.Begin);

            return chars;
        }

        private long ReadFunc(long offset) {
            long pos = Data.BaseStream.Position;

            Data.BaseStream.Seek(Funcs + offset, SeekOrigin.Begin);

            while (Data.PeekChar() != 0) {
                Data.ReadChar();
            }

            Data.ReadChar();

            long func = Data.ReadInt64();

            Data.BaseStream.Seek(pos, SeekOrigin.Begin);

            return func;
        }

        public void Step() {
            byte op = Data.ReadByte();

            Value left;

            Value right;

            switch ((Opcode) op) {
                case Opcode.NOP:
                    break;
                case Opcode.PUSHUI32:
                    Stack.Push(new Value(VType.U32, new ValueUnion((uint) Data.ReadUInt64())));
                    break;
                case Opcode.PUSHUI64:
                    Stack.Push(new Value(VType.U64, new ValueUnion(Data.ReadUInt64())));
                    break;
                case Opcode.PUSHSTR:
                    Stack.Push(new Value(VType.String, ReadString(Data.ReadInt64())));

                    break;
                case Opcode.CALL:
                    long func = ReadFunc(Data.ReadInt64());

                    FrameStack.Push(new Frame(Data.BaseStream.Position));

                    Data.BaseStream.Seek(Code + func, SeekOrigin.Begin);

                    break;
                case Opcode.CALL_NATIVE:
                    int i = (int) Data.ReadUInt64();

                    if (i >= NativeFuncs.Count)
                        throw new Exception($"Invalid native function index {i}");

                    NativeFuncs[i](this);

                    break;
                case Opcode.RETURN:
                    Data.BaseStream.Seek(FrameStack.Pop().Return, SeekOrigin.Begin);

                    break;
                case Opcode.ADD:
                    right = Stack.Pop();

                    left = Stack.Pop();

                    Stack.Push(left.Add(right));

                    break;
                case Opcode.SUB:
                    right = Stack.Pop();

                    left = Stack.Pop();

                    Stack.Push(left.Sub(right));

                    break;
                case Opcode.MUL:
                    right = Stack.Pop();

                    left = Stack.Pop();

                    Stack.Push(left.Mul(right));

                    break;
                case Opcode.DIV:
                    right = Stack.Pop();

                    left = Stack.Pop();

                    Stack.Push(left.Div(right));

                    break;
                case Opcode.JMP:
                    Jump(Data.ReadInt64());
                    break;
                case Opcode.JE:
                    if (Stack.Pop().Data.Bool)
                        Jump(Data.ReadInt64());
                    
                    break;

                case Opcode.JNE:
                    if (! Stack.Pop().Data.Bool)
                        Jump(Data.ReadInt64());
                    
                    break;
                case Opcode.GT:
                    right = Stack.Pop();

                    left = Stack.Pop();

                    Stack.Push(left.GT(right));

                    break;
                case Opcode.LT:
                    right = Stack.Pop();

                    left = Stack.Pop();

                    Stack.Push(left.LT(right));

                    break;
                case Opcode.CMP:
                    right = Stack.Pop();

                    left = Stack.Pop();

                    Stack.Push(new Value(VType.Bool, new ValueUnion(left.Equals(right))));

                    break;
                case Opcode.BIT_XOR:
                    right = Stack.Pop();

                    left = Stack.Pop();

                    Stack.Push(new Value(VType.U64, new ValueUnion(left.Data.ULong ^ right.Data.ULong)));

                    break;

                case Opcode.BIT_NEGATE:
                    left = Stack.Pop();

                    Stack.Push(new Value(VType.U64, new ValueUnion(~left.Data.ULong)));

                    break;
                case Opcode.BIT_AND:
                    right = Stack.Pop();

                    left = Stack.Pop();

                    Stack.Push(new Value(VType.U64, new ValueUnion(left.Data.ULong & right.Data.ULong)));

                    break;
                default:
                    break;
            }

            if (op >= (byte) Opcode.LOAD && op < (byte) Opcode.LOAD_END) {
                if (FrameStack.Count == 0)
                    throw new Exception("No stack frame available to load from.");

                Frame f = FrameStack.Peek();

                Stack.Push(f.Slots[op - (byte) Opcode.LOAD]);
            }

            if (op >= (byte) Opcode.STORE && op < (byte) Opcode.STORE_END) {
                if (FrameStack.Count == 0)
                    throw new Exception("No stack frame available to store to.");

                Frame f = FrameStack.Peek();

                f.Slots[op - (byte) Opcode.STORE] = Stack.Pop();
            }

            PC = Data.BaseStream.Position;
        }

        public void Load() {
            long code = Data.ReadInt64();

            long size = Data.ReadInt64();

            long strings = Data.ReadInt64();

            Data.ReadInt64(); // Ignore strings size

            long funcs = Data.ReadInt64();

            Code = code;

            CodeSize = size;

            Strings = strings;

            Funcs = funcs;

            PC = code;

            // Resolve functions
            Data.BaseStream.Seek(funcs, SeekOrigin.Begin);

            while (Data.PeekChar() != 0) {
                List<char> name = new List<char>();

                while (Data.PeekChar() != 0) {
                    name.Add(Data.ReadChar());
                }

                Data.ReadChar();

                FuncTable[new string(name.ToArray())] = Data.ReadInt64();
            }

            Data.BaseStream.Seek(PC, SeekOrigin.Begin);
        }

        public void Run() {
            while (PC < Code + CodeSize - 1)
                Step();
        }

        public void DebugPrintCodeOps() {
            long loc = Data.BaseStream.Position;

            Data.BaseStream.Seek(Code, SeekOrigin.Begin);

            while (Data.BaseStream.Position < Code + CodeSize) {
                byte op = Data.ReadByte();

                if (op != (byte) Opcode.NOP)
                    Console.Write($"{Data.BaseStream.Position - Code - 1}: ");

                if (op >= (byte) Opcode.LOAD && op <= (byte) Opcode.LOAD_END) {
                    Console.WriteLine($"LOAD_{op - (byte) Opcode.LOAD}");

                    continue;
                }

                if (op >= (byte) Opcode.STORE && op <= (byte) Opcode.STORE_END) {
                    Console.WriteLine($"STORE_{op - (byte) Opcode.STORE}");

                    continue;
                }

                switch (op) {
                    case (byte) Opcode.NOP:
                        break;
                    case (byte) Opcode.PUSHUI32:
                        Console.WriteLine($"PUSHUI32 {Data.ReadInt64()}");
                        break;
                    case (byte) Opcode.PUSHUI64:
                        Console.WriteLine($"PUSHUI64 {Data.ReadInt64()}");
                        break;
                    case (byte) Opcode.PUSHSTR:
                        long str = Data.ReadInt64();

                        Console.WriteLine($"PUSHSTR ({str})[\"{new string(ReadString(str).ToArray())}\"]");
                        break;
                    case (byte) Opcode.JE:
                        Console.WriteLine($"JE {Data.ReadUInt64()}");

                        break;
                    case (byte) Opcode.JMP:
                        Console.WriteLine($"JMP {Data.ReadUInt64()}");

                        break;
                    case (byte) Opcode.ADD:
                        Console.WriteLine("ADD");
                        break;
                    case (byte) Opcode.SUB:
                        Console.WriteLine("SUB");
                        break;
                    case (byte) Opcode.MUL:
                        Console.WriteLine("MUL");
                        break;
                    case (byte) Opcode.DIV:
                        Console.WriteLine("DIV");
                        break;
                    case (byte) Opcode.CALL:
                        Console.WriteLine($"CALL ({Data.ReadInt64()})");
                        break;
                    case (byte) Opcode.CALL_NATIVE:
                        Console.WriteLine($"CALL_NATIVE {Data.ReadInt64()}");
                        break;
                    default:
                        Console.WriteLine($"{(Opcode) op}");
                        break;
                }
            }

            Data.BaseStream.Seek(loc, SeekOrigin.Begin);
        }

        public CCVM(Stream data) {
            Data = new BinaryReader(data);
        }
        
        public CCVM(byte[] data) {
            Data = new BinaryReader(new MemoryStream(data));
        }
    }
}