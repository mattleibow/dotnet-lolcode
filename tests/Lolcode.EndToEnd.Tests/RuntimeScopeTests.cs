namespace Lolcode.EndToEnd.Tests;

public class RuntimeScopeTests : EndToEndTestBase
{
    [Fact]
    public void IfBodyUsesChildScopeAndRestoresParentIt()
    {
        AssertOutput("""
            HAI 1.3
              I HAS A value ITZ "parent"
              "parent IT"
              O RLY?
                YA RLY
                  I HAS A value ITZ "child"
                  "child IT"
              OIC
              VISIBLE value
              VISIBLE IT
            KTHXBYE
            """, "parent\nparent IT");
    }

    [Fact]
    public void GtfoFromSwitchBodyRestoresParentScope()
    {
        AssertOutput("""
            HAI 1.3
              I HAS A value ITZ "parent"
              WIN
              WTF?
                OMG WIN
                  I HAS A value ITZ "child"
                  GTFO
              OIC
              VISIBLE value
            KTHXBYE
            """, "parent");
    }

    [Fact]
    public void GtfoFromLoopBodyRestoresParentScope()
    {
        AssertOutput("""
            HAI 1.3
              I HAS A value ITZ "parent"
              IM IN YR once
                I HAS A value ITZ "child"
                GTFO
              IM OUTTA YR once
              VISIBLE value
            KTHXBYE
            """, "parent");
    }

    [Fact]
    public void BlockDeclarationDoesNotLeak()
    {
        AssertRuntimeError("""
            HAI 1.3
              WIN
              O RLY?
                YA RLY
                  I HAS A blockOnly ITZ 42
              OIC
              VISIBLE blockOnly
            KTHXBYE
            """, "Binding does not exist: blockOnly");
    }

    [Fact]
    public void DirectItTargetSupportsAssignmentAndInPlaceCast()
    {
        AssertOutput("""
            HAI 1.3
              "41"
              IT R SMOOSH IT AN "2" MKAY
              IT IS NOW A NUMBR
              VISIBLE IT
            KTHXBYE
            """, "412");
    }

    [Fact]
    public void DynamicItTargetSupportsReadAssignmentAndInPlaceCast()
    {
        AssertOutput("""
            HAI 1.3
              I HAS A target ITZ "IT"
              "42"
              SRS target R SMOOSH SRS target AN ".9" MKAY
              SRS target IS NOW A NUMBR
              VISIBLE SRS target
            KTHXBYE
            """, "42");
    }
}
