HAI 1.4

OBTW
GameEngine command routing creates a generic first-class-function registry
backed by BUKKIT SRS slots and the NEWLIST name index. NEWROUTER returns
mutable router state; REGISTER, KNOWS, and DISPATCH are methods with no output
side effects beyond that router. Collections functions are hoisted so this file
does not depend on glob order, and game-specific command names never appear
here. Consumers own command state and decide how returned messages are drawn.
TLDR

BTW NEWROUTER creates a first-class-function command registry using SRS slots.
HOW IZ I NEWROUTER
  BTW routerPrototype dispatches registered function values against a state BUKKIT.
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
