# Repetition and text: loops, strings, and switches

A loop repeats instructions while a condition gives it a reason to continue.
This counter prints 0 through 4; `TIL` stops when its condition becomes true:

```lolcode
IM IN YR count UPPIN YR i TIL BOTH SAEM i AN 5
  VISIBLE i
IM OUTTA YR count
```

`UPPIN` increments, `NERFIN` decrements, and `WILE` continues while its
condition is true. `GTFO` exits the nearest loop (or switch). The named loop
opening and closing must agree. Practice with
[`loops.lol`](../../../samples/basics/loops/loops.lol).

YARN is text. Combine fragments with `SMOOSH ... MKAY`, interpolate a variable
as `:{name}`, and escape special characters with `:`: `:)` newline, `:>` tab,
`::` colon, and `:"` quote. See
[`strings.lol`](../../../samples/basics/string-ops/strings.lol).

When a value has several fixed cases, `WTF?` is clearer than nested branches:

```lolcode
day
WTF?
  OMG 1
    VISIBLE "MUNDAI"
    GTFO
  OMGWTF
    VISIBLE "DUNNO"
OIC
```

Cases fall through unless `GTFO` stops them. `OMG` labels must be literals and
unique. **Checkpoint:** print a growing row of stars in a loop, then write a
switch that names a weekday. Next, package logic into functions.
