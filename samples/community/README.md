# Community Compatibility Samples

This collection tracks representative programs from prominent LOLCODE
community references. Runnable adaptations are file-based apps and are covered
by the sample integration tests.

## Esolang

Source revision:
[LOLCODE oldid 174739](https://esolangs.org/w/index.php?title=LOLCODE&oldid=174739).
Esolang wiki content is dedicated under
[CC0 1.0](https://esolangs.org/w/index.php?title=Esolang:Copyrights&oldid=30226).

| Community example | Repository sample | Status |
|-------------------|-------------------|--------|
| Truth-Machine | [truth-machine.lol](esolangs/truth-machine.lol) | Ported to 1.2 and corrected to compare input with `"1"` because `GIMMEH` returns a YARN |
| Hello World | [basic hello world](../basics/hello-world/hello.lol) | Already covered |
| Loops | [basic loops](../basics/loops/loops.lol) | Already covered |

The original Truth-Machine uses a 1.3 typed default and tests the input YARN
directly. Under standard YARN truthiness, the nonempty input `"0"` is true, so
this port uses an explicit comparison.

## Wikipedia

Source revision:
[LOLCODE oldid 1365801280](https://en.wikipedia.org/w/index.php?title=LOLCODE&oldid=1365801280).
Wikipedia text is licensed under CC BY-SA 4.0. To avoid mixing that license into
the repository's MIT-licensed source, these samples are independent programs
based only on the demonstrated language concepts rather than copies or
adaptations of Wikipedia's code.

| Community example | Repository sample | Status |
|-------------------|-------------------|--------|
| Comments | [comments.lol](wikipedia/comments.lol) | Independently written; exercises `BTW` and `OBTW`/`TLDR` |
| Counting loops | [basic loops](../basics/loops/loops.lol) | Already covered with current 1.2 syntax |
| STDIO hello world | Not imported | Requires the unsupported historical `CAN HAS STDIO?` module declaration |
| Legacy file opening | Not imported | Uses nonstandard legacy file/error syntax rather than LOLCODE 1.2 or 1.3 |
| LOLCODE 1.0 counting | Not imported | Uses obsolete syntax; the current 1.2 loop sample covers the same behavior |

## Deferred Compatibility Work

The Esolang Stack requires the draft LOLCODE 1.3 BUKKIT object model. It belongs
with the follow-up compiler/runtime work rather than this 1.2-compatible sample
collection.
