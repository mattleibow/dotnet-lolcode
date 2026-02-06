BTW Test string interpolation with :{var} for NUMBR, YARN, and TROOF
BTW Per spec: :{var} inserts the current value of the variable cast to YARN

HAI 1.2
  I HAS A count ITZ 42
  I HAS A name ITZ "KITTEH"
  I HAS A truth ITZ WIN

  VISIBLE "COUNT: :{count}"
  VISIBLE "NAME: :{name}"
  VISIBLE "TROOF: :{truth}"

  BTW change variable values and ensure interpolation uses updated values
  count R 7
  name R "CEILING CAT"
  truth R FAIL

  VISIBLE "UPDATED COUNT: :{count}"
  VISIBLE "UPDATED NAME: :{name}"
  VISIBLE "UPDATED TROOF: :{truth}"
KTHXBYE
