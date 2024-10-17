using System;
using System.IO;

namespace Coscode {
    public class DeferredWrite {
        public BinaryWriter Writer;

        public long Location;

        public byte Op;

        public ulong? Arg;

        public void Write() {
            long pos = Writer.BaseStream.Position;

            Writer.BaseStream.Seek(Location, SeekOrigin.Begin);

            Writer.Write(Op);

            if (Arg != null)
                Writer.Write((int) Arg);

            Writer.BaseStream.Seek(pos, SeekOrigin.Begin);
        }

        public DeferredWrite(BinaryWriter writer) {
            Writer = writer;
        }
    }

    public class Writer {
        public MemoryStream Out = new MemoryStream();

        private BinaryWriter OutWriter;

        private BinaryWriter Code = new BinaryWriter(new MemoryStream());

        // Function table
        private BinaryWriter Funcs = new BinaryWriter(new MemoryStream());

        // Strings
        private BinaryWriter Strings = new BinaryWriter(new MemoryStream());

        public long Loc() {
            return Code.BaseStream.Position;
        }

        public void AddFunction(string name, long loc) {
            // Write null terminated string

            foreach (char c in name) {
                Funcs.Write(c);
            }

            Funcs.Write((byte) 0);

            // Write function location
            Funcs.Write(loc);
        }

        public DeferredWrite DeferredInstruction(byte ins, ulong? arg = null) {
            DeferredWrite write = new DeferredWrite(Code);

            write.Location = Code.BaseStream.Position;

            write.Op = ins;

            write.Arg = arg;

            Code.BaseStream.Seek(arg != null ? 9 : 1, SeekOrigin.Current);

            return write;
        }

        public long Instruction(byte ins) {
            long pos = Code.BaseStream.Position;
            
            Code.Write(ins);

            return pos;
        }

        public long Instruction(byte ins, long arg) {
            long pos = Code.BaseStream.Position;
            
            Code.Write(ins);

            Code.Write(arg);

            return pos;
        }

        public long AddString(string str) {
            long pos = Strings.BaseStream.Position;

            Strings.Write(str.ToCharArray());

            Strings.Write((byte) 0);

            return pos;
        }

        /*
                    Header Format
            
            Code section start: 8 bytes

            Size of code section: 8 bytes

            Start of string table: 8 bytes
        */
        public void Finish() {
            // Start of code section (Offset from start of file)
            OutWriter.Write(64ul);

            // Size of code section
            OutWriter.Write(Code.BaseStream.Length);

            // Start of string table
            OutWriter.Write(64 + Code.BaseStream.Length);

            Out.Seek(64, SeekOrigin.Begin);

            Code.BaseStream.Seek(0, SeekOrigin.Begin);

            Code.BaseStream.CopyTo(Out);

            // Put string table at end of node section
            Strings.BaseStream.Seek(0, SeekOrigin.Begin);

            Strings.BaseStream.CopyTo(Out);
        }

        public byte[] GetBytes() {
            return Out.GetBuffer();
        }

        public Writer() {
            OutWriter = new BinaryWriter(Out);
        }
    }
}