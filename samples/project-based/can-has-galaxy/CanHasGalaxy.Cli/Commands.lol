HAI 1.4

OBTW
Galaxy command handlers bridge the generic GameEngine router and mutable CLI
session BUKKIT. Each public handler imports only the domain it needs, mutates
only documented game/session state, and returns one communications YARN rather
than drawing partial UI. Application.lol presents the retained dashboard after
dispatch, so handler output cannot interleave with widgets. Functions are
hoisted and therefore independent of the two source files' compilation order.
TLDR

BTW COMMANDSTATUS returns the current concise ship data for the communications panel.
HOW IZ I COMMANDSTATUS YR session
  CAN HAS CanHasGalaxy?
  FOUND YR I IZ CanHasGalaxy'Z STATUS YR session'Z game MKAY
IF U SAY SO

BTW COMMANDMAP returns the reusable star-chart summary for the next dashboard.
HOW IZ I COMMANDMAP YR session
  CAN HAS CanHasGalaxy?
  FOUND YR I IZ CanHasGalaxy'Z MAP YR session'Z game MKAY
IF U SAY SO

BTW COMMANDTRAVEL1 dispatches a fixed portable travel command.
HOW IZ I COMMANDTRAVEL1 YR session
  CAN HAS CanHasGalaxy?
  FOUND YR I IZ CanHasGalaxy'Z TRAVEL YR session'Z game AN YR 1 MKAY
IF U SAY SO

BTW COMMANDTRAVEL2 dispatches a fixed portable travel command.
HOW IZ I COMMANDTRAVEL2 YR session
  CAN HAS CanHasGalaxy?
  FOUND YR I IZ CanHasGalaxy'Z TRAVEL YR session'Z game AN YR 2 MKAY
IF U SAY SO

BTW COMMANDTRAVEL3 dispatches a fixed portable travel command.
HOW IZ I COMMANDTRAVEL3 YR session
  CAN HAS CanHasGalaxy?
  FOUND YR I IZ CanHasGalaxy'Z TRAVEL YR session'Z game AN YR 3 MKAY
IF U SAY SO

BTW COMMANDMINE mutates the shared session's Galaxy game state.
HOW IZ I COMMANDMINE YR session
  CAN HAS CanHasGalaxy?
  FOUND YR I IZ CanHasGalaxy'Z MINE YR session'Z game MKAY
IF U SAY SO

BTW COMMANDSELL mutates the shared session's Galaxy economy state.
HOW IZ I COMMANDSELL YR session
  CAN HAS CanHasGalaxy?
  FOUND YR I IZ CanHasGalaxy'Z SELLORE YR session'Z game MKAY
IF U SAY SO

BTW COMMANDFUEL buys fuel through the Galaxy economy domain function.
HOW IZ I COMMANDFUEL YR session
  CAN HAS CanHasGalaxy?
  FOUND YR I IZ CanHasGalaxy'Z BUYFUEL YR session'Z game MKAY
IF U SAY SO

BTW COMMANDFIGHT resolves one deterministic Galaxy combat turn.
HOW IZ I COMMANDFIGHT YR session
  CAN HAS CanHasGalaxy?
  FOUND YR I IZ CanHasGalaxy'Z FIGHT YR session'Z game MKAY
IF U SAY SO

BTW COMMANDMISSION returns the current Galaxy mission text.
HOW IZ I COMMANDMISSION YR session
  CAN HAS CanHasGalaxy?
  FOUND YR I IZ CanHasGalaxy'Z MISSIONTEXT YR session'Z game MKAY
IF U SAY SO

BTW COMMANDSAVE persists primitives only and keeps the file handle inside Galaxy.
HOW IZ I COMMANDSAVE YR session
  CAN HAS CanHasGalaxy?
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
  CAN HAS CanHasGalaxy?
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
