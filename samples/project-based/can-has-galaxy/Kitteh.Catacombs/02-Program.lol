HAI 1.4

CAN HAS GameEngine?
CAN HAS TerminalUi?

BTW RUNCATACOMBS registers first-class functions and runs a finite portable input loop.
HOW IZ I RUNCATACOMBS
  BTW session is Catacombs-only state passed through the generic router.
  I HAS A session ITZ A BUKKIT
  session HAS A room ITZ 0
  session HAS A running ITZ WIN
  I HAS A router ITZ I IZ GameEngine'Z NEWROUTER MKAY
  I IZ router'Z REGISTER YR "LOOK" AN YR CATLOOK MKAY
  I IZ router'Z REGISTER YR "STEP" AN YR CATSTEP MKAY
  I IZ router'Z REGISTER YR "QUIT" AN YR CATQUIT MKAY
  I IZ TerminalUi'Z HEADER YR "KITTEH CATACOMBS" MKAY
  I IZ TerminalUi'Z MENU YR "LOOK STEP QUIT" MKAY
  IM IN YR catLoop UPPIN YR tick WILE BOTH SAEM session'Z running AN WIN
    I IZ TerminalUi'Z PROMPT YR "CAT>" MKAY
    I HAS A command
    GIMMEH command
    BOTH SAEM command AN ""
    O RLY?
      YA RLY
        VISIBLE "EOF. KITTEH NAPS SAFELY."
        session'Z running R FAIL
      NO WAI
        VISIBLE router IZ DISPATCH YR command AN YR session MKAY
    OIC
  IM OUTTA YR catLoop
  FOUND YR session'Z room
IF U SAY SO

I IZ RUNCATACOMBS MKAY

KTHXBYE
