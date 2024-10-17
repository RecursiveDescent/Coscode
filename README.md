# Coscode

A bytecode format I started for fun.

Should be compatible with [COSMOS](https://github.com/CosmosOS/Cosmos/), but currently untested.

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
| Opcode | Description | Argument |
|---|---|---|
| 0x0 | No operation |  |
| 0x6 | Add |  |
| 0x7 | Sub |  |
| 0x8 | Mul |  |
| 0x9 | Div |  |
...


