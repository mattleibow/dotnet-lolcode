BTW Test string interpolation with :{var} for NUMBR and YARN
BTW Per spec: :{var} inserts the current value of the variable cast to YARN

HAI 1.2
  I HAS A count ITZ 42
  I HAS A name ITZ "KITTEH"

  VISIBLE "COUNT:: :{count}"
  VISIBLE "NAME:: :{name}"

  BTW change variable values and ensure interpolation uses updated values
  count R 7
  name R "CEILING CAT"

  VISIBLE "UPDATED COUNT:: :{count}"
  VISIBLE "UPDATED NAME:: :{name}"
KTHXBYE
