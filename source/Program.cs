using System;
using System.IO;
using System.Reflection;

namespace Coscode {
    

    public class Program
    {
        public static void Print(VM vm) {
            Console.WriteLine(vm.Stack.Pop());
        }

        public static void Main(string[] args)
        {
            Writer wr = new Writer();

            long str = wr.AddString("Hello World!");

            /*wr.Instruction((byte) Opcode.PUSHSTR, str);

            wr.Instruction((byte) Opcode.STORE);

            wr.Instruction((byte) Opcode.LOAD);

            wr.Instruction((byte) Opcode.PUSHSTR, str);

            wr.Instruction((byte) Opcode.CMP);*/

            wr.Instruction((byte) Opcode.PUSHUI32, 1000);

            wr.Instruction((byte) Opcode.PUSHUI32, 704);

            wr.Instruction((byte) Opcode.ADD);

            wr.Instruction((byte) Opcode.PUSHUI32, 1704);

            wr.Instruction((byte) Opcode.GT);

            wr.Instruction(2, 0);

            wr.Finish();

            VM vm = new VM(wr.GetBytes());

            vm.Load();

            vm.NativeFuncs.Add(Print);

            vm.FrameStack.Push(new Frame());

            vm.Step();

            vm.Step();

            vm.Step();

            vm.Step();

            vm.Step();

            vm.Step();
        }
    }
}