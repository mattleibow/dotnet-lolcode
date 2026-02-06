BTW Test Unicode hex escapes :(<hex>) for ASCII and non-ASCII code points
BTW Per spec: :(41) = "A" and hex is a Unicode code point value

HAI 1.2
  BTW simple ASCII using :(41)
  VISIBLE "HEX 41: :(41)"

  BTW multiple escapes in one string
  VISIBLE "MIXED: :(41):(42):(43)"

  BTW non-ASCII BMP character (SMILING FACE)
  VISIBLE "SMILE: :(263A)"

  BTW higher code point (GRINNING FACE)
  VISIBLE "GRIN: :(1F600)"
KTHXBYE
