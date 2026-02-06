# LOLCODE 1.2 Language Specification

This document defines the LOLCODE 1.2 language as implemented by the dotnet-lolcode compiler. It is based on the [original LOLCODE spec](https://github.com/justinmeza/lolcode-spec/blob/master/v1.2/lolcode-spec-v1.2.md) with clarifications and implementation-specific notes.

## Table of Contents

- [Program Structure](#program-structure)
- [Whitespace and Line Endings](#whitespace-and-line-endings)
- [Comments](#comments)
- [Variables](#variables)
- [Types](#types)
- [Literals](#literals)
- [Operators](#operators)
- [Input/Output](#inputoutput)
- [Conditionals](#conditionals)
- [Switch (WTF?)](#switch-wtf)
- [Loops](#loops)
- [Functions](#functions)
- [The IT Variable](#the-it-variable)
- [Type Casting](#type-casting)
- [String Operations](#string-operations)
- [Boolean Operations](#boolean-operations)
- [Comparison](#comparison)
- [Keywords Reference](#keywords-reference)

---

## Program Structure

Every LOLCODE program must begin with `HAI` and end with `KTHXBYE`:

```lolcode
HAI 1.2
  BTW your code here
KTHXBYE
```

The version number (`1.2`) after `HAI` is optional but recommended.

## Whitespace and Line Endings

- Statements are **newline-delimited** (no semicolons)
- A comma (`,`) can be used as a **statement separator** on a single line
- Lines can be continued with `...` (line continuation, must be at end of line)
- Leading/trailing whitespace on lines is ignored
- Indentation is optional and for readability only

```lolcode
HAI 1.2
  I HAS A x ITZ 10, I HAS A y ITZ 20    BTW two statements on one line
  VISIBLE SUM OF x...
    AN y                                   BTW line continuation
KTHXBYE
```

## Comments

### Single-line comments

```lolcode
BTW This is a comment
VISIBLE "hai" BTW inline comment
```

`BTW` makes the rest of the line a comment.

### Multi-line comments

```lolcode
OBTW
  This is a multi-line comment.
  It can span many lines.
TLDR
```

`OBTW` must be the first token on a line. `TLDR` must also be the first token on its line.

## Variables

### Declaration

```lolcode
I HAS A var              BTW declares var with value NOOB
I HAS A var ITZ 42       BTW declares and initializes to NUMBR 42
I HAS A var ITZ "hai"    BTW declares and initializes to YARN "hai"
```

### Assignment

```lolcode
var R 100                BTW assign 100 to var
var R "string"           BTW assign "string" to var
var R SUM OF 3 AN 5      BTW assign expression result to var
```

### Rules
- Variable names may contain letters, numbers, and underscores
- Variable names must start with a letter
- Variables are **function-scoped** (not block-scoped)
- All variables start as `NOOB` (untyped/null) if not initialized

## Types

LOLCODE has five types:

| Type | Description | .NET Equivalent | Example |
|------|------------|----------------|---------|
| `NUMBR` | Integer | `int` (Int32) | `42`, `-7`, `0` |
| `NUMBAR` | Floating-point | `double` (Double) | `3.14`, `-0.5` |
| `YARN` | String | `string` | `"HAI"`, `""` |
| `TROOF` | Boolean | `bool` | `WIN`, `FAIL` |
| `NOOB` | Untyped/null | `null` | (default for uninitialized vars) |

### Type Inference
Variables are **dynamically typed** — their type is determined by the assigned value and can change at any time.

## Literals

### Integer (NUMBR)
```lolcode
42
-7
0
```

### Float (NUMBAR)
```lolcode
3.14
-0.5
100.0
```

### String (YARN)
Strings are enclosed in double quotes:
```lolcode
"HAI WORLD"
""
```

**Escape sequences** (using `:` as escape character):

| Escape | Character |
|--------|-----------|
| `:)` | Newline (`\n`) |
| `:>` | Tab (`\t`) |
| `::` | Literal colon (`:`) |
| `:"` | Literal double quote (`"`) |
| `:o` | Bell character (0x07) |
| `:(XX)` | Unicode character by hex code |
| `:[name]` | Unicode character by name |

### Boolean (TROOF)
```lolcode
WIN                      BTW true
FAIL                     BTW false
```

### NOOB
There is no literal for `NOOB` — it is only the default value of uninitialized variables.

## Operators

All operators use **prefix notation** with `AN` separating operands.

### Arithmetic

| Operator | Meaning | Example |
|----------|---------|---------|
| `SUM OF x AN y` | Addition (x + y) | `SUM OF 3 AN 5` → 8 |
| `DIFF OF x AN y` | Subtraction (x - y) | `DIFF OF 10 AN 3` → 7 |
| `PRODUKT OF x AN y` | Multiplication (x * y) | `PRODUKT OF 4 AN 5` → 20 |
| `QUOSHUNT OF x AN y` | Division (x / y) | `QUOSHUNT OF 10 AN 3` → 3 |
| `MOD OF x AN y` | Modulo (x % y) | `MOD OF 10 AN 3` → 1 |
| `BIGGR OF x AN y` | Maximum | `BIGGR OF 3 AN 7` → 7 |
| `SMALLR OF x AN y` | Minimum | `SMALLR OF 3 AN 7` → 3 |

**Type rules:**
- If both operands are `NUMBR`, result is `NUMBR`
- If either operand is `NUMBAR`, result is `NUMBAR`
- `YARN` operands are cast to numeric types before operation
- Integer division truncates toward zero

### Operators are nestable:
```lolcode
SUM OF PRODUKT OF 3 AN 4 AN 5   BTW (3 * 4) + 5 = 17
```

## Input/Output

### Output (VISIBLE)
```lolcode
VISIBLE "HAI WORLD!"             BTW prints with newline
VISIBLE "NO NEWLINE"!            BTW prints without newline (! suppresses)
VISIBLE x                        BTW prints variable value
VISIBLE "x is " x " and y is " y  BTW concatenates and prints
```

Multiple arguments to `VISIBLE` are concatenated with no separator.

### Input (GIMMEH)
```lolcode
GIMMEH var                       BTW reads a line of input into var as YARN
```

`GIMMEH` always reads input as a `YARN` (string). Cast afterwards if needed:
```lolcode
GIMMEH x
x IS NOW A NUMBR                BTW cast to integer
```

## Conditionals

### If/Else (O RLY?)

The `O RLY?` construct tests the value of `IT` (the implicit variable):

```lolcode
expression                       BTW result goes to IT
O RLY?
  YA RLY                         BTW if IT is WIN (truthy)
    BTW true branch
  MEBBE other_condition           BTW else if (optional, repeatable)
    BTW else-if branch
  NO WAI                          BTW else (optional)
    BTW false branch
OIC                               BTW end if
```

**Truthiness:**
- `WIN` → truthy
- `FAIL` → falsy
- `NOOB` → falsy
- `NUMBR 0` → falsy, nonzero → truthy
- `NUMBAR 0.0` → falsy, nonzero → truthy
- `YARN ""` → falsy, non-empty → truthy

### Shorthand
```lolcode
BOTH SAEM x AN 5, O RLY?
  YA RLY, VISIBLE "x is 5"
  NO WAI, VISIBLE "x is not 5"
OIC
```

## Switch (WTF?)

Tests `IT` against literal values:

```lolcode
x                                BTW result goes to IT
WTF?
  OMG 1                          BTW case 1
    VISIBLE "one"
    GTFO                         BTW break (fall-through without GTFO)
  OMG 2                          BTW case 2
    VISIBLE "two"
    GTFO
  OMG 3                          BTW case 3 (falls through to default)
  OMGWTF                         BTW default case
    VISIBLE "other"
OIC
```

- `OMG` cases compare `IT` to a literal value
- Without `GTFO`, execution **falls through** to the next case
- `OMGWTF` is the default case (optional)

## Loops

```lolcode
IM IN YR label [operation YR var] [condition]
  BTW loop body
IM OUTTA YR label
```

### Components:
- **label** — loop identifier (used to match `IM IN YR` with `IM OUTTA YR`)
- **operation** — `UPPIN` (increment) or `NERFIN` (decrement)
- **var** — loop variable (auto-declared if not existing)
- **condition** — `TIL expr` (loop until true) or `WILE expr` (loop while true)

### Examples:

```lolcode
BTW count from 0 to 9
IM IN YR loop UPPIN YR i TIL BOTH SAEM i AN 10
  VISIBLE i
IM OUTTA YR loop

BTW infinite loop (break with GTFO)
IM IN YR loop
  GIMMEH input
  BOTH SAEM input AN "quit", O RLY?
    YA RLY, GTFO
  OIC
IM OUTTA YR loop

BTW count down from 10 to 1
IM IN YR countdown NERFIN YR i TIL BOTH SAEM i AN 0
  VISIBLE i
IM OUTTA YR countdown
```

### Loop Control:
- `GTFO` — break out of the current loop

## Functions

### Declaration:
```lolcode
HOW IZ I functionName [YR param1 [AN YR param2 ...]]
  BTW function body
  FOUND YR returnValue             BTW return a value
IF U SAY SO
```

### Calling:
```lolcode
I IZ functionName [YR arg1 [AN YR arg2 ...]] MKAY
```

The return value of a function call is stored in `IT`.

### Example:
```lolcode
HOW IZ I add YR a AN YR b
  FOUND YR SUM OF a AN b
IF U SAY SO

I IZ add YR 3 AN YR 5 MKAY
VISIBLE IT                         BTW prints 8
```

### Rules:
- Functions must be declared before they are called
- Parameters are passed by value
- A function without `FOUND YR` returns `NOOB`
- `GTFO` inside a function returns `NOOB` (like a void return)

## The IT Variable

`IT` is a special implicit variable:
- Every expression result is automatically stored in `IT`
- `O RLY?` and `WTF?` test `IT` by default
- `IT` can be used explicitly like any other variable
- Each scope has its own `IT`

```lolcode
SUM OF 3 AN 5                    BTW IT is now 8
VISIBLE IT                        BTW prints 8
O RLY?                            BTW tests IT (which is 8, truthy)
  YA RLY, VISIBLE "truthy!"
OIC
```

## Type Casting

### Expression cast (doesn't modify variable):
```lolcode
MAEK var A NUMBR                 BTW cast var to NUMBR (expression)
MAEK var A YARN                  BTW cast var to YARN
```

### In-place cast (modifies variable):
```lolcode
var IS NOW A NUMBR               BTW cast var in-place to NUMBR
var IS NOW A YARN                BTW cast var in-place to YARN
```

### Casting rules:

| From → To | Result |
|-----------|--------|
| `NUMBR` → `YARN` | String representation (`42` → `"42"`) |
| `NUMBAR` → `YARN` | String representation (`3.14` → `"3.14"`) |
| `TROOF` → `YARN` | `"WIN"` or `"FAIL"` |
| `YARN` → `NUMBR` | Parse integer (fail → 0) |
| `YARN` → `NUMBAR` | Parse float (fail → 0.0) |
| `YARN` → `TROOF` | `""` → `FAIL`, non-empty → `WIN` |
| `NUMBR` → `TROOF` | `0` → `FAIL`, nonzero → `WIN` |
| `NUMBAR` → `TROOF` | `0.0` → `FAIL`, nonzero → `WIN` |
| `TROOF` → `NUMBR` | `WIN` → `1`, `FAIL` → `0` |
| `NOOB` → any | Default value for target type |

## String Operations

### Concatenation (SMOOSH):
```lolcode
SMOOSH "HAI " AN var AN "!" MKAY
SMOOSH x AN y MKAY
```

- All operands are implicitly cast to `YARN`
- `MKAY` terminates the argument list
- Result is a `YARN`

## Boolean Operations

| Operator | Meaning | Example |
|----------|---------|---------|
| `BOTH OF x AN y` | AND | `BOTH OF WIN AN FAIL` → `FAIL` |
| `EITHER OF x AN y` | OR | `EITHER OF WIN AN FAIL` → `WIN` |
| `WON OF x AN y` | XOR | `WON OF WIN AN WIN` → `FAIL` |
| `NOT x` | NOT | `NOT WIN` → `FAIL` |
| `ALL OF x AN y ... MKAY` | N-ary AND | `ALL OF WIN AN WIN AN WIN MKAY` → `WIN` |
| `ANY OF x AN y ... MKAY` | N-ary OR | `ANY OF FAIL AN FAIL AN WIN MKAY` → `WIN` |

Operands are implicitly cast to `TROOF` before evaluation.

## Comparison

| Operator | Meaning | Example |
|----------|---------|---------|
| `BOTH SAEM x AN y` | Equality (x == y) | `BOTH SAEM 3 AN 3` → `WIN` |
| `DIFFRINT x AN y` | Inequality (x != y) | `DIFFRINT 3 AN 5` → `WIN` |

**Greater than / less than** (idiomatic):
```lolcode
BOTH SAEM x AN BIGGR OF x AN y  BTW x >= y
BOTH SAEM x AN SMALLR OF x AN y BTW x <= y
DIFFRINT x AN SMALLR OF x AN y  BTW x > y
DIFFRINT x AN BIGGR OF x AN y   BTW x < y
```

## Keywords Reference

### Program Structure
| Keyword | Purpose |
|---------|---------|
| `HAI` | Program start |
| `KTHXBYE` | Program end |

### Variables
| Keyword | Purpose |
|---------|---------|
| `I HAS A` | Variable declaration |
| `ITZ` | Initialization |
| `R` | Assignment |
| `IT` | Implicit result variable |

### I/O
| Keyword | Purpose |
|---------|---------|
| `VISIBLE` | Print output |
| `GIMMEH` | Read input |

### Arithmetic
| Keyword | Purpose |
|---------|---------|
| `SUM OF ... AN` | Addition |
| `DIFF OF ... AN` | Subtraction |
| `PRODUKT OF ... AN` | Multiplication |
| `QUOSHUNT OF ... AN` | Division |
| `MOD OF ... AN` | Modulo |
| `BIGGR OF ... AN` | Maximum |
| `SMALLR OF ... AN` | Minimum |

### Boolean
| Keyword | Purpose |
|---------|---------|
| `BOTH OF ... AN` | AND |
| `EITHER OF ... AN` | OR |
| `WON OF ... AN` | XOR |
| `NOT` | NOT |
| `ALL OF ... MKAY` | N-ary AND |
| `ANY OF ... MKAY` | N-ary OR |

### Comparison
| Keyword | Purpose |
|---------|---------|
| `BOTH SAEM ... AN` | Equality |
| `DIFFRINT ... AN` | Inequality |

### Conditionals
| Keyword | Purpose |
|---------|---------|
| `O RLY?` | Begin conditional (tests IT) |
| `YA RLY` | True branch |
| `NO WAI` | Else branch |
| `MEBBE` | Else-if branch |
| `OIC` | End conditional/switch |

### Switch
| Keyword | Purpose |
|---------|---------|
| `WTF?` | Begin switch (tests IT) |
| `OMG` | Case label |
| `OMGWTF` | Default case |
| `GTFO` | Break |

### Loops
| Keyword | Purpose |
|---------|---------|
| `IM IN YR` | Begin loop |
| `IM OUTTA YR` | End loop |
| `UPPIN` | Increment operation |
| `NERFIN` | Decrement operation |
| `YR` | Parameter/variable marker |
| `TIL` | Until condition |
| `WILE` | While condition |

### Functions
| Keyword | Purpose |
|---------|---------|
| `HOW IZ I` | Begin function declaration |
| `IF U SAY SO` | End function declaration |
| `I IZ ... MKAY` | Function call |
| `FOUND YR` | Return value |

### Types & Casting
| Keyword | Purpose |
|---------|---------|
| `NUMBR` | Integer type |
| `NUMBAR` | Float type |
| `YARN` | String type |
| `TROOF` | Boolean type |
| `NOOB` | Untyped/null type |
| `MAEK` | Expression cast |
| `IS NOW A` | In-place cast |

### Strings
| Keyword | Purpose |
|---------|---------|
| `SMOOSH ... MKAY` | String concatenation |

### Comments
| Keyword | Purpose |
|---------|---------|
| `BTW` | Single-line comment |
| `OBTW` | Begin multi-line comment |
| `TLDR` | End multi-line comment |

### Misc
| Keyword | Purpose |
|---------|---------|
| `AN` | Operand separator |
| `MKAY` | Expression list terminator |
| `WIN` | Boolean true |
| `FAIL` | Boolean false |
| `,` | Statement separator |
| `...` | Line continuation |
| `!` | Suppress newline (after VISIBLE) |
