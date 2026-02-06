using System.Collections.Immutable;

namespace Lolcode.Compiler.Binding;

/// <summary>
/// Base class for all bound nodes (output of semantic analysis).
/// </summary>
public abstract class BoundNode
{
    /// <summary>The kind of bound node.</summary>
    public abstract BoundNodeKind Kind { get; }
}

/// <summary>Enumerates bound node types.</summary>
public enum BoundNodeKind
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
    public override BoundNodeKind Kind => BoundNodeKind.BlockStatement;
}

/// <summary>Variable declaration: I HAS A x [ITZ expr]</summary>
public sealed class BoundVariableDeclaration : BoundStatement
{
    public string Name { get; }
    public BoundExpression? Initializer { get; }
    public BoundVariableDeclaration(string name, BoundExpression? initializer)
    {
        Name = name;
        Initializer = initializer;
    }
    public override BoundNodeKind Kind => BoundNodeKind.VariableDeclaration;
}

/// <summary>Assignment: x R expr</summary>
public sealed class BoundAssignment : BoundStatement
{
    public string Name { get; }
    public BoundExpression Expression { get; }
    public BoundAssignment(string name, BoundExpression expression)
    {
        Name = name;
        Expression = expression;
    }
    public override BoundNodeKind Kind => BoundNodeKind.Assignment;
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
    public override BoundNodeKind Kind => BoundNodeKind.VisibleStatement;
}

/// <summary>GIMMEH statement.</summary>
public sealed class BoundGimmehStatement : BoundStatement
{
    public string VariableName { get; }
    public BoundGimmehStatement(string variableName) => VariableName = variableName;
    public override BoundNodeKind Kind => BoundNodeKind.GimmehStatement;
}

/// <summary>Expression statement (sets IT).</summary>
public sealed class BoundExpressionStatement : BoundStatement
{
    public BoundExpression Expression { get; }
    public BoundExpressionStatement(BoundExpression expression) => Expression = expression;
    public override BoundNodeKind Kind => BoundNodeKind.ExpressionStatement;
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
    public override BoundNodeKind Kind => BoundNodeKind.IfStatement;
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
    public override BoundNodeKind Kind => BoundNodeKind.SwitchStatement;
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

    /// <summary>The loop variable name. Null for infinite loops.</summary>
    public string? VariableName { get; }

    /// <summary>True for TIL, false for WILE. Null if no condition.</summary>
    public bool? IsTil { get; }

    /// <summary>The loop condition. Null for infinite loops.</summary>
    public BoundExpression? Condition { get; }

    public BoundBlockStatement Body { get; }

    public BoundLoopStatement(
        string label, string? operation, string? variableName,
        bool? isTil, BoundExpression? condition, BoundBlockStatement body)
    {
        Label = label;
        Operation = operation;
        VariableName = variableName;
        IsTil = isTil;
        Condition = condition;
        Body = body;
    }
    public override BoundNodeKind Kind => BoundNodeKind.LoopStatement;
}

/// <summary>GTFO statement.</summary>
public sealed class BoundGtfoStatement : BoundStatement
{
    /// <summary>The context: "loop", "switch", or "function".</summary>
    public string Context { get; }
    public BoundGtfoStatement(string context) => Context = context;
    public override BoundNodeKind Kind => BoundNodeKind.GtfoStatement;
}

/// <summary>HOW IZ I function declaration.</summary>
public sealed class BoundFunctionDeclaration : BoundStatement
{
    public string Name { get; }
    public ImmutableArray<string> Parameters { get; }
    public BoundBlockStatement Body { get; }
    public BoundFunctionDeclaration(string name, ImmutableArray<string> parameters, BoundBlockStatement body)
    {
        Name = name;
        Parameters = parameters;
        Body = body;
    }
    public override BoundNodeKind Kind => BoundNodeKind.FunctionDeclaration;
}

/// <summary>FOUND YR return.</summary>
public sealed class BoundReturnStatement : BoundStatement
{
    public BoundExpression Expression { get; }
    public BoundReturnStatement(BoundExpression expression) => Expression = expression;
    public override BoundNodeKind Kind => BoundNodeKind.ReturnStatement;
}

/// <summary>IS NOW A cast statement.</summary>
public sealed class BoundCastStatement : BoundStatement
{
    public string VariableName { get; }
    public string TargetType { get; }
    public BoundCastStatement(string variableName, string targetType)
    {
        VariableName = variableName;
        TargetType = targetType;
    }
    public override BoundNodeKind Kind => BoundNodeKind.CastStatement;
}

// ============ Bound Expressions ============

/// <summary>Base for bound expressions.</summary>
public abstract class BoundExpression : BoundNode { }

/// <summary>Literal value (int, double, string, bool, null).</summary>
public sealed class BoundLiteralExpression : BoundExpression
{
    public object? Value { get; }
    public BoundLiteralExpression(object? value) => Value = value;
    public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
}

/// <summary>Variable reference.</summary>
public sealed class BoundVariableExpression : BoundExpression
{
    public string Name { get; }
    public BoundVariableExpression(string name) => Name = name;
    public override BoundNodeKind Kind => BoundNodeKind.VariableExpression;
}

/// <summary>NOT expr.</summary>
public sealed class BoundUnaryExpression : BoundExpression
{
    public BoundExpression Operand { get; }
    public BoundUnaryExpression(BoundExpression operand) => Operand = operand;
    public override BoundNodeKind Kind => BoundNodeKind.UnaryExpression;
}

/// <summary>Binary arithmetic/boolean operation.</summary>
public sealed class BoundBinaryExpression : BoundExpression
{
    /// <summary>The operator: "SUM", "DIFF", "PRODUKT", "QUOSHUNT", "MOD", "BIGGR", "SMALLR", "BOTH", "EITHER", "WON"</summary>
    public string Operator { get; }
    public BoundExpression Left { get; }
    public BoundExpression Right { get; }
    public BoundBinaryExpression(string @operator, BoundExpression left, BoundExpression right)
    {
        Operator = @operator;
        Left = left;
        Right = right;
    }
    public override BoundNodeKind Kind => BoundNodeKind.BinaryExpression;
}

/// <summary>SMOOSH concatenation.</summary>
public sealed class BoundSmooshExpression : BoundExpression
{
    public ImmutableArray<BoundExpression> Operands { get; }
    public BoundSmooshExpression(ImmutableArray<BoundExpression> operands) => Operands = operands;
    public override BoundNodeKind Kind => BoundNodeKind.SmooshExpression;
}

/// <summary>ALL OF variadic AND.</summary>
public sealed class BoundAllOfExpression : BoundExpression
{
    public ImmutableArray<BoundExpression> Operands { get; }
    public BoundAllOfExpression(ImmutableArray<BoundExpression> operands) => Operands = operands;
    public override BoundNodeKind Kind => BoundNodeKind.AllOfExpression;
}

/// <summary>ANY OF variadic OR.</summary>
public sealed class BoundAnyOfExpression : BoundExpression
{
    public ImmutableArray<BoundExpression> Operands { get; }
    public BoundAnyOfExpression(ImmutableArray<BoundExpression> operands) => Operands = operands;
    public override BoundNodeKind Kind => BoundNodeKind.AnyOfExpression;
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
    public override BoundNodeKind Kind => BoundNodeKind.ComparisonExpression;
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
    public override BoundNodeKind Kind => BoundNodeKind.CastExpression;
}

/// <summary>I IZ function call.</summary>
public sealed class BoundFunctionCallExpression : BoundExpression
{
    public string FunctionName { get; }
    public ImmutableArray<BoundExpression> Arguments { get; }
    public BoundFunctionCallExpression(string functionName, ImmutableArray<BoundExpression> arguments)
    {
        FunctionName = functionName;
        Arguments = arguments;
    }
    public override BoundNodeKind Kind => BoundNodeKind.FunctionCallExpression;
}

/// <summary>IT implicit variable reference.</summary>
public sealed class BoundItExpression : BoundExpression
{
    public override BoundNodeKind Kind => BoundNodeKind.ItExpression;
}
