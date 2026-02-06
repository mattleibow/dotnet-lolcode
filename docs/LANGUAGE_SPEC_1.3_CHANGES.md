# LOLCODE 1.3 Changes Specification

This document describes the differences between the [LOLCODE 1.2 spec](archive/lolcode-spec-v1.2.md) (Final Draft) and the [LOLCODE 1.3 spec](archive/lolcode-spec-v1.3.md) (Draft). These changes are **not implemented** in the dotnet-lolcode compiler (which targets 1.2), but are documented here for future reference and to avoid painting ourselves into a corner.

> *The 1.3 spec was never finalized. It is a community draft of proposals.*

---

## Status

| Feature | Category | Complexity | This Compiler |
|---------|----------|-----------|---------------|
| Version header `HAI 1.3` | Changed | Trivial | ❌ Not implemented |
| Memory/GC semantics | Clarified | None (.NET GC) | ✅ Automatic |
| `I HAS A x ITZ A <type>` (typed defaults) | New Feature | Low | ❌ Not implemented |
| Primitive immutability | Clarified | None (.NET) | ✅ Automatic |
| `SRS` dynamic identifiers | New Feature | High | ❌ Not implemented |
| `<var> R NOOB` deallocation | Clarified | None | ✅ Works under 1.2 |
| Functions as variables | Changed | Medium | ❌ Not implemented |
| `HOW DUZ I` alias | Changed | Low | ❌ Not implemented |
| BUKKIT object system | New Feature | Very High | ❌ Not implemented |
| Slot access `'Z` | New Feature | High | ❌ Not implemented |
| Object methods | New Feature | High | ❌ Not implemented |
| `ME` keyword | New Feature | High | ❌ Not implemented |
| `O HAI IM` blocks | New Feature | High | ❌ Not implemented |
| Special slots | New Feature | High | ❌ Not implemented |
| Inheritance `ITZ LIEK A` | New Feature | Very High | ❌ Not implemented |
| Mixin inheritance `SMOOSH` | New Feature | Very High | ❌ Not implemented |

---

## 1. Version and Metadata

**Category:** Changed

The 1.3 spec downgrades from "FINAL DRAFT" to "DRAFT" and reframes the goal:

> *The goal of this specification is to advance the LOLCODE 1.2 specification with generally agreed-upon proposals from the original forum and site. These proposals add language constructs and functionality that make LOLCODE more similar to what programmers have come to expect from other modern programming languages.*

Programs would use `HAI 1.3` instead of `HAI 1.2`.

---

## 2. Memory / Garbage Collection

**Category:** Clarified (new section)

> All variables are merely references to locations in memory. It is assumed that when a variable is no longer referenced, that variable's allocated space will be freed sometime in the future, or on program exit.

**Impact:** None for .NET — the CLR GC handles this automatically.

---

## 3. Variable Naming

**Category:** Changed (minor)

1.2 says "all uppercase or lowercase letters", 1.3 changes to "all small or lowercase letters". This may be a typo in 1.3 — the meaning is unclear, as the rest of the naming rules remain identical.

---

## 4. Typed Variable Initialization

**Category:** New Feature

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

Additionally, bare `I HAS A <variable>` is explicitly stated as shorthand for `I HAS A <variable> ITZ NOOB`.

**New keywords:** `ITZ A <type>` (in declaration context)

---

## 5. Primitive Type Immutability

**Category:** Clarified (new section)

> All primitive types are considered Immutable. All built in operations return new objects instead of references to old objects. The exceptions to this rule are WIN, FAIL and NOOB. Every TROOF reference is either the WIN or FAIL object. Every NOOB reference is to the NOOB instance.

**Impact:** None for .NET — value types and strings are already immutable.

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

**New keywords:** `SRS`

**Impact:** High — requires runtime variable lookup by name (dictionary-based variable storage instead of pure compile-time locals).

---

## 7. Variable Deallocation

**Category:** Clarified

```
<variable> R NOOB
```

1.3 explicitly describes this as "deallocation" — the reference still exists in scope but points to nothing. The previous value will be garbage collected if no other references exist.

**Impact:** None — this already works under 1.2 semantics (assign NOOB to a variable).

---

## 8. Functions as Variables

**Category:** Changed

Functions now occupy the **same namespace** as variables and can be overwritten:

```
HOW DUZ I var YR stuff
    BTW implement
IF U SAY SO

I HAS A var ITZ 0    BTW Error: var is already taken (function exists)
var R 0               BTW Legal: function is replaced with NUMBR 0
```

1.3 also introduces `HOW DUZ I` as an alternative/alias for `HOW IZ I` for function definition.

**New keywords:** `HOW DUZ I`

**Impact:** Medium — functions must be stored in the same symbol table as variables; reassignment must be supported.

---

## 9. Function Call Namespace Clarification

**Category:** Clarified

> The I parameter is used to distinguish a function call on the current namespace vs. a function call on a bukkit (defined below).

`I IZ <func>` calls from the current/global namespace. `<object> IZ <slot>` calls from an object's namespace.

---

## 10. BUKKIT — Full Object/Container System

**Category:** New Feature (massive)

This is the single largest addition in 1.3. BUKKITs replace the "reserved for future expansion" placeholder with a complete prototype-based object system.

### 10.1 Declaration

```
I HAS A <object> ITZ A BUKKIT
```

Creates an empty object with default BUKKIT behavior.

### 10.2 Slot Creation / Assignment

```
<object> HAS A <slotname> ITZ <expression>
```

Places a value into a named slot. Slots can hold NUMBRs, NUMBARs, TROOFs, YARNs, functions (`FUNKSHUN`), and other BUKKITs. Slots are indexed by NUMBR or YARN. A slot may be declared/initialized more than once (just changes the value).

### 10.3 Slot Access Operator: `'Z`

```
<object>'Z <slotname>
<object>'Z SRS <expression>     BTW indirect access via SRS
```

### 10.4 Object Method Definition

```
HOW IZ <object> <slot> [YR <argument>...]
    <code block>
IF U SAY SO
```

Note: `HOW IZ <object>` (not `HOW IZ I`) — defines a method on a specific object.

### 10.5 Object Method Calls

```
<object> IZ <slotname> [YR <arg>...] MKAY
```

Distinguished from `I IZ <func>` (global function call) by the object reference.

### 10.6 `ME` Keyword

Inside a method called on an object, `ME` refers to the calling object:

```
HOW IZ I fooin YR bar
    ME HAS A bar2       BTW creates slot on calling object
    ME'Z bar R bar       BTW sets calling object's bar slot
IF U SAY SO
```

If there is no calling object, accessing `ME` throws an exception.

### 10.7 Scope Rules Inside Object Methods

Variable lookup order:
1. Function namespace (args + locally declared vars)
2. Calling object's namespace (if called from object)
3. "Global" namespace

`IT` is always looked up from global namespace.

### 10.8 Alternate Object Definition Syntax

```
O HAI IM <object> [IM LIEK <parent>]
    <code block>
KTHX
```

Inside this block, `I` refers to `<object>`, not the global scope. Identifiers resolve: object slots → global scope → error.

### 10.9 Special Slots

Every BUKKIT has three special slots:

| Slot | Purpose |
|------|---------|
| `parent` | Reference to prototype/parent object |
| `omgwtf` | Called when slot access fails (default: throw error) |
| `izmakin` | Constructor — called after prototype copy, before return |

### 10.10 Inheritance / Prototyping

```
I HAS A <child> ITZ LIEK A <parent>
```

Or with alternate syntax:
```
O HAI IM <child> IM LIEK <parent>
    <code block>
KTHX
```

**Slot inheritance rules:**
- **Accessing:** searches current object → parent → parent's parent → ... (stops at NOOB parent or cycle)
- **Assigning:** if found in ancestor chain, creates a copy in current object then sets; if not found anywhere, declaration error
- **Functions:** during a slot-access function call, the function obtains variables from the object it was accessed from (polymorphic dispatch)

### 10.11 Mixin Inheritance via `SMOOSH`

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

**New keywords:** `BUKKIT`, `HAS A` (object context), `'Z`, `ME`, `O HAI IM`, `KTHX`, `IM LIEK`, `ITZ LIEK A`, `SMOOSH` (extended to inheritance), `parent`, `omgwtf`, `izmakin`

---

## Summary of All New Keywords

| Keyword | Context | Purpose |
|---------|---------|---------|
| `SRS` | Anywhere an identifier is expected | Dynamic identifier resolution |
| `HOW DUZ I` | Function definition | Alias for `HOW IZ I` |
| `ITZ A <type>` | Variable declaration | Typed initialization with defaults |
| `BUKKIT` | Type name | Container/object type |
| `<obj> HAS A` | Slot creation | Create/assign slot on object |
| `'Z` | Slot access | Read/write object slots |
| `ME` | Inside methods | Reference to calling object |
| `O HAI IM` | Object definition | Block-based object literal |
| `KTHX` | Object definition | Closes `O HAI IM` block |
| `IM LIEK` | Inheritance | Inherit from parent in block syntax |
| `ITZ LIEK A` | Inheritance | Prototype-based inheritance |
| `HOW IZ <obj>` | Method definition | Define method in object slot |
| `<obj> IZ <slot>` | Method call | Call method on object |
| `parent` | Special slot | Prototype chain link |
| `omgwtf` | Special slot | Missing slot handler |
| `izmakin` | Special slot | Post-construction hook |

---

## Design Considerations for Future 1.3 Support

If the compiler later targets 1.3, these architectural decisions should be considered now:

1. **Variable storage:** The `SRS` operator requires runtime variable lookup by name. Consider using `Dictionary<string, object>` as the variable store (the runtime library already uses `object` for all values).

2. **Function/variable unification:** Functions should be stored as values in the same symbol table as variables, not in a separate function table.

3. **BUKKIT as runtime type:** `Dictionary<string, object>` is a natural fit for BUKKIT slots. The `'Z` operator maps to dictionary indexing.

4. **Prototype chains:** The `parent` slot creates a linked list of objects. Slot lookup walks this chain — similar to JavaScript's prototype chain.

5. **`ME` binding:** When calling `<obj> IZ <slot>`, the runtime must pass `obj` as the `ME` context to the function.

6. **`O HAI IM` blocks:** These are essentially object literal expressions with their own scope — similar to JavaScript's object constructors.
