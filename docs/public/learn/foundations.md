# Foundations: structure, output, values, and variables

Every LOLCODE program starts with `HAI 1.2` and ends with `KTHXBYE`. Between
them, statements run top to bottom. `VISIBLE` sends values to the terminal:

```lolcode
HAI 1.2
  VISIBLE "HAI WORLD!"
  VISIBLE 42
KTHXBYE
```

A quoted text value is a **YARN**; whole numbers are **NUMBR**, decimal numbers
are **NUMBAR**, and `WIN` or `FAIL` are **TROOF**. A variable gives a value a
name, so later instructions can use or replace it:

```lolcode
I HAS A name ITZ "LOLCATZ"
I HAS A age ITZ 9
age R 10
VISIBLE name " IZ " age
```

`I HAS A` declares a variable, `ITZ` initializes it, and `R` assigns a new
value. A declaration without `ITZ` holds `NOOB`, the language's uninitialized
value. Explore the complete
[`variables.lol`](../../../samples/basics/variables/variables.lol) sample.

> [!TIP]
> `VISIBLE` accepts several values and joins their display forms. It normally
> ends the line; `VISIBLE "prompt:: " !` writes a literal colon and intentionally
> does not end the line.

**Checkpoint:** Print your name and a favorite number, then update the number
and print it again. Next, learn how values can drive a decision.
