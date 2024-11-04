using System;
using System.Collections.Generic;
using System.IO;
using Coscode.Writer;

namespace Coscode.Assembler {
    public class CCToken {
        public string Type;

        public string Value;

        public bool Is(string t, string v) {
            return Type == t && Value == v;
        }

        public CCToken(string type, string value) {
            Type = type;
            Value = value;
        }
    }

    public class CCTokenizer {
        private string Source;

        private int Pos = 0;

        private char Get() {
            if (Pos >= Source.Length)
                return '\0';

            return Source[Pos++];
        }

        private char Peek() {
            if (Pos >= Source.Length)
                return '\0';

            return Source[Pos];
        }

        private bool IsAlpha(char c) {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        private bool IsDigit(char c) {
            return c >= '0' && c <= '9';
        }

        private bool IsAlphaNumeric(char c) {
            return IsAlpha(c) || IsDigit(c);
        }

        private bool IsWhitespace(char c) {
            return c == ' ' || c == '\t' || c == '\n' || c == '\r';
        }

        public CCToken GetToken() {
            while (IsWhitespace(Peek()))
                Get();

            if (IsAlpha(Peek())) {
                string value = "";

                while (IsAlphaNumeric(Peek()))
                    value += Get();

                return new CCToken("Identifier", value);
            }

            if (IsDigit(Peek())) {
                string value = "";

                while (IsDigit(Peek()))
                    value += Get();

                return new CCToken("Number", value);
            }

            if (Peek() == '"') {
                Get();

                string value = "";

                while (Peek() != '"')
                    value += Get();

                Get();

                return new CCToken("String", value);
            }

            if (Peek() == '/') {
                Get();

                if (Peek() == '/') {
                    Get();

                    while (Peek() != '\n')
                        Get();

                    return GetToken();
                }
            }

            if (Peek() == '=') {
                Get();

                if (Peek() == '=') {
                    Get();

                    return new CCToken("Operator", "==");
                }

                return new CCToken("Operator", "=");
            }

            if (Peek() == '!') {
                Get();

                if (Peek() == '=') {
                    Get();

                    return new CCToken("Operator", "!=");
                }

                return new CCToken("Operator", "!");
            }

            if (Peek() == '>')
                return new CCToken("Operator", Get().ToString());

            if (Peek() == '<')
                return new CCToken("Operator", Get().ToString());

            if (Peek() == '+')
                return new CCToken("Operator", Get().ToString());

            if (Peek() == '-')
                return new CCToken("Operator", Get().ToString());

            if (Peek() == '*')
                return new CCToken("Operator", Get().ToString());

            if (Peek() == '/')
                return new CCToken("Operator", Get().ToString());

            if (Peek() == ',')
                return new CCToken("Comma", Get().ToString());

            if (Peek() == ';')
                return new CCToken("Semicolon", Get().ToString());

            if (Peek() == ':')
                return new CCToken("Colon", Get().ToString());

            if (Peek() == '(')
                return new CCToken("LParen", Get().ToString());

            if (Peek() == ')')
                return new CCToken("RParen", Get().ToString());
            
            if (Peek() == '{')
                return new CCToken("LBrace", Get().ToString());

            if (Peek() == '}')
                return new CCToken("RBrace", Get().ToString());

            if (Peek() == '\0')
                return new CCToken("EOF", "");

            return new CCToken("Unknown", Get().ToString());
        }

        public CCToken PeekToken(int i = 1) {
            int pos = Pos;

            for (i -= 1; i > 0; i--)
                GetToken();

            CCToken t = GetToken();

            Pos = pos;

            return t;
        }

        public CCToken Expect(string type) {
            CCToken t = GetToken();

            if (t.Type != type)
                throw new Exception($"Expected {type} but got {t.Type}");

            return t;
        }

        public CCTokenizer(string source) {
            Source = source;
        }
    }

    public class Instruction {
        public string Name;

        public CCASTNode Arg = null;

        public Instruction(string name) {
            Name = name;
        }

        public Instruction(string name, CCASTNode arg) {
            Name = name;
            Arg = arg;
        }
    }

    public class CCASTNode {
        public string Type;

        public CCToken Value = null;

        public List<CCASTNode> Children = new List<CCASTNode>();

        public CCASTNode(string type) {
            Type = type;
        }

        public CCASTNode(string type, CCToken value) {
            Type = type;

            Value = value;
        }
    }

    public class CCParser {
        private CCTokenizer Lexer;

        public CCASTNode TopLevel() {
            if (Lexer.PeekToken(2).Is("Colon", ":") || Lexer.PeekToken(2).Is("LBrace", "{")) {
                CCASTNode func = new CCASTNode("Function");

                func.Value = Lexer.GetToken();

                CCToken colonorbrace = Lexer.GetToken();

                CCASTNode args = new CCASTNode("ArgDefs");

                if (colonorbrace.Type == "Colon") {
                    while (! Lexer.PeekToken().Is("LBrace", "{")) {
                        args.Children.Add(new CCASTNode("Arg", Lexer.GetToken()));

                        if (Lexer.PeekToken().Is("Comma", ","))
                            Lexer.GetToken();

                        if (Lexer.PeekToken().Type == "EOF")
                            throw new Exception("Unexpected end of file");
                    }

                    Lexer.GetToken();
                }

                func.Children.Add(args);

                CCASTNode block = new CCASTNode("Block");

                while (! Lexer.PeekToken().Is("RBrace", "}")) {
                    block.Children.Add(Statement());

                    if (Lexer.PeekToken().Is("Semicolon", ";"))
                        Lexer.GetToken();

                    if (Lexer.PeekToken().Type == "EOF")
                        throw new Exception("Unexpected end of file");
                }

                Lexer.Expect("RBrace");

                func.Children.Add(block);

                return func;
            }

            return null;
        }

        public CCASTNode Statement() {
            if (Lexer.PeekToken().Is("Identifier", "if")) {
                Lexer.GetToken();

                CCASTNode node = new CCASTNode("If");

                node.Children.Add(LogicalCmp());

                Lexer.Expect("LBrace");

                CCASTNode block = new CCASTNode("Block");

                while (! Lexer.PeekToken().Is("RBrace", "}")) {
                    block.Children.Add(Statement());

                    if (Lexer.PeekToken().Is("Semicolon", ";"))
                        Lexer.GetToken();

                    if (Lexer.PeekToken().Type == "EOF")
                        throw new Exception("Unexpected end of file");
                }

                Lexer.Expect("RBrace");

                node.Children.Add(block);

                return node;
            }

            // Merge with if statement logic in the future?
            if (Lexer.PeekToken().Is("Identifier", "while")) {
                Lexer.GetToken();

                CCASTNode node = new CCASTNode("While");

                node.Children.Add(LogicalCmp());

                Lexer.Expect("LBrace");

                CCASTNode block = new CCASTNode("Block");

                while (! Lexer.PeekToken().Is("RBrace", "}")) {
                    block.Children.Add(Statement());

                    if (Lexer.PeekToken().Is("Semicolon", ";"))
                        Lexer.GetToken();

                    if (Lexer.PeekToken().Type == "EOF")
                        throw new Exception("Unexpected end of file");
                }

                Lexer.Expect("RBrace");

                node.Children.Add(block);

                return node;
            }

            return Expression();
        }

        public CCASTNode LogicalCmp() {
            CCASTNode left = Expression();

            while (Lexer.PeekToken().Is("Operator", "==") || Lexer.PeekToken().Is("Operator", "!=")
                   || Lexer.PeekToken().Is("Operator", ">") || Lexer.PeekToken().Is("Operator", "<")) {
                CCToken op = Lexer.GetToken();

                string type = op.Value == "==" ? "Equal" : "NotEqual";

                if (op.Value == ">")
                    type = "GreaterThan";

                if (op.Value == "<")
                    type = "LessThan";

                CCASTNode node = new CCASTNode(type);

                node.Children.Add(left);

                node.Children.Add(Expression());

                left = node;
            }

            return left;
        }

        public CCASTNode Expression() {
            if (Lexer.PeekToken().Is("Identifier", "return")) {
                Lexer.GetToken();

                CCASTNode node = new CCASTNode("Return");

                node.Children.Add(Expression());

                return node;
            }

            return Add();
        }

        public CCASTNode Add() {
            CCASTNode left = Sub();

            while (Lexer.PeekToken().Is("Operator", "+")) {
                Lexer.GetToken();

                CCASTNode node = new CCASTNode("Add");

                node.Children.Add(left);

                node.Children.Add(Sub());

                left = node;
            }

            return left;
        }

        public CCASTNode Sub() {
            CCASTNode left = Mul();

            while (Lexer.PeekToken().Is("Operator", "-")) {
                Lexer.GetToken();

                CCASTNode node = new CCASTNode("Sub");

                node.Children.Add(left);

                node.Children.Add(Mul());

                left = node;
            }

            return left;
        }

        public CCASTNode Mul() {
            CCASTNode left = Div();

            while (Lexer.PeekToken().Is("Operator", "*")) {
                Lexer.GetToken();

                CCASTNode node = new CCASTNode("Mul");

                node.Children.Add(left);

                node.Children.Add(Div());

                left = node;
            }

            return left;
        }

        public CCASTNode Div() {
            CCASTNode left = Unary();

            while (Lexer.PeekToken().Is("Operator", "/")) {
                Lexer.GetToken();

                CCASTNode node = new CCASTNode("Div");

                node.Children.Add(left);

                node.Children.Add(Unary());

                left = node;
            }

            return left;
        }

        public CCASTNode Unary() {
            if (Lexer.PeekToken().Is("Operator", "!")) {
                Lexer.GetToken();

                CCASTNode node = new CCASTNode("Not");

                node.Children.Add(Unary());

                return node;
            }

            return Primary();
        }

        public CCASTNode Primary() {
            CCToken token = Lexer.GetToken();

            if (token.Is("LParen", "(")) {
                CCASTNode node = LogicalCmp();

                Lexer.Expect("RParen");

                return node;
            }

            if (Lexer.PeekToken().Is("LParen", "(")) {
                Lexer.GetToken();

                List<CCASTNode> args = new List<CCASTNode>();

                while (! Lexer.PeekToken().Is("RParen", ")")) {
                    args.Add(Expression());

                    if (Lexer.PeekToken().Is("Comma", ","))
                        Lexer.GetToken();

                    if (Lexer.PeekToken().Type == "EOF")
                        throw new Exception("Unexpected end of file");
                }

                Lexer.Expect("RParen");

                CCASTNode call = new CCASTNode("Call", token);

                foreach (CCASTNode arg in args)
                    call.Children.Add(arg);

                return call;
            }

            return new CCASTNode("Primary", token);
        }

        public CCParser(string source) {
            Lexer = new CCTokenizer(source);
        }
    }

    public class CCCompiler {
        private CCParser Parser;

        public CCWriter Output = new CCWriter();

        public Dictionary<string, long> Functions = new Dictionary<string, long>();

        public Dictionary<string, long> FunctionCode = new Dictionary<string, long>();

        private CCASTNode CurrentFunc = null;

        public void Compile(CCASTNode node) {
            if (node.Type == "Function") {
                long fn = Output.StartFunction(node.Value.Value);

                Functions[node.Value.Value] = fn;

                FunctionCode[node.Value.Value] = Output.Loc();

                CurrentFunc = node;

                // Store args
                for (int i = node.Children[0].Children.Count; i > 0; i--) {
                    Output.Instruction((byte) (Opcode.STORE + i - 1));
                }

                // Compile block
                foreach (CCASTNode child in node.Children[1].Children) {
                    Compile(child);
                }

                // Force return even if there isn't an explicit return
                if (node.Value.Value != "main")
                    Output.Instruction((byte) Opcode.RETURN);

                CurrentFunc = null;

                return;
            }

            if (node.Type == "If" || node.Type == "While") {
                long loopstart = Output.Loc();

                Compile(node.Children[0]);

                DeferredWrite skipjmp = Output.DeferredInstruction((byte) Opcode.JE, 0);

                DeferredWrite skipblock = Output.DeferredInstruction((byte) Opcode.JMP, 0);

                skipjmp.Arg = Output.Loc();

                skipjmp.Write();

                foreach (CCASTNode child in node.Children[1].Children) {
                    Compile(child);
                }

                if (node.Type == "While") {
                    Output.Instruction((byte) Opcode.JMP, loopstart);
                }

                skipblock.Arg = Output.Loc();

                skipblock.Write();

                return;
            }

            if (node.Type == "Call") {
                foreach (CCASTNode arg in node.Children) {
                    Compile(arg);
                }

                if (node.Value.Value == "print") {
                    Output.Instruction((byte) Opcode.CALL_NATIVE, 0);

                    return;
                }

                if (node.Value.Value == "readi32") {
                    Output.Instruction((byte) Opcode.CALL_NATIVE, 1);

                    return;
                }

                Output.Instruction((byte) Opcode.CALL, Functions[node.Value.Value]);

                return;
            }

            if (node.Type == "Return") {
                Compile(node.Children[0]);

                Output.Instruction((byte) Opcode.RETURN);

                return;
            }

            if (node.Type == "Equal") {
                Compile(node.Children[0]);

                Compile(node.Children[1]);

                Output.Instruction((byte) Opcode.CMP);

                return;
            }

            if (node.Type == "NotEqual") {
                Compile(node.Children[0]);

                Compile(node.Children[1]);

                Output.Instruction((byte) Opcode.CMP);

                // Bitwise logical negation
                Output.Instruction((byte) Opcode.BIT_NEGATE);

                Output.Instruction((byte) Opcode.PUSHUI32, 1);

                Output.Instruction((byte) Opcode.BIT_AND);

                return;
            }

            if (node.Type == "GreaterThan") {
                Compile(node.Children[0]);

                Compile(node.Children[1]);

                Output.Instruction((byte) Opcode.GT);

                return;
            }

            if (node.Type == "LessThan") {
                Compile(node.Children[0]);

                Compile(node.Children[1]);

                Output.Instruction((byte) Opcode.LT);

                return;
            }

            if (node.Type == "Not") {
                Compile(node.Children[0]);

                Output.Instruction((byte) Opcode.BIT_NEGATE);

                Output.Instruction((byte) Opcode.PUSHUI32, 1);

                Output.Instruction((byte) Opcode.BIT_AND);

                return;
            }

            if (node.Type == "Add") {
                Compile(node.Children[0]);

                Compile(node.Children[1]);

                Output.Instruction((byte) Opcode.ADD);

                return;
            }

            if (node.Type == "Sub") {
                Compile(node.Children[0]);

                Compile(node.Children[1]);

                Output.Instruction((byte) Opcode.SUB);

                return;
            }

            if (node.Type == "Mul") {
                Compile(node.Children[0]);

                Compile(node.Children[1]);

                Output.Instruction((byte) Opcode.MUL);

                return;
            }

            if (node.Type == "Div") {
                Compile(node.Children[0]);

                Compile(node.Children[1]);

                Output.Instruction((byte) Opcode.DIV);

                return;
            }

            if (node.Type == "Primary") {
                if (node.Value.Type == "Number") {
                    Output.Instruction((byte) Opcode.PUSHUI64, long.Parse(node.Value.Value));

                    return;
                }

                if (node.Value.Type == "String") {
                    Output.Instruction((byte) Opcode.PUSHSTR, Output.AddString(node.Value.Value));

                    return;
                }

                if (node.Value.Type == "Identifier") {
                    if (CurrentFunc != null) {
                        CCASTNode fargs = CurrentFunc.Children[0];

                        for (int i = 0; i < fargs.Children.Count; i++) {
                            if (fargs.Children[i].Value.Value == node.Value.Value) {
                                Output.Instruction((byte) (Opcode.LOAD + i));

                                return;
                            }
                        }
                    }
                }
            }
        }

        public void Compile() {
            DeferredWrite skiptomain = Output.DeferredInstruction((byte) Opcode.JMP, 0);

            Console.WriteLine(Output.Out.Position);

            while (true) {
                CCASTNode next = Parser.TopLevel();

                if (next == null)
                    break;

                Compile(next);
            }

            skiptomain.Arg = FunctionCode["main"];

            skiptomain.Write();

            Output.Finish();
        }

        public CCCompiler(string source) {
            Parser = new CCParser(source);
        }
    }
}