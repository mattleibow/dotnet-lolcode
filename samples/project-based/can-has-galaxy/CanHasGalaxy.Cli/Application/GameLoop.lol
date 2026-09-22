HAI 1.4

OBTW
Galaxy CLI game loop owns interactive input and the intentional final RUN call.
TLDR

BTW RUN owns interactive input while domain state stays in a dispatchable BUKKIT.
HOW IZ I RUN
  CAN HAS GameEngine?
  CAN HAS TerminalUi?
  CAN HAS CanHasGalaxy?
  I HAS A session ITZ A BUKKIT
  session HAS A game ITZ I IZ CanHasGalaxy'Z CREATEGAME YR "CAPTAIN" AN YR 7 MKAY
  session HAS A running ITZ WIN
  session HAS A saveFile ITZ "can-has-galaxy.save"
  session HAS A message ITZ "WELCOME, CAPTAIN. CHART READY."
  I HAS A router ITZ I IZ GameEngine'Z NEWROUTER MKAY
  I IZ REGISTERCOMMANDS YR router MKAY
  IM IN YR commandLoop UPPIN YR tick WILE BOTH SAEM session'Z running AN WIN
    I IZ CanHasGalaxy'Z RENDERDASHBOARD YR session'Z game AN YR session'Z message MKAY
    I IZ TerminalUi'Z PROMPT YR "GALAXY>" MKAY
    I HAS A command
    GIMMEH command
    VISIBLE ""
    BOTH SAEM command AN ""
    O RLY?
      YA RLY
        VISIBLE "EOF. SAFE LANDIN."
        session'Z running R FAIL
      NO WAI
        session'Z message R router IZ DISPATCH YR command AN YR session MKAY
        BOTH SAEM session'Z running AN FAIL
        O RLY?
          YA RLY
            I IZ CanHasGalaxy'Z RENDERDASHBOARD YR session'Z game AN YR session'Z message MKAY
        OIC
    OIC
  IM OUTTA YR commandLoop
  FOUND YR session'Z game
IF U SAY SO

I IZ RUN MKAY

KTHXBYE
