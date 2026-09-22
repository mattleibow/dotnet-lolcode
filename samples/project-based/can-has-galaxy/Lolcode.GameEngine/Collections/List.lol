HAI 1.4

OBTW
GameEngine list primitives provide NEWLIST, a prototype-backed BUKKIT list
with dynamic SRS item slots. Construction performs no I/O or top-level state.
TLDR

BTW NEWLIST creates a prototype-backed dynamic list for reusable game state.
HOW IZ I NEWLIST
  O HAI IM listPrototype
    BTW ADD appends a value into a dynamically named BUKKIT slot.
    HOW IZ I ADD YR value
      I HAS A slotName ITZ SMOOSH "item" AN ME'Z count MKAY
      ME HAS A SRS slotName ITZ value
      ME'Z count R SUM OF ME'Z count AN 1
      FOUND YR ME'Z count
    IF U SAY SO

    BTW GET returns the stored item at a zero-based index.
    HOW IZ I GET YR index
      I HAS A slotName ITZ SMOOSH "item" AN index MKAY
      FOUND YR ME'Z SRS slotName
    IF U SAY SO

    BTW SIZE exposes the number of appended values.
    HOW IZ I SIZE
      FOUND YR ME'Z count
    IF U SAY SO

    BTW CONTAINS checks a list without exposing its dynamic storage slots.
    HOW IZ I CONTAINS YR wanted
      IM IN YR search UPPIN YR index TIL BOTH SAEM index AN ME'Z count
        BOTH SAEM ME IZ GET YR index MKAY AN wanted
        O RLY?
          YA RLY
            FOUND YR WIN
        OIC
      IM OUTTA YR search
      FOUND YR FAIL
    IF U SAY SO
  KTHX
  I HAS A list ITZ LIEK A listPrototype
  list HAS A count ITZ 0
  FOUND YR list
IF U SAY SO

KTHXBYE
