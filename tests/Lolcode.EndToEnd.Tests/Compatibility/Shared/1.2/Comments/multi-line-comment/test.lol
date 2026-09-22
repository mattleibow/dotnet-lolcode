BTW Test multi-line comments with OBTW...TLDR
BTW Per spec: started on own line or after comma

HAI 1.2
  VISIBLE "BEFORE BLOCK"

  OBTW
    This is a multi-line comment.
    It can span many lines.
    None of this code executes:
    VISIBLE "NOT PRINTED"
  TLDR

  VISIBLE "AFTER BLOCK"

  BTW OBTW on same line as code after comma
  I HAS A x ITZ 5, OBTW
    comment block here
    more comments
  TLDR

  VISIBLE x

  BTW TLDR followed by code after comma
  OBTW another block TLDR, VISIBLE "AFTER TLDR COMMA"

  BTW nested-looking but not really nested
  OBTW
    OBTW this is not a nested comment
    just text inside the outer comment
  TLDR

  VISIBLE "DONE"
KTHXBYE
