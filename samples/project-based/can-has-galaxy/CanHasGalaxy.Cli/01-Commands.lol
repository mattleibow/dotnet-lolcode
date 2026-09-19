HAI 1.4

CAN HAS CanHasGalaxy?
CAN HAS TerminalUi?

BTW COMMANDSTATUS renders the current ship data through the shared UI widgets.
HOW IZ I COMMANDSTATUS YR session
  I IZ CanHasGalaxy'Z RENDERSTATUS YR session'Z game MKAY
  FOUND YR "STATUS SHOWN."
IF U SAY SO

BTW COMMANDMAP displays the reusable one-line star chart view.
HOW IZ I COMMANDMAP YR session
  I HAS A result ITZ I IZ CanHasGalaxy'Z MAP YR session'Z game MKAY
  I IZ TerminalUi'Z PANEL YR "STAR CHART" AN YR result MKAY
  FOUND YR result
IF U SAY SO

BTW COMMANDTRAVEL1 dispatches a fixed portable travel command.
HOW IZ I COMMANDTRAVEL1 YR session
  FOUND YR I IZ CanHasGalaxy'Z TRAVEL YR session'Z game AN YR 1 MKAY
IF U SAY SO

BTW COMMANDTRAVEL2 dispatches a fixed portable travel command.
HOW IZ I COMMANDTRAVEL2 YR session
  FOUND YR I IZ CanHasGalaxy'Z TRAVEL YR session'Z game AN YR 2 MKAY
IF U SAY SO

BTW COMMANDTRAVEL3 dispatches a fixed portable travel command.
HOW IZ I COMMANDTRAVEL3 YR session
  FOUND YR I IZ CanHasGalaxy'Z TRAVEL YR session'Z game AN YR 3 MKAY
IF U SAY SO

BTW COMMANDMINE mutates the shared session's Galaxy game state.
HOW IZ I COMMANDMINE YR session
  FOUND YR I IZ CanHasGalaxy'Z MINE YR session'Z game MKAY
IF U SAY SO

BTW COMMANDSELL mutates the shared session's Galaxy economy state.
HOW IZ I COMMANDSELL YR session
  FOUND YR I IZ CanHasGalaxy'Z SELLORE YR session'Z game MKAY
IF U SAY SO

BTW COMMANDFUEL buys fuel through the Galaxy economy domain function.
HOW IZ I COMMANDFUEL YR session
  FOUND YR I IZ CanHasGalaxy'Z BUYFUEL YR session'Z game MKAY
IF U SAY SO

BTW COMMANDFIGHT resolves one deterministic Galaxy combat turn.
HOW IZ I COMMANDFIGHT YR session
  FOUND YR I IZ CanHasGalaxy'Z FIGHT YR session'Z game MKAY
IF U SAY SO

BTW COMMANDMISSION returns the current Galaxy mission text.
HOW IZ I COMMANDMISSION YR session
  FOUND YR I IZ CanHasGalaxy'Z MISSIONTEXT YR session'Z game MKAY
IF U SAY SO

BTW COMMANDSAVE persists primitives only and keeps the file handle inside Galaxy.
HOW IZ I COMMANDSAVE YR session
  I IZ CanHasGalaxy'Z SAVE YR session'Z game AN YR session'Z saveFile MKAY
  O RLY?
    YA RLY
      FOUND YR SMOOSH "SAVE OK:: " AN session'Z saveFile MKAY
    NO WAI
      FOUND YR "SAVE FAIL."
  OIC
IF U SAY SO

BTW COMMANDLOAD replaces session state only when Galaxy validation succeeds.
HOW IZ I COMMANDLOAD YR session
  I HAS A loaded ITZ I IZ CanHasGalaxy'Z LOAD YR session'Z saveFile MKAY
  BOTH SAEM loaded AN NOOB
  O RLY?
    YA RLY
      FOUND YR "NO VALID SAVE."
    NO WAI
      session'Z game R loaded
      FOUND YR "LOAD OK."
  OIC
IF U SAY SO

BTW COMMANDQUIT ends the input loop through the shared mutable session BUKKIT.
HOW IZ I COMMANDQUIT YR session
  session'Z running R FAIL
  FOUND YR "KTHXBAI, CAPTAIN."
IF U SAY SO

KTHXBYE
