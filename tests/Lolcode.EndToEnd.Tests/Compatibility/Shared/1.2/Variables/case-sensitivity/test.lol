BTW Test variable names are case-sensitive
BTW Per spec: cheezburger, CheezBurger, CHEEZBURGER are different variables

HAI 1.2
  BTW declare variables with same name different case
  I HAS A cat ITZ "lowercase"
  I HAS A Cat ITZ "mixed"
  I HAS A CAT ITZ "uppercase"

  VISIBLE cat
  VISIBLE Cat
  VISIBLE CAT

  BTW modify one, others unchanged
  cat R "modified lowercase"
  VISIBLE cat
  VISIBLE Cat
  VISIBLE CAT

  BTW more complex names
  I HAS A myVar ITZ 1
  I HAS A MyVar ITZ 2
  I HAS A MYVAR ITZ 3
  I HAS A myvar ITZ 4

  VISIBLE myVar
  VISIBLE MyVar
  VISIBLE MYVAR
  VISIBLE myvar

  BTW underscores and numbers
  I HAS A var_1 ITZ "one"
  I HAS A VAR_1 ITZ "ONE"
  I HAS A Var_1 ITZ "One"

  VISIBLE var_1
  VISIBLE VAR_1
  VISIBLE Var_1
KTHXBYE
