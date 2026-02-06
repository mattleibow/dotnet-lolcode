# LOLCODE Recommendation 1.0 (Modified)

## DRAFT

### 22 June 2007

*This is an edit of recommendation [1.0](https://web.archive.org/web/20111101140114/http://lolcode.com/spec/1.0) to reflect what is and isn't seen as being viable in the long-term for the language.*

1. Statements are separated by ~~`[\n.]+`~~ ***`[\n,]+`***

2. `CAN HAS <module/"file">?`
   - for inclusion/requirement

3. `GIMMEH [(LINE|WORD|LETTAR)] <VAR> [OUTTA <filedesc>]`
   - with default being LINE and STDIN

4. `HAI`

5. `KTHXBYE`
   - only closes HAI and exits with good condition

6. `DIAF [<num> [<text>]]`
   - Exits the program (failure)
   - Status code: `<num>`
   - Printed to stderr or equivalent: `<text>`

7. `BYES [<num> [<text>]]` ***???***

8. ~~`KTHX`~~
   - ~~is the universal "closing bracket" line~~
   - ~~for any if block, looping block, function, etc… except HAI~~

9. `IM IN YR [<loop label>]`
   - ~~label has no effect~~

10. `VISIBLE <stuff>[!]`
    - prints stuff as minimally as possible and with a newline (unless !)

11. `I HAS A <l_value> [ITZ …]` ~~Everything is an array. muahahahaha. To clarify, every VARIABLE is an array, and it knows its own dimensions. By default, the variable has one element~~
    - `ITZ …` has been tabled for future usage

12. `[## IN MAH]* <var>`
    - Since all variables are arrays, with no IN MAH it references the array as a whole.
    - Multiple occurrences of this index sub-levels of the array.
      - `1 IN MAH 2 IN MAH arr` ⇔ `arr[2][1]`
        - IN MAH clarifies the dimension of the array itself, it is not meant to "return" a smaller array. So, `0 IN MAH 0 IN MAH VAR` must only be used in a two-dimensional array, just like `**arr` should only be used in a 2-D array in C.
    - Does NOT expand the size of the array (except as l_value of assignment)
      - Throws error/dies on out of bounds?

13. `LOL <var> R <val>`
    - Assigns value into l_value specified by var.
    - Extends the size of var if necessary
    - If no index is specified, applies to all elements? (one by default)

14. `IZ <cond> [?] [(.|\n) YARLY] (.|\n) <code> (.|\n) [NOWAI (.|\n) <code>] KTHX`
    - Conditional syntax. See examples. ***to be revisited?***

15. Comparison operators: `[NOT] ((BIGR|SMALR) THAN|LIEK)`
    - See example and keywords:operators

16. Logical/Bitwise operators: `(NOT|AND|OR|XOR)`
    - See example and keywords:operators

17. Operators (all math, as of v1.0, is integer math)
    - `a UP b` : a + b
    - `UPZ a!![b]` : a += b (b=1)
    - `a NERF b` : a - b
    - `NERFZ a!![b]` : a -= b (b=1)
    - `a TIEMZ b` : a \* b
    - `TIEMZD a!![b]` : a \*= b (b=1)
    - `a OVAR b` : a / b
    - `OVARZ a!![b]` : a /= b (b=1) ***currently being revisited***

---

*specs/1.0-mod.txt · Last modified: 2007/06/22 16:31 by atl*
