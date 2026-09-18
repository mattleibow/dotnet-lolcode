namespace Lolcode.EndToEnd.Tests;

public class ObjectTests : EndToEndTestBase
{
    [Fact]
    public void GlobalAndObjectFunctionsWithSameNameHaveIndependentSignatures()
    {
        AssertOutput("""
            HAI 1.3
              HOW IZ I combine YR value
                FOUND YR SMOOSH "global-" AN value MKAY
              IF U SAY SO
              O HAI IM box
                HOW IZ I combine YR left AN YR right
                  FOUND YR SMOOSH "object-" AN left AN right MKAY
                IF U SAY SO
              KTHX
              VISIBLE I IZ combine YR "one" MKAY
              VISIBLE I IZ box'Z combine YR "two" AN YR "three" MKAY
            KTHXBYE
            """, "global-one\nobject-twothree");
    }

    [Fact]
    public void AlternateObjectDeclarationCannotReplaceLocalBinding()
    {
        AssertCompileError("""
            HAI 1.3
              I HAS A occupied ITZ 1
              O HAI IM occupied
              KTHX
            KTHXBYE
            """, "LOL2002");
    }

    [Fact]
    public void NestedFunctionCannotReplaceParameter()
    {
        AssertCompileError("""
            HAI 1.3
              HOW IZ I outer YR occupied
                HOW IZ I occupied
                IF U SAY SO
              IF U SAY SO
            KTHXBYE
            """, "LOL2010");
    }

    [Fact]
    public void ArgumentSideEffectsCannotRetargetFunctionOrReceiver()
    {
        AssertOutput("""
            HAI 1.3
              O HAI IM first
                I HAS A marker ITZ "first"
                HOW IZ I show YR ignored
                  VISIBLE ME'Z marker
                IF U SAY SO
              KTHX
              O HAI IM second
                I HAS A marker ITZ "second"
                HOW IZ I show YR ignored
                  VISIBLE ME'Z marker
                IF U SAY SO
              KTHX
              I HAS A target ITZ first
              HOW IZ I retarget
                target R second
                FOUND YR 0
              IF U SAY SO
              I IZ target'Z show YR I IZ retarget MKAY MKAY
            KTHXBYE
            """, "first");
    }

    [Fact]
    public void AssignmentEvaluatesRhsBeforeDynamicTarget()
    {
        AssertOutput("""
            HAI 1.3
              I HAS A targetName ITZ "first"
              I HAS A first ITZ 0
              I HAS A second ITZ 0
              HOW IZ I chooseValue
                targetName R "second"
                FOUND YR 42
              IF U SAY SO
              SRS targetName R I IZ chooseValue MKAY
              VISIBLE first
              VISIBLE second
            KTHXBYE
            """, "0\n42");
    }

    [Fact]
    public void LaterDynamicSlotCannotRetargetCapturedObject()
    {
        AssertOutput("""
            HAI 1.3
              I HAS A first ITZ A BUKKIT
              first HAS A value ITZ "first"
              I HAS A second ITZ A BUKKIT
              second HAS A value ITZ "second"
              I HAS A target ITZ first
              HOW IZ I chooseSlot
                target R second
                FOUND YR "value"
              IF U SAY SO
              VISIBLE target'Z SRS I IZ chooseSlot MKAY
            KTHXBYE
            """, "first");
    }

    [Fact]
    public void PrototypeLookupAndMethodReceiverArePolymorphic()
    {
        AssertOutput("""
            HAI 1.3
              O HAI IM parent
                I HAS A prefix ITZ "parent-"
                HOW IZ I show YR value
                  VISIBLE SMOOSH ME'Z prefix AN value MKAY
                IF U SAY SO
              KTHX
              I HAS A child ITZ LIEK A parent
              child HAS A prefix ITZ "child-"
              child IZ show YR "value" MKAY
              parent IZ show YR "value" MKAY
            KTHXBYE
            """, "child-value\nparent-value");
    }

    [Fact]
    public void SrsResolvesDeclarationsSlotsFunctionsAndParameters()
    {
        AssertOutput("""
            HAI 1.3
              I HAS A objectName ITZ "box"
              I HAS A slotName ITZ "answer"
              I HAS A functionName ITZ "read"
              I HAS A parameterName ITZ "suffix"
              I HAS A SRS objectName ITZ A BUKKIT
              box HAS A SRS slotName ITZ 42
              HOW IZ box SRS functionName YR SRS parameterName
                VISIBLE ME'Z SRS slotName AN SRS parameterName
              IF U SAY SO
              I IZ box'Z SRS functionName YR "!" MKAY
            KTHXBYE
            """, "42!");
    }

    [Fact]
    public void SrsFunctionCallScopesSupportDynamicObjectPaths()
    {
        AssertOutput("""
            HAI 1.3
              O HAI IM box
                HOW IZ I direct
                  VISIBLE "direct"
                IF U SAY SO
              KTHX
              O HAI IM root
                O HAI IM child
                  HOW IZ I nested
                    VISIBLE "nested"
                  IF U SAY SO
                KTHX
              KTHX
              I HAS A scopeName ITZ "box"
              I HAS A childName ITZ "child"
              I HAS A functionName ITZ "nested"
              SRS scopeName IZ direct MKAY
              root'Z SRS childName IZ SRS functionName MKAY
            KTHXBYE
            """, "direct\nnested");
    }

    [Fact]
    public void MixinsAreCopiedInReverseArgumentOrder()
    {
        AssertOutput("""
            HAI 1.3
              O HAI IM parent
                I HAS A inherited ITZ "base"
              KTHX
              O HAI IM first
                I HAS A common ITZ "first"
                I HAS A one ITZ 1
              KTHX
              O HAI IM second
                I HAS A common ITZ "second"
                I HAS A two ITZ 2
              KTHX
              I HAS A child ITZ A parent SMOOSH first AN second
              VISIBLE child'Z inherited
              VISIBLE child'Z common
              VISIBLE child'Z one
              VISIBLE child'Z two
            KTHXBYE
            """, "base\nfirst\n1\n2");
    }

    [Fact]
    public void FunctionBindingsAreFirstClassAndReplaceable()
    {
        AssertOutput("""
            HAI 1.3
              HOW IZ I value
                FOUND YR 41
              IF U SAY SO
              I HAS A box ITZ A BUKKIT
              box HAS A callable ITZ value
              VISIBLE I IZ box'Z callable MKAY
              value R 42
              VISIBLE value
            KTHXBYE
            """, "41\n42");
    }

    [Fact]
    public void GimmehAcceptsDynamicAndSlotTargets()
    {
        AssertOutput("""
            HAI 1.3
              I HAS A targetName ITZ "dynamicValue"
              I HAS A dynamicValue
              I HAS A box ITZ A BUKKIT
              box HAS A slotValue
              GIMMEH SRS targetName
              GIMMEH box'Z slotValue
              VISIBLE dynamicValue
              VISIBLE box'Z slotValue
            KTHXBYE
            """, "dynamic input\nslot input", stdin: "dynamic input\nslot input\n");
    }

    [Fact]
    public void IsNowAAcceptsDynamicAndSlotTargets()
    {
        AssertOutput("""
            HAI 1.3
              I HAS A targetName ITZ "dynamicValue"
              I HAS A dynamicValue ITZ "42"
              I HAS A box ITZ A BUKKIT
              box HAS A slotValue ITZ "7"
              SRS targetName IS NOW A NUMBR
              box'Z slotValue IS NOW A NUMBR
              VISIBLE MAEK BOTH SAEM dynamicValue AN 42 A NUMBR
              VISIBLE MAEK BOTH SAEM box'Z slotValue AN 7 A NUMBR
            KTHXBYE
            """, "1\n1");
    }

    [Fact]
    public void ArticlelessDynamicDeclarationIsAccepted()
    {
        AssertOutput("""
            HAI 1.3
              I HAS A name ITZ "answer"
              I HAS SRS name ITZ 42
              VISIBLE answer
            KTHXBYE
            """, "42");
    }

    [Fact]
    public void DynamicDeclarationCannotReplaceLocalVariable()
    {
        AssertRuntimeError("""
            HAI 1.3
              I HAS A name ITZ "existing"
              I HAS A existing ITZ 1
              I HAS SRS name ITZ 2
            KTHXBYE
            """, "Binding already exists: existing");
    }

    [Fact]
    public void DynamicDeclarationCannotReplaceLocalFunction()
    {
        AssertRuntimeError("""
            HAI 1.3
              I HAS A name ITZ "existing"
              HOW IZ I existing
              IF U SAY SO
              I HAS SRS name ITZ 2
            KTHXBYE
            """, "Binding already exists: existing");
    }

    [Fact]
    public void DynamicFunctionCannotReplaceLocalVariable()
    {
        AssertRuntimeError("""
            HAI 1.3
              I HAS A name ITZ "existing"
              I HAS A existing ITZ 1
              HOW IZ I SRS name
              IF U SAY SO
            KTHXBYE
            """, "Binding already exists: existing");
    }

    [Fact]
    public void DuplicateDirectBukkitSlotDeclarationIsRejected()
    {
        AssertRuntimeError("""
            HAI 1.3
              I HAS A box ITZ A BUKKIT
              box HAS A value ITZ 1
              box HAS A value ITZ 2
            KTHXBYE
            """, "Binding already exists: value");
    }

    [Fact]
    public void DuplicateDynamicBukkitSlotDeclarationIsRejected()
    {
        AssertRuntimeError("""
            HAI 1.3
              I HAS A slotName ITZ "value"
              I HAS A box ITZ A BUKKIT
              box HAS A SRS slotName ITZ 1
              box HAS SRS slotName ITZ 2
            KTHXBYE
            """, "Binding already exists: value");
    }

    [Fact]
    public void BukkitSlotDeclarationMayOverrideInheritedSlot()
    {
        AssertOutput("""
            HAI 1.3
              I HAS A parent ITZ A BUKKIT
              parent HAS A value ITZ 1
              I HAS A child ITZ A parent
              child HAS A value ITZ 2
              VISIBLE parent'Z value
              VISIBLE child'Z value
            KTHXBYE
            """, "1\n2");
    }

    [Fact]
    public void InheritedSlotOverrideEvaluatesInitializer()
    {
        AssertOutput("""
            HAI 1.3
              I HAS A parent ITZ A BUKKIT
              parent HAS A value ITZ 1
              I HAS A child ITZ LIEK A parent
              HOW IZ I initializer
                VISIBLE "initializer"
                FOUND YR 2
              IF U SAY SO
              child HAS A value ITZ I IZ initializer MKAY
              VISIBLE child'Z value
            KTHXBYE
            """, "initializer\n2");
    }

    [Fact]
    public void ScopedDeclarationCollisionPrecedesInitializer()
    {
        AssertRuntimeError("""
            HAI 1.3
              I HAS A box ITZ A BUKKIT
              box HAS A occupied ITZ 1
              HOW IZ I initializer
                VISIBLE "initializer"
                FOUND YR 2
              IF U SAY SO
              box HAS A occupied ITZ I IZ initializer MKAY
            KTHXBYE
            """, "Binding already exists: occupied", expectedOutput: "");
    }

    [Fact]
    public void SrsDeclarationCollisionPrecedesInitializer()
    {
        AssertRuntimeError("""
            HAI 1.3
              I HAS A occupied ITZ 1
              I HAS A name ITZ "occupied"
              HOW IZ I initializer
                VISIBLE "initializer"
                FOUND YR 2
              IF U SAY SO
              I HAS SRS name ITZ I IZ initializer MKAY
            KTHXBYE
            """, "Binding already exists: occupied", expectedOutput: "");
    }

    [Fact]
    public void AlternateObjectCollisionPrecedesPrototypeAndBody()
    {
        AssertRuntimeError("""
            HAI 1.3
              I HAS A occupied ITZ 1
              I HAS A objectName ITZ "occupied"
              I HAS A parent ITZ A BUKKIT
              HOW IZ I chooseParent
                VISIBLE "prototype"
                FOUND YR "parent"
              IF U SAY SO
              O HAI IM SRS objectName IM LIEK SRS I IZ chooseParent MKAY
                VISIBLE "body"
              KTHX
            KTHXBYE
            """, "Binding already exists: occupied", expectedOutput: "");
    }

    [Fact]
    public void AssignmentToInheritedSlotUpdatesOwnerWithoutCreatingShadow()
    {
        AssertOutput("""
            HAI 1.3
              I HAS A parent ITZ A BUKKIT
              parent HAS A value ITZ 1
              I HAS A child ITZ A parent
              child'Z value R 2
              VISIBLE parent'Z value
              VISIBLE child'Z value
              child HAS A value ITZ 3
              VISIBLE parent'Z value
              VISIBLE child'Z value
            KTHXBYE
            """, "2\n2\n2\n3");
    }

    [Fact]
    public void MethodUsesCallerLexicalScopeAndReceiverOnlyThroughMe()
    {
        AssertOutput("""
            HAI 1.3
              I HAS A value ITZ "caller"
              O HAI IM box
                I HAS A value ITZ "receiver"
                HOW IZ I show
                  VISIBLE value
                  VISIBLE ME'Z value
                IF U SAY SO
              KTHX
              I IZ box'Z show MKAY
            KTHXBYE
            """, "caller\nreceiver");
    }

    [Fact]
    public void MethodCanReadCallingBlockLexicalBindings()
    {
        AssertOutput("""
            HAI 1.3
              O HAI IM box
                HOW IZ I show
                  VISIBLE lexical
                IF U SAY SO
              KTHX
              WIN
              O RLY?
                YA RLY
                  I HAS A lexical ITZ "calling block"
                  I IZ box'Z show MKAY
              OIC
            KTHXBYE
            """, "calling block");
    }

    [Fact]
    public void ReceiverlessCallPreservesCallerMe()
    {
        AssertOutput("""
            HAI 1.3
              HOW IZ I helper
                VISIBLE ME'Z value
              IF U SAY SO
              O HAI IM box
                I HAS A value ITZ "receiver"
                HOW IZ I invoke
                  I IZ helper MKAY
                IF U SAY SO
              KTHX
              I IZ box'Z invoke MKAY
            KTHXBYE
            """, "receiver");
    }

    [Fact]
    public void NestedDefaultParentBukkitPreservesMethodCaller()
    {
        AssertOutput("""
            HAI 1.3
              O HAI IM outer
                I HAS A marker ITZ "ok"
                HOW IZ I make
                  O HAI IM nested
                    VISIBLE ME'Z marker
                  KTHX
                IF U SAY SO
              KTHX
              I IZ outer'Z make MKAY
            KTHXBYE
            """, "ok");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GtfoLeavingObjectBodyRestoresOuterScope(bool useLoop)
    {
        string controlFlow = useLoop
            ? """
                IM IN YR once
                  O HAI IM unfinished
                    GTFO
                  KTHX
                IM OUTTA YR once
                """
            : """
                WIN
                WTF?
                  OMG WIN
                    O HAI IM unfinished
                      GTFO
                    KTHX
                OIC
                """;

        AssertRuntimeError($$"""
            HAI 1.3
              I HAS A name ITZ "existing"
              I HAS A existing ITZ 1
            {{controlFlow}}
              I HAS SRS name ITZ 2
            KTHXBYE
            """, "Binding already exists: existing");
    }
}
