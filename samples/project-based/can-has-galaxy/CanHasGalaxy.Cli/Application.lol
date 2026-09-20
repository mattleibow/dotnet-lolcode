HAI 1.4

OBTW
Galaxy CLI application owns the interactive session BUKKIT, command router,
save-file choice, and EOF-safe input loop. It imports application libraries
inside RUN, registers hoisted command functions, and presents one retained
Galaxy screen before each input prompt. Command handlers update game state and
return a communications message; this file alone controls rendering and input.
The final top-level RUN call is intentionally effectful and relies on direct
function hoisting rather than source-file order.
TLDR

BTW REGISTERCOMMANDS installs first-class command function values in the shared router.
HOW IZ I REGISTERCOMMANDS YR router
  I IZ router'Z REGISTER YR "STATUS" AN YR COMMANDSTATUS MKAY
  I IZ router'Z REGISTER YR "MAP" AN YR COMMANDMAP MKAY
  I IZ router'Z REGISTER YR "TRAVEL1" AN YR COMMANDTRAVEL1 MKAY
  I IZ router'Z REGISTER YR "TRAVEL2" AN YR COMMANDTRAVEL2 MKAY
  I IZ router'Z REGISTER YR "TRAVEL3" AN YR COMMANDTRAVEL3 MKAY
  I IZ router'Z REGISTER YR "MINE" AN YR COMMANDMINE MKAY
  I IZ router'Z REGISTER YR "SELL" AN YR COMMANDSELL MKAY
  I IZ router'Z REGISTER YR "FUEL" AN YR COMMANDFUEL MKAY
  I IZ router'Z REGISTER YR "FIGHT" AN YR COMMANDFIGHT MKAY
  I IZ router'Z REGISTER YR "MISSION" AN YR COMMANDMISSION MKAY
  I IZ router'Z REGISTER YR "SAVE" AN YR COMMANDSAVE MKAY
  I IZ router'Z REGISTER YR "LOAD" AN YR COMMANDLOAD MKAY
  I IZ router'Z REGISTER YR "QUIT" AN YR COMMANDQUIT MKAY
  FOUND YR router
IF U SAY SO

BTW RUN owns interactive input while domain state stays in a dispatchable BUKKIT.
HOW IZ I RUN
  CAN HAS GameEngine?
  CAN HAS TerminalUi?
  CAN HAS CanHasGalaxy?
  BTW session is the CLI-owned mutable BUKKIT passed to generic handlers.
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
    BTW Terminate the prompt in redirected output where typed input is not echoed.
    VISIBLE ""
    BOTH SAEM command AN ""
    O RLY?
      YA RLY
        VISIBLE "EOF. SAFE LANDIN."
        session'Z running R FAIL
      NO WAI
        session'Z message R router IZ DISPATCH YR command AN YR session MKAY
        BTW A terminating command receives one final dashboard for its response.
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
