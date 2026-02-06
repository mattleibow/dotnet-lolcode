# LOLCODE 1.3 Changes Specification

This document describes **only the differences** between the [LOLCODE 1.2 spec](archive/lolcode-spec-v1.2.md) (Final Draft) and the [LOLCODE 1.3 spec](archive/lolcode-spec-v1.3.md) (Draft). These changes are **not implemented** in the dotnet-lolcode compiler (which targets 1.2), but are documented here for future reference and to avoid painting ourselves into a corner.

> *The 1.3 spec was never finalized. It is a community draft of proposals. Some sections contain internal inconsistencies or typos, which are noted below.*

---

## Status

| Feature | Category | Complexity | This Compiler |
|---------|----------|-----------|---------------|
| Version header `HAI 1.3` | Changed | Trivial | ❌ Not implemented |
| Memory/GC semantics | Clarified | None (.NET GC) | ✅ Automatic |
| `I HAS A x ITZ A <type>` (typed defaults) | New Feature | Low | ❌ Not implemented |
| Bare `I HAS A` = `ITZ NOOB` (explicit) | Clarified | None | ✅ Already works |
| Primitive immutability | Clarified | None (.NET) | ✅ Automatic |
| `SRS` dynamic identifiers | New Feature | High | ❌ Not implemented |
| `<var> R NOOB` deallocation | Clarified | None | ✅ Works under 1.2 |
| Functions as variables (unified namespace) | Changed | Medium | ❌ Not implemented |
| `HOW DUZ I` alias | New Feature | Low | ❌ Not implemented |
| Function call `I` vs object namespace | Clarified | Low | ❌ Not implemented |
| BUKKIT object system | New Feature | Very High | ❌ Not implemented |
| Slot access `'Z` | New Feature | High | ❌ Not implemented |
| Object methods (`HOW IZ <obj>`) | New Feature | High | ❌ Not implemented |
| Object method calls (`<obj> IZ <slot>`) | New Feature | High | ❌ Not implemented |
| `ME` keyword | New Feature | High | ❌ Not implemented |
| `O HAI IM` / `KTHX` blocks | New Feature | High | ❌ Not implemented |
| Special slots (`parent`, `omgwtf`, `izmakin`) | New Feature | High | ❌ Not implemented |
| Inheritance `ITZ LIEK A` | New Feature | Very High | ❌ Not implemented |
| Mixin inheritance (extended `SMOOSH`) | New Feature | Very High | ❌ Not implemented |
| `IT` always from global namespace | Clarified | Low | ⚠️ Design consideration |
| Variable naming wording change | Changed (likely typo) | Trivial | ❌ No action needed |

---

## 1. Document Metadata and Philosophy

**Category:** Changed / Structural

The 1.3 spec makes several metadata changes:

- **Status downgrade:** "FINAL DRAFT" → "DRAFT" (with trailing `*` footnote)
- **Date footnote:** Adds `*Date reflects latest documented proposal.`
- **Archived references:** Adds a link to the [original 1.3 proposal goals](https://web.archive.org/web/20130113074443/http://lolcode.com/proposals/1.3/1.3)
- **Goal statement rewrite:** The philosophy changes from "baseline for future specs" to "advance 1.2 with agreed-upon proposals":

> *The goal of this specification is to advance the LOLCODE 1.2 specification with generally agreed-upon proposals from the original forum and site. These proposals add language constructs and functionality that make LOLCODE more similar to what programmers have come to expect from other modern programming languages.*

- **Version header:** Programs use `HAI 1.3` instead of `HAI 1.2`.

**Provenance marker changes throughout:** All `(from 1.1)` and `(modified from 1.1)` markers in 1.2 are updated to `(from 1.2)` or `(updated from 1.2)` in 1.3, reflecting that 1.3 builds on 1.2 as its baseline.

---

## 2. Memory / Garbage Collection

**Category:** Clarified (new section added before Scope)

> All variables are merely references to locations in memory. It is assumed that when a variable is no longer referenced, that variable's allocated space will be freed sometime in the future, or on program exit.

**Impact:** None for .NET — the CLR GC handles this automatically.

---

## 3. Variable Naming

**Category:** Changed (minor — likely typo)

1.2 says "all uppercase or lowercase letters", 1.3 changes to "all small or lowercase letters". The rest of the naming rules (including examples using `CHEEZBURGER`) remain identical, so this appears to be a drafting error. No behavioral change.

---

## 4. Variable Declaration — Expanded and Restructured

**Category:** New Feature + Clarified + Structural

The 1.2 combined "Declaration and Assignment" section is split into separate "Declaration" and "Assignment" subsections in 1.3, with provenance updated to `(updated from 1.2)`.

### 4.1 Typed Default Initialization (New Feature)

**New syntax:**
```
I HAS A <variable> ITZ A <type>
```

Initializes a variable to the **default value** for the given type:

| Type | Default |
|------|---------|
| `YARN` | `""` |
| `TROOF` | `FAIL` |
| `NUMBR` | `0` |
| `NUMBAR` | `0.0` |
| `NOOB` | `NOOB` |

### 4.2 Bare Declaration Explicitly Defined (Clarified)

1.3 explicitly states that `I HAS A <variable>` is shorthand for `I HAS A <variable> ITZ NOOB`. (This was implicit in 1.2.)

### 4.3 Literal vs Expression Initialization (Clarified)

1.3 adds explanatory text: if the value in `I HAS A <variable> ITZ <value>` is a literal, the variable is initialized to the appropriate object type. If it's an identifier or expression, it's initialized to the resulting expression.

---

## 5. Primitive Type Immutability

**Category:** Clarified (new section)

> All primitive types are considered Immutable. All built in operations return new objects instead of references to old objects. The exceptions to this rule are WIN, FAIL and NOOB. Every TROOF reference is either the WIN or FAIL object. Every NOOB reference is to the NOOB instance.

**Impact:** None for .NET — value types and strings are already immutable. `WIN`/`FAIL`/`NOOB` as singletons aligns with how the compiler should represent these.

---

## 6. `SRS` (Serious) — Dynamic Identifier Resolution

**Category:** New Feature

**New syntax:**
```
SRS <expression>
```

Interprets a `YARN` value (or anything castable to `YARN`) as a variable identifier at runtime. Can be used **anywhere** a regular identifier is expected.

```
I HAS A name ITZ "var"
I HAS A SRS name ITZ 0     BTW same as: I HAS A var ITZ 0
```

The `A` becomes optional in declarations with `SRS`:
```
I HAS SRS name ITZ 0       BTW also valid
```

**New keyword:** `SRS`

**Impact:** High — requires runtime variable lookup by name (dictionary-based variable storage instead of pure compile-time locals).

---

## 7. Variable Deallocation

**Category:** Clarified (new subsection)

```
<variable> R NOOB
```

1.3 adds a dedicated "Deallocation" subsection explicitly describing this as clearing the reference. The reference still exists in scope but points to nothing. The previous value will be garbage collected if no other references exist.

**Impact:** None — this already works under 1.2 semantics (assign NOOB to a variable).

---

## 8. Functions as Variables (Unified Namespace)

**Category:** Changed

Functions now occupy the **same namespace** as variables. This is the key behavioral change:

```
HOW DUZ I var YR stuff
    BTW implement
IF U SAY SO

I HAS A var ITZ 0    BTW Error: var is already taken (function exists)
var R 0               BTW Legal: function is replaced with NUMBR 0
```

> **1.3 spec note:** The example uses `HOW DUZ I` (see §9), but the Functions section still defines `HOW IZ I` as the primary syntax. This appears to be a draft inconsistency.

**Impact:** Medium — functions must be stored in the same symbol table as variables; reassignment must be supported.

---

## 9. `HOW DUZ I` — Function Definition Alias

**Category:** New Feature

1.3 introduces `HOW DUZ I` as an alternative spelling for `HOW IZ I` in function definitions. It appears in the Variables/Declaration section examples but is not formally defined in the Functions section (which still uses `HOW IZ I`).

**New keyword:** `HOW DUZ I`

**Impact:** Low — lexer/parser addition.

---

## 10. Function Call Namespace Clarification

**Category:** Clarified

1.3 adds this sentence to the end of the Functions/Calling section:

> The I parameter is used to distinguish a function call on the current namespace vs. a function call on a bukkit (defined below).

This establishes that `I IZ <func>` calls from the current/local namespace, while `<object> IZ <slot>` calls from an object's namespace (see §12.5).

---

## 11. Arrays Placeholder Removed from Types

**Category:** Removed / Replaced

The 1.2 `### Arrays` subsection under Types (which said "Array and dictionary types are currently under-specified…") is **removed entirely** in 1.3. It is replaced by the new top-level `## Arrays` section defining the full BUKKIT system (see §12).

---

## 12. BUKKIT — Full Object/Container System

**Category:** New Feature (massive)

This is the single largest addition in 1.3. BUKKITs replace the "reserved for future expansion" placeholder with a complete prototype-based object system, defined in a new top-level `## Arrays` section (marked `*(updated from 1.2)*`).

### 12.1 BUKKIT as Container Type

BUKKITs are the container type. They may hold NUMBRs, NUMBARs, TROOFs, YARNs, functions (`FUNKSHUN`), and other BUKKITs. Each entity within a BUKKIT may be indexed by a NUMBR or a YARN. These indices are generically called "slots".

### 12.2 Declaration

```
I HAS A <object> ITZ A BUKKIT
```

Creates an empty object with default BUKKIT behavior.

### 12.3 Slot Creation / Assignment

```
<object> HAS A <slotname> ITZ <expression>
```

Places a value into a named slot. A slot may be declared/initialized more than once (just changes the value). The slot name can be any identifier or a `SRS` expression. A function can be assigned into a slot:

```
HOW IZ I blogin YR stuff
    VISIBLE stuff
IF U SAY SO

<object> HAS A blogin ITZ blogin
```

> **Note:** `HAS A` is not a new keyword — it already exists in `I HAS A`. In BUKKIT context, `<object> HAS A` uses the same tokens in a new grammatical position.

### 12.4 Slot Access Operator: `'Z`

```
<object>'Z <slotname>
<object>'Z SRS <expression>     BTW indirect access via SRS
```

> **1.3 spec inconsistency:** The prose says "slots are accessed using the slot operator `-`" but all examples use `'Z`. We follow the examples and treat `'Z` as the correct operator.

### 12.5 Object Method Definition

```
HOW IZ <object> <slot> [YR <argument>...]
    <code block>
IF U SAY SO
```

Note: `HOW IZ <object>` (not `HOW IZ I`) — defines a method on a specific object.

### 12.6 Object Method Calls

```
<object> IZ <slotname> [YR <arg>...] MKAY
```

Distinguished from `I IZ <func>` (global function call) by the object reference. Combined with `SRS`, allows dynamic method dispatch:

```
HOW IZ I getin YR object AN YR varName
    I HAS A funcName ITZ SMOOSH "get" AN varName MKAY
    FOUND YR object IZ SRS funcName MKAY
IF U SAY SO
```

### 12.7 `ME` Keyword

Inside a method called on an object, `ME` refers to the calling object:

```
HOW IZ I fooin YR bar
    ME HAS A bar2       BTW creates slot on calling object
    ME'Z bar R bar       BTW sets calling object's bar slot
IF U SAY SO
```

If there is no calling object, accessing `ME` throws an exception.

### 12.8 Scope Rules Inside Object Methods

Variable lookup order:
1. Function namespace (args + locally declared vars via `I HAS A`)
2. Calling object's namespace (if called from object)
3. "Global" namespace

`IT` is always looked up from global namespace.

> **1.3 spec note:** This introduces a "global namespace" concept, while the unchanged Scope section still says "there is no global scope." This appears to be a draft contradiction — the 1.3 spec likely intends "the main program block's scope" when it says "global."

### 12.9 Alternate Object Definition Syntax

```
O HAI IM <object> [IM LIEK <parent>]
    <code block>
KTHX
```

Inside this block, `I` refers to `<object>`, not the global scope. Identifiers resolve: object slots → global scope → error.

Example:
```
O HAI IM pokeman
    I HAS A name ITZ "pikachu"
    HOW IZ I pikachuin YR face
        BTW DEFINE
    IF U SAY SO
KTHX
```

### 12.10 Special Slots

Every BUKKIT has three special slots:

| Slot | Purpose |
|------|---------|
| `parent` | Reference to prototype/parent object |
| `omgwtf` | Called when slot access fails (default: throw error) |
| `izmakin` | Constructor — called after prototype copy, before return |

> **Note:** These are special slot *names*, not language keywords. They are reserved names within every BUKKIT's namespace.

> **1.3 spec note:** The description of `omgwtf` references "canhas" ("The default implementation of canhas is to always throw an exception") without defining it elsewhere. This appears to be a remnant of an earlier draft name for the slot-access-failure mechanism.

### 12.11 Inheritance / Prototyping

```
I HAS A <child> ITZ LIEK A <parent>
```

Or with alternate syntax:
```
O HAI IM <child> IM LIEK <parent>
    <code block>
KTHX
```

Inheritance automatically creates a `parent` slot on the new object pointing to the prototype. Changing the `parent` slot changes the prototype.

**Slot inheritance rules:**
- **Accessing:** searches current object → parent → parent's parent → ... (stops at NOOB parent or cycle)
- **Assigning:** if found in ancestor chain, creates a copy in current object then sets; if not found anywhere, declaration error
- **Functions:** during a slot-access function call, the function obtains variables from the object it was accessed from (polymorphic dispatch)

Example demonstrating polymorphic dispatch:
```
HOW IZ I funkin YR shun
    VISIBLE SMOOSH prefix AN shun MKAY
IF U SAY SO

O HAI IM parentClass
    I HAS A prefix ITZ "parentClass-"
    I HAS A funkin ITZ funkin
KTHX

O HAI IM testClass IM LIEK parentClass
    I HAS A prefix ITZ "testClass-"
KTHX

parentClass IZ funkin YR "HAI" MKAY    BTW prints: parentClass-HAI
testClass IZ funkin YR "HAI" MKAY      BTW prints: testClass-HAI
```

### 12.12 Mixin Inheritance via `SMOOSH`

```
I HAS A <object> ITZ A <parent> SMOOSH <mixin> [AN <mixin>]*
```

Or:
```
O HAI IM <object> IM LIEK <parent> SMOOSH <mixin> [AN <mixin>]*
    <code block>
KTHX
```

Copies all slots from mixins into the new object in **reverse order** of declaration, then sets parent. Mixin inheritance is **static** — later changes to mixin objects don't propagate.

> **Note:** `SMOOSH` is **not** a new keyword — it already exists in 1.2 for string concatenation. In 1.3, it gains a second meaning in the inheritance context.

---

## 13. Minor Formatting/Structural Changes

These changes have no semantic impact but exist as differences between the two specs:

| Change | Detail |
|--------|--------|
| Section restructuring | "Declaration and Assignment" split into "Declaration" + "Assignment" |
| Provenance markers | All `(from 1.1)` / `(modified from 1.1)` updated to `(from 1.2)` / `(updated from 1.2)` |
| Functions section | Gains `(updated from 1.2)` provenance marker |
| Loops paragraph | Backtick formatting removed from metavariable names in iteration loop description |
| Function definition syntax | Unicode ellipsis `…` normalized to three ASCII periods `...` in argument syntax |
| Typos in 1.3 | "distingish" (for "distinguish"), "instatiates" (for "instantiates") appear in draft |

---

## Summary of New/Changed Syntax

| Syntax | Context | Section |
|--------|---------|---------|
| `SRS <expression>` | Anywhere an identifier is expected | §6 |
| `HOW DUZ I` | Function definition (alias for `HOW IZ I`) | §9 |
| `I HAS A <var> ITZ A <type>` | Typed default initialization | §4.1 |
| `I HAS A <obj> ITZ A BUKKIT` | BUKKIT object creation | §12.2 |
| `<obj> HAS A <slot> ITZ <expr>` | Slot creation/assignment | §12.3 |
| `<obj>'Z <slot>` | Slot access | §12.4 |
| `HOW IZ <obj> <slot> [YR ...]` | Object method definition | §12.5 |
| `<obj> IZ <slot> [YR ...] MKAY` | Object method call | §12.6 |
| `ME` | Reference to calling object | §12.7 |
| `O HAI IM <obj> [IM LIEK <parent>]` | Alternate object definition | §12.9 |
| `KTHX` | Closes `O HAI IM` block | §12.9 |
| `I HAS A <obj> ITZ LIEK A <parent>` | Prototype inheritance | §12.11 |
| `... SMOOSH <mixin> [AN <mixin>]*` | Mixin inheritance (extended `SMOOSH`) | §12.12 |

---

## Known 1.3 Draft Issues

The 1.3 spec is an unfinished draft with several issues to be aware of:

1. **`HOW DUZ I` vs `HOW IZ I`:** The functions-as-variables example uses `HOW DUZ I`, but the Functions section still defines only `HOW IZ I`. The alias is implied but not formally specified.

2. **Slot access operator:** The prose says `"-"` but all examples use `'Z`. The examples should be followed.

3. **`canhas` reference:** The special slots section mentions "the default implementation of canhas" without defining `canhas` anywhere. Likely an earlier draft name for the slot-failure mechanism.

4. **"Global namespace" contradiction:** The Scope section (unchanged from 1.2) says "there is no global scope," but the BUKKIT scope rules reference a "global namespace" and say "IT is always looked up from global namespace."

5. **Variable naming wording:** "uppercase" changed to "small" — likely a typo since examples still use uppercase.

6. **Typos:** "distingish" (distinguish), "instatiates" (instantiates).

---

## Design Considerations for Future 1.3 Support

If the compiler later targets 1.3, these architectural decisions should be considered now:

1. **Variable storage:** The `SRS` operator requires runtime variable lookup by name. Consider using `Dictionary<string, object>` as the variable store (the runtime library already uses `object` for all values).

2. **Function/variable unification:** Functions should be stored as values in the same symbol table as variables, not in a separate function table. This enables `var R 0` to overwrite a function.

3. **BUKKIT as runtime type:** `Dictionary<string, object>` is a natural fit for BUKKIT slots. The `'Z` operator maps to dictionary indexing.

4. **Prototype chains:** The `parent` slot creates a linked list of objects. Slot lookup walks this chain — similar to JavaScript's prototype chain.

5. **`ME` binding:** When calling `<obj> IZ <slot>`, the runtime must pass `obj` as the `ME` context to the function.

6. **`O HAI IM` blocks:** These are essentially object literal expressions with their own scope — similar to JavaScript's object constructors.

7. **Generalized call expressions:** Design the call AST to support both `I IZ <func>` (no receiver) and `<obj> IZ <slot>` (with receiver) from the start.

8. **`SMOOSH` context sensitivity:** `SMOOSH` has two meanings in 1.3 (string concatenation and mixin inheritance). The parser will need to distinguish these by context.

9. **`IT` scoping:** The 1.3 rule that `IT` is "always looked up from global namespace" may affect how `IT` is stored and resolved. Don't bury `IT` as a simple local variable in the implementation.
