HAI 1.4

OBTW
Kitteh Catacombs application owns its crawler session, generic router, and
EOF-safe input loop. Commands return message YARNs while this function remains
the only rendering side effect. The final call relies only on hoisted functions.
TLDR

BTW RUNCATACOMBS registers first-class functions and runs a finite portable input loop.
HOW IZ I RUNCATACOMBS
  CAN HAS GameEngine?
  CAN HAS TerminalUi?
  BTW session is Catacombs-only state passed through the generic router.
  I HAS A session ITZ A BUKKIT
  session HAS A room ITZ 0
  session HAS A running ITZ WIN
  session HAS A message ITZ "THE CATACOMBS ARE QUIET. TYPE LOOK."
  I HAS A router ITZ I IZ GameEngine'Z NEWROUTER MKAY
  I IZ router'Z REGISTER YR "LOOK" AN YR CATLOOK MKAY
  I IZ router'Z REGISTER YR "STEP" AN YR CATSTEP MKAY
  I IZ router'Z REGISTER YR "QUIT" AN YR CATQUIT MKAY
  IM IN YR catLoop UPPIN YR tick WILE BOTH SAEM session'Z running AN WIN
    I IZ CATSCREEN YR session AN YR session'Z message MKAY
    I IZ TerminalUi'Z PROMPT YR "CAT>" MKAY
    I HAS A command
    GIMMEH command
    BTW Terminate the prompt in redirected output where typed input is not echoed.
    VISIBLE ""
    BOTH SAEM command AN ""
    O RLY?
      YA RLY
        VISIBLE "EOF. KITTEH NAPS SAFELY."
        session'Z running R FAIL
      NO WAI
        session'Z message R router IZ DISPATCH YR command AN YR session MKAY
        BTW Completion and QUIT both receive one final rendered response.
        BOTH SAEM session'Z running AN FAIL
        O RLY?
          YA RLY
            I IZ CATSCREEN YR session AN YR session'Z message MKAY
        OIC
    OIC
  IM OUTTA YR catLoop
  FOUND YR session'Z room
IF U SAY SO

I IZ RUNCATACOMBS MKAY

KTHXBYE
