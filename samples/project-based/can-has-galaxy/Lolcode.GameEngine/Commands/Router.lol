HAI 1.4

OBTW
GameEngine command routing creates a first-class-function registry backed by
BUKKIT SRS slots and NEWLIST name indexes, without output side effects.
TLDR

BTW NEWROUTER creates a first-class-function command registry using SRS slots.
HOW IZ I NEWROUTER
  O HAI IM routerPrototype
    BTW REGISTER saves a command's function value in a dynamic command slot.
    HOW IZ I REGISTER YR name AN YR action
      ME HAS A SRS name ITZ action
      I IZ ME'Z names'Z ADD YR name MKAY
      FOUND YR WIN
    IF U SAY SO

    BTW KNOWS validates a command before dynamic function invocation.
    HOW IZ I KNOWS YR name
      FOUND YR ME'Z names IZ CONTAINS YR name MKAY
    IF U SAY SO

    BTW DISPATCH invokes the function value registered under the supplied name.
    HOW IZ I DISPATCH YR name AN YR state
      I IZ ME'Z KNOWS YR name MKAY
      O RLY?
        YA RLY
          FOUND YR ME IZ SRS name YR state MKAY
        NO WAI
          FOUND YR "COMMAND NOT FOUND."
      OIC
    IF U SAY SO
  KTHX
  I HAS A router ITZ LIEK A routerPrototype
  router HAS A names ITZ I IZ NEWLIST MKAY
  FOUND YR router
IF U SAY SO

KTHXBYE
