BTW Test edge cases: BTW ignores trailing ... and ,
BTW Per spec: line continuation and soft-command-breaks are ignored after BTW

HAI 1.2
  BTW this comment has ... at the end but it does NOT continue...
  VISIBLE "LINE ONE"

  BTW this comment has comma, but it is ignored,
  VISIBLE "LINE TWO"

  BTW comment with both ... and ,
  VISIBLE "LINE THREE"

  VISIBLE "CODE" BTW inline comment ignores ... on same line too...
  VISIBLE "NEXT LINE"

  BTW comment with multiple ... in text ... still just one comment...
  VISIBLE "AFTER DOTS"

  BTW ellipsis in comment … should also be ignored…
  VISIBLE "AFTER ELLIPSIS"
KTHXBYE
