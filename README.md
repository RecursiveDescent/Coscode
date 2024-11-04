# Coscode

A custom stack-based bytecode format focused on being useful and closer to a geniune executable format.

Should be compatible with [COSMOS](https://github.com/CosmosOS/Cosmos/), but currently untested.

This format is incomplete and the specification is subject to change drastically in the future.

*Keep in mind that just because the example language compiler does not support a feature yet, doesn't mean the bytecode is incapable of it. The language itself is also incomplete.*

# Features

- [x] Relatively memory efficient
- [x] Realistic layout. There are separate sections for code/data/etc.
- [x] Utility class to assist in creating bytecode executables.
- [x] Basic language that compiles to bytecode.

# Planned

- [ ] Redesign header to allow custom sections.
- [ ] Define debug information format.
- [ ] Implement debugger.
- [ ] Basic JIT capability. (No guarantee that this is possible, but not going to try unless it has a high demand)

## Format

### Header
| Value | Size |
|---|---|
|Code start  | 8 bytes|
|Code size   | 8 bytes|
|String start| 8 bytes|
|String size | 8 bytes|
|Function table start| 8 bytes|
|Function table size | 8 bytes|

### Opcodes
Each opcode with an argument reads an 8 byte value, otherwise the instruction is a single byte.

| Opcode         | Value | Description                                 | Parameter Size    |
|----------------|-------|---------------------------------------------|-------------------|
| NOP            | 0     | No operation                                | None              |
| CALL           | 1     | Call a function                             | 8 bytes  (Index into function table)         |
| CALL_NATIVE    | 2     | Call a native function                      | 8 bytes (Index of native function)          |
| PUSHUI32       | 3     | Push a 32-bit unsigned integer              | 8 bytes           |
| PUSHUI64       | 4     | Push a 64-bit unsigned integer              | 8 bytes           |
| PUSHSTR        | 5     | Push a string                               | 8 bytes (Index into string table)          |
| ADD            | 6     | Add top two stack values                    | None              |
| SUB            | 7     | Subtract top two stack values               | None              |
| MUL            | 8     | Multiply top two stack values               | None              |
| DIV            | 9     | Divide top two stack values                 | None              |
| RETURN         | 10    | Return from a function                      | None              |
| JE             | 11    | Jump if equal (Top of stack is 0)           | 8 bytes           |
| JNE            | 12    | Jump if not equal (Top of stack is not 0)   | 8 bytes           |
| JMP            | 13    | Unconditional jump                          | 8 bytes           |
| GT             | 14    | Greater than comparison                     | None              |
| LT             | 15    | Less than comparison                        | None              |
| CMP            | 16    | Compare top two stack values                | None              |
| BIT_NEGATE     | 17    | Bitwise negate                              | None              |
| BIT_AND        | 18    | Bitwise AND                                 | None              |
| BIT_OR         | 19    | Bitwise OR                                  | None              |
| BIT_XOR        | 20    | Bitwise XOR                                 | None              |
| BIT_SHL        | 21    | Bitwise shift left                          | None              |
| BIT_SHR        | 22    | Bitwise shift right                         | None              |
| CREATE_OBJ     | 23    | Create object value                         | None              |
| OBJ_SET        | 24    | Set object member                           | 8 bytes (member slot) |
| OBJ_GET        | 25    | Get object member                           | 8 bytes (member slot) |
| LOAD_0         | 100   | Load value from slot 0                      | None              |
| ...            |101-109|                                             | None              |
| LOAD_END       | 110   | End of load instructions                    | None              |
| LOAD_EXT       | 111   | Load value from slot (extended)             | 8 bytes (slot)    |
| STORE          | 112   | Store value to slot 0                       | None              |
| ...            |113-121|                                             | None              |
| STORE_END      | 122   | End of store instructions                   | None              |
| STORE_EXT      | 123   | Store value to slot (extended)              | 8 bytes (slot)    |
...


