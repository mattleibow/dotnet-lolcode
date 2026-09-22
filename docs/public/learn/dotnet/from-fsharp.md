# Coming from F#

## Dynamic state rather than expression-first typing

F# habits are valuable—small functions and explicit transformation—but several
core assumptions do not carry over:

| F# intuition | LOLCODE correction |
| --- | --- |
| bindings are immutable by default | LOLCODE bindings are mutable with `R` |
| sequencing returns the final expression | bare expressions update `IT`; many statements do not |
| functions curry and close over lexical values | LOLCODE functions have declared arity and cannot capture outer variables |
| records/unions model data | BUKKIT is a dynamic prototype object, not a record or discriminated union |
| pattern matching decomposes shapes | `WTF?` matches literal runtime identity and falls through |
| file order is explicit in the project | LOLCODE also preserves `Compile` initialization order, while direct top-level functions are hoisted only for multi-tree compilations |

## Function and interop shape

An F# function such as `let add x y = x + y` commonly compiles to a curried
FSharp.Core function shape, not an eligible public library instance method.
F# modules, records, unions, options, tuples, and async workflows often expose
framework-specific CLR types.

Do not claim direct F# `CAN HAS` support. Add a C# adapter with non-overloaded
public instance methods and supported parameter/result types when necessary.

## Sequencing example

F#:

```fsharp
let twice value = value * 2
printfn "%d" (twice 21)
```

LOLCODE:

```lolcode
HOW IZ I twice YR value
  FOUND YR PRODUKT OF value AN 2
IF U SAY SO
VISIBLE I IZ twice YR 21 MKAY
```

In LOLCODE, parentheses are not the call grammar, and the function cannot close
over surrounding values. Continue to [port a program](port-a-program.md).
