BTW Test variable declaration with I HAS A, with and without ITZ
BTW Per spec: I HAS A declares, ITZ initializes in same statement

HAI 1.2
  BTW declaration without initialization (becomes NOOB)
  I HAS A uninit

  BTW declaration with initialization - various types
  I HAS A num ITZ 42
  I HAS A float_num ITZ 3.14
  I HAS A str ITZ "HELLO"
  I HAS A truth ITZ WIN
  I HAS A lie ITZ FAIL

  BTW print initialized variables
  VISIBLE num
  VISIBLE float_num
  VISIBLE str
  VISIBLE truth
  VISIBLE lie

  BTW declare and init with expression
  I HAS A sum ITZ SUM OF 10 AN 20
  VISIBLE sum

  BTW declare with variable reference
  I HAS A copy ITZ num
  VISIBLE copy

  BTW multiple declarations
  I HAS A a ITZ 1
  I HAS A b ITZ 2
  I HAS A c ITZ 3
  VISIBLE a
  VISIBLE b
  VISIBLE c
KTHXBYE
