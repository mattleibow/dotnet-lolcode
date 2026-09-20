HAI 1.4

OBTW
Galaxy persistence command handlers own the CLI save-file policy while the
domain library retains all STDIO resource ownership and record validation.
TLDR

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

KTHXBYE
