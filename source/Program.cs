using System;
using System.IO;
using System.Reflection;
using Coscode.Writer;
using Coscode.Assembler;

namespace Coscode {
    

    public class Program
    {
        public static void Print(CCVM vm) {
            Console.WriteLine(vm.Stack.Pop());
        }

        public static void Main(string[] args)
        {
            CCWriter wr = new CCWriter();

            // long str = wr.AddString("Hello World!");

            // DeferredWrite skip = wr.DeferredInstruction((byte) Opcode.JMP, 0);

            // long prntref = wr.StartFunction("Prnt");

            // wr.Instruction((byte) Opcode.PUSHSTR, str);

           //  wr.Instruction((byte) Opcode.CALL_NATIVE, 0);

            // wr.Instruction((byte) Opcode.RETURN);

            /*long mainref = wr.StartFunction("main");

            long main = wr.Instruction((byte) Opcode.CALL, prntref);

            skip.Arg = main;

            skip.Write();

            wr.Instruction((byte) Opcode.PUSHUI32, 1704);

            wr.Instruction((byte) Opcode.CALL_NATIVE, 0);*/

            /*wr.Instruction((byte) Opcode.PUSHSTR, str);

            wr.Instruction((byte) Opcode.STORE);

            wr.Instruction((byte) Opcode.LOAD);

            wr.Instruction((byte) Opcode.PUSHSTR, str);

            wr.Instruction((byte) Opcode.CMP);*/

            // wr.Finish();

            CCCompiler compiler = new CCCompiler(new StreamReader("factorial_example.cc").ReadToEnd());

            compiler.Compile();

            CCVM vm = new CCVM(compiler.Output.GetBytes());

            vm.Load();

            vm.NativeFuncs.Add(Print);

            vm.FrameStack.Push(new Frame());

            vm.DebugPrintCodeOps();

            Console.WriteLine("-------------------------------------------------");

            vm.Run();
        }
    }
}