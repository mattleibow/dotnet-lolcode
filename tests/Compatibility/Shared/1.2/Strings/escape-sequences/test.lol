BTW Test basic string escape sequences (:), :>, ::, :", :o
BTW Per spec: colon introduces escape sequences and unicode/interpolation forms

HAI 1.2
  BTW newline escape :)
  VISIBLE "LINE1:)LINE2"

  BTW tab escape :>
  VISIBLE "COL1:>COL2"

  BTW colon escape ::
  VISIBLE "A::B"

  BTW quote escape :"
  VISIBLE "SHE SED :"OH RLY?:""

  BTW bell escape :o (prints something before and after bell)
  VISIBLE "BELL-START:oBELL-END"
KTHXBYE
