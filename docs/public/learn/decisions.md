# 04 Decisions and program flow

**Prerequisite:** input, variables, casts, and arithmetic.

**Outcome:** create TROOF results, branch with `O RLY?`, combine conditions,
and choose among fixed cases with `WTF?`.

## A decision selects which instructions run

A comparison produces `WIN` or `FAIL`. Put that result in the implicit
per-scope value `IT`, then `O RLY?` chooses a branch:

```lolcode
HAI 1.2
  I HAS A score ITZ 82
  BOTH SAEM BIGGR OF score AN 60 AN score
  O RLY?
    YA RLY
      VISIBLE "PASS"
    NO WAI
      VISIBLE "TRY AGAIN"
  OIC
KTHXBYE
```

Expected output:

```text
PASS
```

The expression asks whether `score` is the larger of `score` and `60`, which
means it is at least 60. A **bare expression statement** updates `IT`.
Assignment does not. In stable 1.2, conditional and switch declarations plus
their final `IT` remain in the enclosing scope. The 1.3/1.4 progression uses
child scopes instead; use the [implementation profile](../reference/implementation-profile.md)
when mixing versions.

## Equality, logic, and identity

`BOTH SAEM` checks equality; `DIFFRINT` checks inequality. It does not turn
unlike primitive types into the same type, so `"3"` and `3` are different.
Boolean operators convert operands to TROOF:

- `NOT value`
- `BOTH OF left AN right`
- `EITHER OF left AN right`
- `WON OF left AN right` (exactly one)
- variadic `ALL OF ... MKAY` and `ANY OF ... MKAY`

NOOB becomes `FAIL` when tested for truth, but its other implicit conversions
are errors.

## More than two fixed choices

```lolcode
HAI 1.2
  I HAS A command ITZ "look"
  command
  WTF?
    OMG "look"
      VISIBLE "U SEE A DOOR"
      GTFO
    OMG "open"
      VISIBLE "IT IZ LOCKD"
      GTFO
    OMGWTF
      VISIBLE "DUNNO DAT COMMAND"
  OIC
KTHXBYE
```

Expected output:

```text
U SEE A DOOR
```

`WTF?` reads `IT`. `OMG` labels are unique literals. Cases fall through into
the next case unless `GTFO` exits the switch, so use that behavior deliberately.
Switch matching uses exact runtime type and value identity.

## Practice: predict, run, explain, modify, create

1. **Predict:** Does `BOTH SAEM "5" AN 5` produce WIN or FAIL?
2. **Run:** Print it and confirm the types matter.
3. **Explain:** Why must an input YARN be cast before numeric equality?
4. **Modify:** Add a `"help"` case to the command switch.
5. **Create:** Ask for a temperature, convert it, and print `COLD`, `WARM`, or
   `HOT` using nested decisions.

<details>
<summary>Hint and explained solution</summary>

```lolcode
HAI 1.2
  I HAS A temp
  VISIBLE "TEMPERATURE? " !
  GIMMEH temp
  temp IS NOW A NUMBR

  BOTH SAEM SMALLR OF temp AN 10 AN temp
  O RLY?
    YA RLY
      VISIBLE "COLD"
    NO WAI
      BOTH SAEM BIGGR OF temp AN 25 AN temp
      O RLY?
        YA RLY, VISIBLE "HOT"
        NO WAI, VISIBLE "WARM"
      OIC
  OIC
KTHXBYE
```

The first condition recognizes values at or below 10. Only its `NO WAI` path
needs to ask whether the value is at or above 25.
</details>

## Common mistakes

- Expecting `O RLY?` to read a named variable automatically; it reads `IT`.
- Assigning a condition to a variable immediately before `O RLY?`; assignment
  does not update `IT`. Put the variable as a bare expression.
- Treating `"3"` and `3` as equal.
- Forgetting `GTFO` in a switch and unintentionally falling through.
- Using expressions as `OMG` labels; labels must be literals.

## Checkpoint

You can trace which branch runs and explain why. Compare with the maintained
[conditionals](../../../samples/basics/conditionals/conditionals.lol) and
[switch](../../../samples/basics/switch/switch.lol) samples. Next:
[repeat work and build text](repetition-and-text.md).
