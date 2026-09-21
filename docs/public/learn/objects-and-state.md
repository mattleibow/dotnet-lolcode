# 09 Objects and program state

**Prerequisite:** planning state and functions.

**Outcome:** understand why related state may be grouped in a BUKKIT, read a
small 1.3-draft object program, and recognize how BUKKITs differ from CLR
classes.

> [!IMPORTANT]
> BUKKIT, prototypes, `ME`, and dynamic names come from the LOLCODE 1.3 draft
> progression and pinned reference behavior. They are implemented here, but
> they are not part of the stable 1.2 core used by the beginner capstone.

## Group related facts and behavior

As programs grow, separate variables such as `catName`, `catLives`, and
`catMood` become easier to manage when grouped. A **BUKKIT** is a runtime object
with named slots and an optional prototype. It is not a statically declared
CLR class.

```lolcode
HAI 1.3
  O HAI IM cat
    I HAS A name ITZ "MIPS"
    I HAS A lives ITZ 9

    HOW IZ I describe
      VISIBLE ME'Z name " HAS " ME'Z lives " LIVEZ"
    IF U SAY SO
  KTHX

  cat IZ describe MKAY
KTHXBYE
```

Expected output:

```text
MIPS HAS 9 LIVEZ
```

`O HAI IM ... KTHX` defines an object. In a method, `ME` is the receiver, and
receiver slots require `ME'Z`. Bare names resolve through invocation locals
and caller lexical bindings; dynamic calls can therefore observe caller
bindings. Receiverless calls preserve the ambient `ME`.

## Prototypes are delegation, not classes

```lolcode
HAI 1.3
  O HAI IM animal
    I HAS A sound ITZ "?"
    HOW IZ I speak
      VISIBLE ME'Z sound
    IF U SAY SO
  KTHX

  I HAS A cat ITZ LIEK A animal
  cat HAS A sound ITZ "MEOW"
  cat IZ speak MKAY
KTHXBYE
```

Expected output:

```text
MEOW
```

The new BUKKIT delegates missing slots through its prototype. Calling the
inherited method still binds `ME` to `cat`, so it reads the overridden sound.
BUKKIT equality is object identity. Slots and functions share dynamic runtime
bindings and can be replaced; CLR-style field layouts, constructors, access
modifiers, generics, and compile-time member types do not apply.

## Practice: predict, run, explain, modify, create

1. **Predict:** If `cat HAS A sound...` is removed, what is printed?
2. **Run:** Confirm prototype lookup finds `"?"`.
3. **Explain:** Why does the inherited method use `ME'Z sound` rather than a
   bare `sound`?
4. **Modify:** Add a `name` slot and make `speak` print name plus sound.
5. **Create:** Define a prototype `counter` with a `value` slot and a method
   that displays it; create two derived objects with different values.

<details>
<summary>Hint and explained solution</summary>

```lolcode
HAI 1.3
  O HAI IM counter
    I HAS A value ITZ 0
    HOW IZ I show
      VISIBLE ME'Z value
    IF U SAY SO
  KTHX

  I HAS A first ITZ LIEK A counter
  first HAS A value ITZ 1
  I HAS A second ITZ LIEK A counter
  second HAS A value ITZ 2
  first IZ show MKAY
  second IZ show MKAY
KTHXBYE
```

Both objects inherit the method, but `ME` selects the receiver for each call.
</details>

## Common mistakes

- Treating BUKKIT as a CLR class declaration.
- Reading a receiver slot with a bare name; receiver access requires `ME'Z`.
- Assuming dynamic declarations and calls are hoisted like direct top-level
  multi-file functions. They are not.
- Using 1.3 forms under `HAI 1.2`.
- Assuming child-scope declarations or `IT` leak in 1.3/1.4 conditionals,
  switches, or loops.

## Checkpoint

You can explain slots, receivers, and prototype lookup without translating them
into class inheritance. For exact advanced semantics, use
[objects and dynamic names](../language/objects-and-dynamic-names.md). Next:
[grow into a .NET project](project-workflow.md).
