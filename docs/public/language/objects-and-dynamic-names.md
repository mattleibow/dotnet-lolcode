# Objects and dynamic names

<span class="badge badge-draft">1.3 draft provenance</span>
<span class="badge badge-supported">Supported</span>

BUKKIT and SRS are advanced features adopted from the unfinished 1.3 draft and
the pinned reference implementation. They are implemented here, but they are
not part of the stable 1.2 core. `HAI 1.3` or `HAI 1.4` permits the runtime-name
forms where static binding cannot resolve an identifier; the version token is
not a complete feature switch.

## BUKKIT and slots

Create a BUKKIT, then declare its slots:

```lolcode
HAI 1.3
I HAS A cat ITZ A BUKKIT
cat HAS A name ITZ "LILBUB"
VISIBLE cat'Z name
KTHXBYE
```

BUKKITs are runtime namespaces with a prototype lookup chain. A new BUKKIT
falls back to the scope in which it was created. `parent` reads or rewires that
prototype; assigning NOOB terminates the chain. Lookup detects prototype cycles.
Assignment updates the scope that owns an inherited slot, rather than silently
creating a receiver-local shadow. Declarations may override inherited slots but
not a slot already owned by the destination BUKKIT.

## Methods, ME, and mixins

Functions are values and can occupy a BUKKIT slot. Calling a method binds `ME`
to the receiver:

```lolcode
I HAS A counter ITZ A BUKKIT
counter HAS A value ITZ 0
counter HAS A NEXT ITZ HOW IZ I
  ME'Z value R SUM OF ME'Z value AN 1
  FOUND YR ME'Z value
IF U SAY SO
VISIBLE counter IZ NEXT MKAY
```

`ME` is the calling BUKKIT, separate from ordinary lexical lookup. Inside a
method, bare names use invocation locals and lexical scopes; use `ME'Z` for
receiver slots. A BUKKIT created within a method retains the active caller as
well as its prototype behavior.

Object definitions can set a prototype and copy mixin slots. Mixins are shallow
copied in reverse argument order before the declared parent is installed. The
implementation accepts both `ITZ LIEK A parent` and the draft's `ITZ A parent
SMOOSH ...` spelling. Consult the [1.3 draft delta](../reference/language-spec-1.3-changes.md)
for the historical, sometimes contradictory proposal text.

## SRS: a computed identifier

`SRS <expression>` evaluates the expression, casts it to YARN, and uses that
text as an identifier at that point in a path:

```lolcode
I HAS A slotName ITZ "name"
VISIBLE cat'Z SRS slotName
```

It works for variables, parameters, functions, BUKKIT slots, and object names.
Every SRS segment is evaluated independently. This is why dynamic paths reach
the runtime rather than being completely fixed during binding. Functions and
variables share the runtime binding namespace, so a function value can be
stored, replaced, and called dynamically.

Conditional, switch, and loop bodies create child scopes. Their declarations
and `IT` do not leak, though an assignment can update an existing lexical
binding. For the canonical compatibility details, including object identity,
casts, and scope behavior, see the
[implementation profile](../reference/implementation-profile.md).
