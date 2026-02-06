BTW Test assignment with R operator
BTW Per spec: <variable> R <expression> for assignment

HAI 1.2
  BTW basic assignment after declaration
  I HAS A x
  x R 10
  VISIBLE x

  BTW reassignment
  x R 20
  VISIBLE x

  BTW assignment with expression
  x R SUM OF x AN 5
  VISIBLE x

  BTW type change via assignment
  I HAS A var ITZ 42
  VISIBLE var
  var R "NOW A STRING"
  VISIBLE var
  var R 3.14
  VISIBLE var
  var R WIN
  VISIBLE var

  BTW assignment using another variable
  I HAS A a ITZ 100
  I HAS A b
  b R a
  VISIBLE b

  BTW self-referential assignment
  I HAS A n ITZ 5
  n R PRODUKT OF n AN n
  VISIBLE n
KTHXBYE
