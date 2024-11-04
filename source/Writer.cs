using System;
using System.IO;

namespace Coscode.Writer {
    public class DeferredWrite {
        public BinaryWriter Writer;

        public long Location;

        public byte Op;

        public long? Arg;

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

    public class CCWriter {
        public MemoryStream Out = new MemoryStream();

        private BinaryWriter OutWriter;

        private BinaryWriter Code = new BinaryWriter(new MemoryStream());

        // Function table
        private BinaryWriter Funcs = new BinaryWriter(new MemoryStream());

        // Strings
        private BinaryWriter Strings = new BinaryWriter(new MemoryStream());

        /// <summary>
        /// Gets the current position of the code stream.
        /// </summary>
        /// <returns>The current position of the code stream.</returns>
        public long Loc() {
            return Code.BaseStream.Position;
        }

        /// <summary>
        /// Starts a new function definition.
        /// </summary>
        /// <param name="name">The name of the function.</param>
        /// <returns>The position of the function entry in the function table.</returns>
        public long StartFunction(string name) {
            long loc = Code.BaseStream.Position;

            long pos = Funcs.BaseStream.Position;

            // Write null terminated string
            foreach (char c in name) {
                Funcs.Write(c);
            }

            Funcs.Write((byte) 0);

            // Write function location
            Funcs.Write(loc);

            return pos;
        }

        /// <summary>
        /// Creates a deferred instruction that can be written later.
        /// This may be necessary if for example, you need to emit a jump instruction to a location later in the generated code.
        /// </summary>
        /// <param name="ins">The instruction opcode.</param>
        /// <param name="arg">The instruction argument (optional).</param>
        /// <returns>A DeferredWrite object representing the deferred instruction.</returns>
        public DeferredWrite DeferredInstruction(byte ins, long? arg = null) {
            DeferredWrite write = new DeferredWrite(Code);

            write.Location = Code.BaseStream.Position;

            write.Op = ins;

            write.Arg = arg;

            Code.BaseStream.Seek(arg != null ? 9 : 1, SeekOrigin.Current);

            return write;
        }

        /// <summary>
        /// Writes a single-byte instruction to the code stream.
        /// </summary>
        /// <param name="ins">The instruction opcode.</param>
        /// <returns>The position of the instruction in the code stream.</returns>
        public long Instruction(byte ins) {
            long pos = Code.BaseStream.Position;
            
            Code.Write(ins);

            return pos;
        }

        /// <summary>
        /// Writes an instruction with an argument to the code stream.
        /// </summary>
        /// <param name="ins">The instruction opcode.</param>
        /// <param name="arg">The instruction argument.</param>
        /// <returns>The position of the instruction in the code stream.</returns>
        public long Instruction(byte ins, long arg) {
            long pos = Code.BaseStream.Position;
            
            Code.Write(ins);

            Code.Write(arg);

            return pos;
        }

        /// <summary>
        /// Adds a string to the string table.
        /// </summary>
        /// <param name="str">The string to add.</param>
        /// <returns>A reference to the string in the string table.</returns>
        /// <remarks>Strings are stored as null-terminated strings inside the string table.</remarks>
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
        /// <summary>
        /// Finishes the compilation and writes the code to the output stream.
        /// </summary>
        public void Finish() {
            long hsize = 64;

            // Start of code section (Offset from start of file)
            OutWriter.Write(hsize);

            // Size of code section
            OutWriter.Write(Code.BaseStream.Length);

            // Start of string table
            OutWriter.Write(hsize + Code.BaseStream.Length);

            // Size of string table
            OutWriter.Write(Strings.BaseStream.Length);

            // Start of function table
            OutWriter.Write(hsize + Code.BaseStream.Length + Strings.BaseStream.Length);

            // Size of function table
            OutWriter.Write(Funcs.BaseStream.Length + 1);

            Out.Seek(hsize, SeekOrigin.Begin);

            Code.BaseStream.Seek(0, SeekOrigin.Begin);

            Code.BaseStream.CopyTo(Out);

            // Put string table at end of code section
            Strings.BaseStream.Seek(0, SeekOrigin.Begin);

            Strings.BaseStream.CopyTo(Out);

            // Put function table at end of strings
            Funcs.BaseStream.Seek(0, SeekOrigin.Begin);

            Funcs.BaseStream.CopyTo(Out);

            Out.WriteByte(0); // Terminate function table with a null byte
        }

        /// <summary>
        /// Convert the output stream into bytes.
        /// </summary>
        /// <returns>A byte array containing the compiled code.</returns>
        public byte[] GetBytes() {
            return Out.GetBuffer();
        }

        public CCWriter() {
            OutWriter = new BinaryWriter(Out);
        }
    }
}