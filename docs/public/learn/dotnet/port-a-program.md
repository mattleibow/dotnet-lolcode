# Port a program by preserving behavior

Porting is not replacing tokens one for one. First identify inputs, outputs,
state, decisions, iteration, and function contracts. Then express those
behaviors with LOLCODE's runtime rules.

## Source program

Imagine this C# method and caller:

```csharp
static string Classify(int value) =>
    value % 2 == 0 ? "EVEN" : "ODD";

Console.WriteLine(Classify(7));
```

## Step 1: state the contract

- Input: one whole number.
- Output: YARN `"EVEN"` or `"ODD"`.
- Decision: remainder after division by two equals zero.
- No hidden state or exceptional input in this narrow function.

## Step 2: translate the operations

```lolcode
HAI 1.2
  HOW IZ I classify YR value
    BOTH SAEM MOD OF value AN 2 AN 0
    O RLY?
      YA RLY, FOUND YR "EVEN"
      NO WAI, FOUND YR "ODD"
    OIC
  IF U SAY SO

  VISIBLE I IZ classify YR 7 MKAY
KTHXBYE
```

Expected output:

```text
ODD
```

The remainder expression is nested inside equality. That bare comparison puts
its TROOF result in function `IT`, which `O RLY?` consumes.

## Step 3: add the host boundary

If the value comes from `GIMMEH`, it begins as YARN. Convert it before the
call. Decide how malformed input should behave rather than relying on explicit
cast-to-zero behavior.

If the classification must remain in an existing C#, VB, or F# library, use an
eligible public static adapter. Keep complex types behind the adapter and pass
only supported primitive/object values. Avoid overloads. For VB/F# especially,
verify the emitted CLR surface instead of assuming source-language constructs
map directly to importable methods.

## Step 4: test boundaries

Test `0`, `1`, `2`, `-1`, and `-2`. Then compare the original and ported
observable outputs. If negative remainder behavior or invalid input matters,
record it explicitly in both test suites.

## Porting checklist

- Replace static-type assumptions with explicit runtime-kind decisions.
- Convert input at a boundary, not opportunistically inside arithmetic.
- Rewrite infix operators as prefix expressions and make grouping visible.
- Pass every function dependency as a parameter.
- Treat `IT` as scoped mutable runtime state.
- Review equality and switch identity.
- Model BUKKITs as prototypes, not CLR classes.
- Preserve multi-file initialization order.
- Add adapters for ineligible managed signatures.
- Keep `HAI` version, SDK/package version, and host/runtime version as separate
  compatibility facts.

Continue with the [language tour](../../language/tour.md) or build a mixed
solution using [projects and interoperability](workflows-and-interop.md).
