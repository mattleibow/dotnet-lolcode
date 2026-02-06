HAI 1.2
  BTW Test deeply nested conditionals and loops

  I HAS A x ITZ 5

  BOTH SAEM x AN 5
  O RLY?
    YA RLY
      VISIBLE "Outer:: x is 5"
      
      IM IN YR outer UPPIN YR i TIL BOTH SAEM i AN 2
        VISIBLE "Outer loop:: " i
        
        BOTH SAEM i AN 1
        O RLY?
          YA RLY
            VISIBLE "Inner:: i is 1"
            
            IM IN YR inner UPPIN YR j TIL BOTH SAEM j AN 2
              VISIBLE "Inner loop:: " j
              
              BOTH SAEM j AN 1
              O RLY?
                YA RLY
                  VISIBLE "Deepest:: j is 1"
              OIC
            IM OUTTA YR inner
        OIC
      IM OUTTA YR outer
  OIC

  VISIBLE "Done!"
KTHXBYE
