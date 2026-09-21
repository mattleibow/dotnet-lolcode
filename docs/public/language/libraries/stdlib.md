# STDLIB library

<span class="badge badge-reference">1.4 reference behavior</span>

`STDLIB` supplies a per-import pseudo-random generator.

```lolcode
HAI 1.4
  CAN HAS STDLIB?
  I IZ STDLIB'Z MIX YR 123 MKAY
  VISIBLE I IZ STDLIB'Z BLOW YR 10 MKAY
KTHXBYE
```

| Slot | Result |
| --- | --- |
| `MIX YR seed` | reseeds this import's generator; returns NOOB |
| `BLOW YR maximum` | NUMBR from zero (inclusive) to maximum (exclusive); returns 0 when maximum is zero or negative |

State belongs to the imported library context. A separate import/module gets a
separate generator; state is not a process-global random singleton. Reseeding
with the same value makes subsequent values reproducible within the same
runtime implementation, which is useful for tests but not cryptography.

Provider implementation classes are internal and are not API contracts.
