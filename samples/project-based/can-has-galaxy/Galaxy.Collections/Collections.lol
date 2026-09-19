HAI 1.4

HOW IZ I NEWLIST
  O HAI IM listPrototype
    HOW IZ I ADD YR value
      I HAS A slotName ITZ SMOOSH "item" AN ME'Z count MKAY
      ME HAS A SRS slotName ITZ value
      ME'Z count R SUM OF ME'Z count AN 1
      FOUND YR ME'Z count
    IF U SAY SO

    HOW IZ I GET YR index
      I HAS A slotName ITZ SMOOSH "item" AN index MKAY
      FOUND YR ME'Z SRS slotName
    IF U SAY SO

    HOW IZ I SIZE
      FOUND YR ME'Z count
    IF U SAY SO
  KTHX
  I HAS A list ITZ LIEK A listPrototype
  list HAS A count ITZ 0
  list HAS A append ITZ list'Z ADD
  FOUND YR list
IF U SAY SO

HOW IZ I PAIR YR left AN YR right
  I HAS A pair ITZ A BUKKIT
  pair HAS A left ITZ left
  pair HAS A right ITZ right
  FOUND YR pair
IF U SAY SO

HOW IZ I ENCODE YR value
  FOUND YR SMOOSH value MKAY
IF U SAY SO

HOW IZ I DIGIT YR text AN YR position
  CAN HAS STRING?
  I HAS A character ITZ I IZ STRING'Z AT YR text AN YR position MKAY
  character IS NOW A NUMBR
  FOUND YR character
IF U SAY SO

KTHXBYE
