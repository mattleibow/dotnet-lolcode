# LOLCODE 1.3 Changes Specification

This document describes **only the differences** between the archived [LOLCODE 1.2 Final Draft](archive/lolcode-spec-v1.2.md) and archived [LOLCODE 1.3 Draft](archive/lolcode-spec-v1.3.md). It preserves contradictions and incomplete proposals rather than silently turning them into settled language rules.

> *The 1.3 spec was never finalized. It is a community draft of proposals. Some sections contain internal inconsistencies or typos, which are noted below.*

Compiler support and implementation considerations are recorded separately in the non-normative [Implementation Profile](LANGUAGE_IMPLEMENTATION.md).

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

**Selected provenance marker changes:** Formatting, File Creation, Declaration, Types, Case, Functions, and the new Arrays section use `(from 1.2)` or `(updated from 1.2)`. Comments and Naming retain `(from 1.1)`.

---

## 2. Memory / Garbage Collection

**Category:** Clarified (new section added before Scope)

> All variables are merely references to locations in memory. It is assumed that when a variable is no longer referenced, that variable's allocated space will be freed sometime in the future, or on program exit.

---

## 3. Variable Naming

**Category:** Changed (minor — likely typo)

1.2 says "all uppercase or lowercase letters"; 1.3 changes that phrase to "all small or lowercase letters". The rest of the rule still permits mixtures and still distinguishes `cheezburger`, `CheezBurger`, and `CHEEZBURGER`. The new wording is internally contradictory. It is likely a drafting error, but the archived delta does not establish whether uppercase identifiers were intended to become illegal.

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

---

## 6. `SRS` (Serious) — Dynamic Identifier Resolution

**Category:** New Feature

**New syntax:**
```
SRS <expression>
```

Interprets a `YARN` value (or anything castable to `YARN`) as an identifier at runtime. It can be used **anywhere** a regular identifier is expected, including variable, function, object, and slot positions.

```
I HAS A name ITZ "var"
I HAS A SRS name ITZ 0     BTW same as: I HAS A var ITZ 0
```

The `A` becomes optional in declarations with `SRS`:
```
I HAS SRS name ITZ 0       BTW also valid
```

**New keyword:** `SRS`

---

## 7. Variable Deallocation

**Category:** Clarified (new subsection)

```
<variable> R NOOB
```

1.3 adds a dedicated "Deallocation" subsection explicitly describing this as clearing the reference. The reference still exists in scope but points to nothing. The previous value will be garbage collected if no other references exist.

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

---

## 9. `HOW DUZ I` — Unresolved Draft Inconsistency

**Category:** Draft inconsistency

The functions-as-variables example uses `HOW DUZ I`, but the normative Functions section continues to define only `HOW IZ I`. The earlier archived 1.2 wiki witness used `HOW DUZ I`, while the later 1.2 revision deliberately changed it to `HOW IZ I`. The isolated 1.3 occurrence therefore does **not** establish an alias; it is preserved as unresolved stale wording.

---

## 10. Function Call Namespace Clarification

**Category:** Clarified

1.3 adds this sentence to the end of the Functions/Calling section:

> The I parameter is used to distinguish a function call on the current namespace vs. a function call on a bukkit (defined below).

This establishes that `I IZ <func>` calls from the current/local namespace, while `<object> IZ <slot>` calls from an object's namespace (see §12.5).

---

## 11. Arrays Placeholder Removed; Reservation Retained

**Category:** Removed + contradictory addition

The 1.2 `### Arrays` subsection under Types (which said arrays and dictionaries were under-specified) is removed. A new top-level `## Arrays` section proposes the BUKKIT system (see §12). However, the inherited Types overview still says BUKKIT is "reserved for future expansion." The draft never reconciles those statements.

---

## 12. BUKKIT — Full Object/Container System

**Category:** New Feature (massive)

This is the single largest addition in 1.3: a proposed prototype-based object system in a new top-level `## Arrays` section (marked `*(updated from 1.2)*`). It is not complete or internally consistent: BUKKIT remains reserved in the Types overview, and the TYPE/default/cast tables are not extended for BUKKIT or functions.

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

Places a value into a named slot. A slot may be declared/initialized more than once (just changes the value). The slot name can be any identifier or the source's undefined "SRS BIZNUS cast." This appears to mean an `SRS` expression, but the draft does not say so explicitly. A function can be assigned into a slot:

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

> **1.3 spec inconsistencies:** The prose names `-`, while every example uses `'Z`. Examples also vary between `<object> 'Z <slot>` and `<object>'Z <slot>`, even though whitespace normally separates tokens. `'Z` is strongly evidenced as the intended operator, but spacing is not settled by the draft.

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

Distinguished from `I IZ <func>` (a call in the current namespace) by the object reference. Combined with `SRS`, this allows dynamic method dispatch:

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

The BUKKIT section says `IT` is always looked up from the global namespace.

> **1.3 spec contradiction:** The unchanged Scope section says there is no global scope, the Statements section says `IT` remains local, and the Functions section says functions cannot access outer variables. The BUKKIT lookup rules nevertheless introduce a global namespace and globally resolved `IT`. The draft does not define whether "global" means the main program block or a new scope, so neither interpretation is normalized here.

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
| `omgwtf` | Called when slot access fails; may return a value that is installed in the missing slot or throw |
| `izmakin` | Called after an object is fully prototyped and before the prototyping operation returns |

These are special slot names, not language keywords.

> **1.3 spec note:** The description of `omgwtf` says "the default implementation of canhas" throws, but `canhas` is not defined. The intended relationship between that name and `omgwtf` is unresolved.

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

Copies mixins into the new object in reverse **mixin argument** order, then replaces the parent slot with the declared parent. Mixin inheritance is **static**: later changes to mixin objects do not propagate.

The draft also gives a post-creation workaround. It creates an intermediate object by mixing an existing object into a BUKKIT, rewires that intermediate object's `parent`, and then assigns the intermediate object as the existing child's parent:

```lolcode
I HAS A slice ITZ A bukkit SMOOSH cheeze
slice'Z parent R burger'Z parent
cheezburger2'Z parent R slice
```

The accompanying comment says this copies `cheeze` and its parent slots, which is broader than the earlier rule that only slots "defined on the mixin" are copied. The draft does not resolve that conflict.

> **Note:** `SMOOSH` is **not** a new keyword — it already exists in 1.2 for string concatenation. In 1.3, it gains a second meaning in the inheritance context.

---

## 13. Minor Formatting/Structural Changes

These changes have no semantic impact but exist as differences between the two specs:

| Change | Detail |
|--------|--------|
| Section restructuring | "Declaration and Assignment" split into "Declaration" + "Assignment" |
| Provenance markers | Selected sections move to 1.2 provenance; Comments and Naming retain 1.1 provenance |
| Functions section | Gains `(updated from 1.2)` provenance marker |
| Loops paragraph | Backtick formatting removed from metavariable names in iteration loop description |
| Function definition syntax | Unicode ellipsis `…` normalized to three ASCII periods `...` in argument syntax |
| Typos in 1.3 | "distingish" (for "distinguish"), "instatiates" (for "instantiates") appear in draft |

---

## Summary of New/Changed Syntax

| Syntax | Context | Section |
|--------|---------|---------|
| `SRS <expression>` | Anywhere an identifier is expected | §6 |
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

1. **`HOW DUZ I` vs `HOW IZ I`:** The functions-as-variables example uses `HOW DUZ I`, but the Functions section still defines only `HOW IZ I`. This may be stale text from the earlier 1.2 witness; no alias is established.

2. **Slot access operator:** The prose says `-`, all examples use `'Z`, and examples disagree about whitespace before `'Z`.

3. **`canhas` reference:** The special slots section mentions "the default implementation of canhas" without defining `canhas` anywhere.

4. **Scope and `IT`:** "No global scope," local `IT`, function isolation, and BUKKIT global lookup cannot all hold simultaneously.

5. **BUKKIT status and type integration:** BUKKIT is both reserved and defined. NUMBR/YARN indexing lacks matching grammar; TYPE values, typed defaults, and cast targets omit BUKKIT and the undefined `FUNKSHUN` type.

6. **Special slots:** `omgwtf` materializes returned values into missing slots, but its default behavior is attributed to undefined `canhas`. `izmakin` is defined only for prototyping.

7. **Mixin copying:** The main rule copies slots defined on mixins, while the post-creation example claims parent slots are copied too.

8. **Unresolved source terms and syntax:** "SRS BIZNUS cast" and `FUNKSHUN` are undefined; one function example contains a stray `?`; inheritance examples alternate between `ITZ LIEK A` and `ITZ A`.

9. **Variable naming wording:** "uppercase" changed to "small" even though uppercase examples remain.

10. **Typos:** "distingish" (distinguish), "instatiates" (instantiates).

---

## Non-delta Material

Compiler support, complexity estimates, and possible implementation strategies are intentionally excluded from this delta. See the non-normative [Implementation Profile](LANGUAGE_IMPLEMENTATION.md).
