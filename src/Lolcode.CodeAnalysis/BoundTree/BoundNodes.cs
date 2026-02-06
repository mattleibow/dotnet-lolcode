using System.Collections.Immutable;
using Lolcode.CodeAnalysis.Symbols;

namespace Lolcode.CodeAnalysis.BoundTree;

/// <summary>
/// Context tracking for GTFO resolution.
/// </summary>
public enum ControlFlowContext
{
    None,
    Loop,
    Switch,
    Function
}

/// <summary>
/// Base class for all bound nodes (output of semantic analysis).
/// </summary>
public abstract class BoundNode
{
    /// <summary>The kind of bound node.</summary>
    public abstract BoundKind Kind { get; }
}

/// <summary>Enumerates bound node types.</summary>
public enum BoundKind
{
    // Statements
    BlockStatement,
    VariableDeclaration,
    Assignment,
    VisibleStatement,
    GimmehStatement,
    ExpressionStatement,
    IfStatement,
    SwitchStatement,
    LoopStatement,
    GtfoStatement,
    FunctionDeclaration,
    ReturnStatement,
    CastStatement,

    // Expressions
    LiteralExpression,
    VariableExpression,
    UnaryExpression,
    BinaryExpression,
    SmooshExpression,
    AllOfExpression,
    AnyOfExpression,
    ComparisonExpression,
    CastExpression,
    FunctionCallExpression,
    ItExpression,
}

// ============ Bound Statements ============

/// <summary>Base for bound statements.</summary>
public abstract class BoundStatement : BoundNode { }

/// <summary>A block of bound statements.</summary>
public sealed class BoundBlockStatement : BoundStatement
{
    public ImmutableArray<BoundStatement> Statements { get; }
    public BoundBlockStatement(ImmutableArray<BoundStatement> statements) => Statements = statements;
    public override BoundKind Kind => BoundKind.BlockStatement;
}

/// <summary>Variable declaration: I HAS A x [ITZ expr]</summary>
public sealed class BoundVariableDeclaration : BoundStatement
{
    public VariableSymbol Variable { get; }
    public BoundExpression? Initializer { get; }
    public BoundVariableDeclaration(VariableSymbol variable, BoundExpression? initializer)
    {
        Variable = variable;
        Initializer = initializer;
    }
    public override BoundKind Kind => BoundKind.VariableDeclaration;
}

/// <summary>Assignment: x R expr</summary>
public sealed class BoundAssignment : BoundStatement
{
    public VariableSymbol Variable { get; }
    public BoundExpression Expression { get; }
    public BoundAssignment(VariableSymbol variable, BoundExpression expression)
    {
        Variable = variable;
        Expression = expression;
    }
    public override BoundKind Kind => BoundKind.Assignment;
}

/// <summary>VISIBLE statement.</summary>
public sealed class BoundVisibleStatement : BoundStatement
{
    public ImmutableArray<BoundExpression> Arguments { get; }
    public bool SuppressNewline { get; }
    public BoundVisibleStatement(ImmutableArray<BoundExpression> arguments, bool suppressNewline)
    {
        Arguments = arguments;
        SuppressNewline = suppressNewline;
    }
    public override BoundKind Kind => BoundKind.VisibleStatement;
}

/// <summary>GIMMEH statement.</summary>
public sealed class BoundGimmehStatement : BoundStatement
{
    public VariableSymbol Variable { get; }
    public BoundGimmehStatement(VariableSymbol variable) => Variable = variable;
    public override BoundKind Kind => BoundKind.GimmehStatement;
}

/// <summary>Expression statement (sets IT).</summary>
public sealed class BoundExpressionStatement : BoundStatement
{
    public BoundExpression Expression { get; }
    public BoundExpressionStatement(BoundExpression expression) => Expression = expression;
    public override BoundKind Kind => BoundKind.ExpressionStatement;
}

/// <summary>O RLY? conditional.</summary>
public sealed class BoundIfStatement : BoundStatement
{
    public BoundBlockStatement ThenBlock { get; }
    public ImmutableArray<BoundMebbeClause> MebbeClauses { get; }
    public BoundBlockStatement? ElseBlock { get; }
    public BoundIfStatement(BoundBlockStatement thenBlock, ImmutableArray<BoundMebbeClause> mebbeClauses, BoundBlockStatement? elseBlock)
    {
        ThenBlock = thenBlock;
        MebbeClauses = mebbeClauses;
        ElseBlock = elseBlock;
    }
    public override BoundKind Kind => BoundKind.IfStatement;
}

/// <summary>Bound MEBBE clause.</summary>
public sealed class BoundMebbeClause
{
    public BoundExpression Condition { get; }
    public BoundBlockStatement Body { get; }
    public BoundMebbeClause(BoundExpression condition, BoundBlockStatement body)
    {
        Condition = condition;
        Body = body;
    }
}

/// <summary>WTF? switch.</summary>
public sealed class BoundSwitchStatement : BoundStatement
{
    public ImmutableArray<BoundOmgClause> OmgClauses { get; }
    public BoundBlockStatement? DefaultBlock { get; }
    public BoundSwitchStatement(ImmutableArray<BoundOmgClause> omgClauses, BoundBlockStatement? defaultBlock)
    {
        OmgClauses = omgClauses;
        DefaultBlock = defaultBlock;
    }
    public override BoundKind Kind => BoundKind.SwitchStatement;
}

/// <summary>Bound OMG clause.</summary>
public sealed class BoundOmgClause
{
    public object? LiteralValue { get; }
    public BoundBlockStatement Body { get; }
    public BoundOmgClause(object? literalValue, BoundBlockStatement body)
    {
        LiteralValue = literalValue;
        Body = body;
    }
}

/// <summary>IM IN YR loop.</summary>
public sealed class BoundLoopStatement : BoundStatement
{
    public string Label { get; }

    /// <summary>"UPPIN", "NERFIN", or a custom function name. Null for infinite loops.</summary>
    public string? Operation { get; }

    /// <summary>The loop variable. Null for infinite loops.</summary>
    public VariableSymbol? Variable { get; }

    /// <summary>True for TIL, false for WILE. Null if no condition.</summary>
    public bool? IsTil { get; }

    /// <summary>The loop condition. Null for infinite loops.</summary>
    public BoundExpression? Condition { get; }

    public BoundBlockStatement Body { get; }

    public BoundLoopStatement(
        string label, string? operation, VariableSymbol? variable,
        bool? isTil, BoundExpression? condition, BoundBlockStatement body)
    {
        Label = label;
        Operation = operation;
        Variable = variable;
        IsTil = isTil;
        Condition = condition;
        Body = body;
    }
    public override BoundKind Kind => BoundKind.LoopStatement;
}

/// <summary>GTFO statement.</summary>
public sealed class BoundGtfoStatement : BoundStatement
{
    /// <summary>The resolved control flow context.</summary>
    public ControlFlowContext Context { get; }
    public BoundGtfoStatement(ControlFlowContext context) => Context = context;
    public override BoundKind Kind => BoundKind.GtfoStatement;
}

/// <summary>HOW IZ I function declaration.</summary>
public sealed class BoundFunctionDeclaration : BoundStatement
{
    public FunctionSymbol Function { get; }
    public BoundBlockStatement Body { get; }
    public BoundFunctionDeclaration(FunctionSymbol function, BoundBlockStatement body)
    {
        Function = function;
        Body = body;
    }
    public override BoundKind Kind => BoundKind.FunctionDeclaration;
}

/// <summary>FOUND YR return.</summary>
public sealed class BoundReturnStatement : BoundStatement
{
    public BoundExpression Expression { get; }
    public BoundReturnStatement(BoundExpression expression) => Expression = expression;
    public override BoundKind Kind => BoundKind.ReturnStatement;
}

/// <summary>IS NOW A cast statement.</summary>
public sealed class BoundCastStatement : BoundStatement
{
    public VariableSymbol Variable { get; }
    public string TargetType { get; }
    public BoundCastStatement(VariableSymbol variable, string targetType)
    {
        Variable = variable;
        TargetType = targetType;
    }
    public override BoundKind Kind => BoundKind.CastStatement;
}

// ============ Bound Expressions ============

/// <summary>Base for bound expressions.</summary>
public abstract class BoundExpression : BoundNode { }

/// <summary>Literal value (int, double, string, bool, null).</summary>
public sealed class BoundLiteralExpression : BoundExpression
{
    public object? Value { get; }
    public BoundLiteralExpression(object? value) => Value = value;
    public override BoundKind Kind => BoundKind.LiteralExpression;
}

/// <summary>Variable reference.</summary>
public sealed class BoundVariableExpression : BoundExpression
{
    public VariableSymbol Variable { get; }
    public BoundVariableExpression(VariableSymbol variable) => Variable = variable;
    public override BoundKind Kind => BoundKind.VariableExpression;
}

/// <summary>NOT expr.</summary>
public sealed class BoundUnaryExpression : BoundExpression
{
    public BoundUnaryOperatorKind OperatorKind { get; }
    public BoundExpression Operand { get; }
    public BoundUnaryExpression(BoundUnaryOperatorKind operatorKind, BoundExpression operand)
    {
        OperatorKind = operatorKind;
        Operand = operand;
    }
    public override BoundKind Kind => BoundKind.UnaryExpression;
}

/// <summary>Binary arithmetic/boolean operation.</summary>
public sealed class BoundBinaryExpression : BoundExpression
{
    public BoundBinaryOperatorKind OperatorKind { get; }
    public BoundExpression Left { get; }
    public BoundExpression Right { get; }
    public BoundBinaryExpression(BoundBinaryOperatorKind operatorKind, BoundExpression left, BoundExpression right)
    {
        OperatorKind = operatorKind;
        Left = left;
        Right = right;
    }
    public override BoundKind Kind => BoundKind.BinaryExpression;
}

/// <summary>SMOOSH concatenation.</summary>
public sealed class BoundSmooshExpression : BoundExpression
{
    public ImmutableArray<BoundExpression> Operands { get; }
    public BoundSmooshExpression(ImmutableArray<BoundExpression> operands) => Operands = operands;
    public override BoundKind Kind => BoundKind.SmooshExpression;
}

/// <summary>ALL OF variadic AND.</summary>
public sealed class BoundAllOfExpression : BoundExpression
{
    public ImmutableArray<BoundExpression> Operands { get; }
    public BoundAllOfExpression(ImmutableArray<BoundExpression> operands) => Operands = operands;
    public override BoundKind Kind => BoundKind.AllOfExpression;
}

/// <summary>ANY OF variadic OR.</summary>
public sealed class BoundAnyOfExpression : BoundExpression
{
    public ImmutableArray<BoundExpression> Operands { get; }
    public BoundAnyOfExpression(ImmutableArray<BoundExpression> operands) => Operands = operands;
    public override BoundKind Kind => BoundKind.AnyOfExpression;
}

/// <summary>BOTH SAEM or DIFFRINT comparison.</summary>
public sealed class BoundComparisonExpression : BoundExpression
{
    /// <summary>True for BOTH SAEM (equality), false for DIFFRINT (inequality).</summary>
    public bool IsEquality { get; }
    public BoundExpression Left { get; }
    public BoundExpression Right { get; }
    public BoundComparisonExpression(bool isEquality, BoundExpression left, BoundExpression right)
    {
        IsEquality = isEquality;
        Left = left;
        Right = right;
    }
    public override BoundKind Kind => BoundKind.ComparisonExpression;
}

/// <summary>MAEK cast expression.</summary>
public sealed class BoundCastExpression : BoundExpression
{
    public BoundExpression Operand { get; }
    public string TargetType { get; }
    public BoundCastExpression(BoundExpression operand, string targetType)
    {
        Operand = operand;
        TargetType = targetType;
    }
    public override BoundKind Kind => BoundKind.CastExpression;
}

/// <summary>I IZ function call.</summary>
public sealed class BoundFunctionCallExpression : BoundExpression
{
    public FunctionSymbol Function { get; }
    public ImmutableArray<BoundExpression> Arguments { get; }
    public BoundFunctionCallExpression(FunctionSymbol function, ImmutableArray<BoundExpression> arguments)
    {
        Function = function;
        Arguments = arguments;
    }
    public override BoundKind Kind => BoundKind.FunctionCallExpression;
}

/// <summary>IT implicit variable reference.</summary>
public sealed class BoundItExpression : BoundExpression
{
    public override BoundKind Kind => BoundKind.ItExpression;
}
