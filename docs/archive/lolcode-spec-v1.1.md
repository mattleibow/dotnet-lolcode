# LOLCODE Recommendation 1.1

## DRAFT

### June 19th, 2007

*The following is a DRAFT IN PROGRESS documenting the points put to vote on the forum. Contributors to the forum, and **familiar with the discussions** are encouraged to edit this document to capture the consensus reached by the vote.*

---

## CREATING A LOLCODE FILE

All LOLCODE programs must be opened with the command `HAI` with an optional version parameter. This parameter will be a designated codename which will be determined by a vote of the developer community.

```
HAI GINGER
```

Indicates that the program is valid with GINGER version of LOLCODE.

```
HAI
```

Indicates that the program is valid with the most recent version.

A LOLCODE file is closed by the keyword `KTHXBYE` which closes the `HAI` code-block.

---

## TYPING AND VARIABLE DECLARATIONS

There are currently 4 types of variables that LOLCODE recognizes: strings (YARN), numbers (NUMBR), booleans (TROOF), and arrays (BUKKIT). However, typing is handled dynamically, so until a variable is given an initial value, it is untyped and null. To declare a variable, the keyword is "I HAS A" followed by the variable name. To assign the variable a value, you can then follow the variable name with "ITZ \<value\>" or enter the value on a separate line with the beginning with the keyword LOL, followed by the variable name, the assignment operator "R" and then the value.

```
I HAS A VAR            BTW VAR is null and untyped
LOL VAR R THREE        BTW VAR is now a YARN and equals "THREE"
LOL VAR R 3            BTW VAR is now a NUMBR and equals 3
```

Type conversion is handled automatically.

---

## KEYWORDS

Keywords are case sensitive, and must be typed in all CAPITAL letters. They must begin with a letter and may be followed only by other letters, numbers, and underscores. No spaces, dashes, or other symbols.

---

## IDENTIFIERS

Identifiers are case sensitive, and may be in all CAPITAL or lowercase letters (or a mixture of the two). They must begin with a letter and may be followed only by other letters, numbers, and underscores. No spaces, dashes, or other symbols are allowed.

---

## COMMENTS

Single line comments are begun by `BTW`, and may occur either after a line of code, on a separate line, or following a line of code following a line separator (,).

All of these are valid single line comments:

```
I HAS A VAR ITZ 12        BTW VAR = 12
```

```
I HAS A VAR ITZ 12,       BTW VAR = 12
```

```
I HAS A VAR ITZ 12
                BTW VAR = 12
```

Multi-line comments are begun by `OBTW` and ended with `TLDR`, and should be started on their own line, or following a line of code after a line separator.

These are valid multi-line comments:

```
I HAS A VAR ITZ 12
           OBTW this is a long comment block
                see, i have more comments here
                and here
           TLDR
I HAS A FISH ITZ BOB
```

```
I HAS A VAR ITZ 12,  OBTW this is a long comment block
      see, i have more comments here
      and here
TLDR, I HAS A FISH ITZ BOB
```

---

## UNINITIALIZED AND NULL VALUES

All uninitialized values return a null value signified by `NOOB`. All uninitialized array and hash elements are `NOOB`. Uninitialized NUMBRs or those that have been assigned the value `NOOB` cannot be operated on. Attempting to perform math on them will cause an error. Only direct equality comparisons can be made on NUMBRs with NOOB values. Use of `BIGR` and `SMALR` and their variations to compare a value to `NOOB` will result in an error.

---

## CONDITIONALS

### IF-THEN

Still to be finalised with consensus.

### SWITCHES

The LOLCODE keyword for switches is WTF. The WTF is followed by an optional IZ and then the expression that is being evaluated and then an optional ?. The IZ and ? are syntactic sugar to help the code read better, if necessary. A comparison block is opened by OMG and must be a literal, not an expression. Each literal must be unique. The OMG block can be followed by any number of statements and can be terminated by a GTFO which breaks to the end of the the WTF statement. If a OMG block is not terminated by a GTFO, then the next OMG block is executed as is the next until a GTFO or the end of the WTF block is reached. The optional default value if none of the literals evaluate as true is signified by OMGWTF.

```
WTF COLOR   BTW could be WTF IZ COLOR or WTF COLOR? or WTF IZ COLOR?
      OMG R
          VISIBLE "RED FISH"
          GTFO
      OMG Y
          VISIBLE "YELLOW FISH"
      OMG G
      OMG B
          VISIBLE "FISH HAS A FLAVOR"
          GTFO
      OMGWTF
          VISIBLE "FISH IS TRANSPARENT"
OIC
```

In this example, the output results of evaluating the variable COLOR would be:

"R":
```
RED FISH
```

"Y":
```
YELLOW FISH
FISH HAS A FLAVOR
```

"G":
```
FISH HAS A FLAVOR
```

"B":
```
FISH HAS A FLAVOR
```

none of the above:
```
FISH IS TRANSPARENT
```

---

## INPUT AND OUTPUT

From 1.0:

To print to the standard output, the keyword is VISIBLE and automatically appends a newline unless the line concludes with an exclamation mark.

```
VISIBLE <stuff>[!]
```

There is currently no defined standard for printing to a file.

To accept input from the user, the keyword is

```
GIMMEH <variable>
```

which takes YARN and NUMBAR for input and stores the value in the given variable.

---

## ARRAYS AND HASHES

LOLCODE's use of arrays is similar to PHP's in that arrays can have either integers or strings as keys. In the first format, they are traditional arrays; in the latter, they are hashes.

---

## GOTO

There is no GOTO statement in this version of LOLCODE.

---

*specs/1.1.txt · Last modified: 2007/06/22 23:05 by tehnic*
