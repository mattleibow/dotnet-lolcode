using System.Collections.Immutable;
using Lolcode.CodeAnalysis.Text;

namespace Lolcode.CodeAnalysis.Syntax;

/// <summary>
/// Base class for all syntax nodes in the AST.
/// </summary>
public abstract class SyntaxNode
{
    /// <summary>The kind of syntax node.</summary>
    public abstract SyntaxKind Kind { get; }

    /// <summary>The text span of this node.</summary>
    public abstract TextSpan Span { get; }
}

// ============ Statements ============

/// <summary>Base class for statement syntax nodes.</summary>
public abstract class StatementSyntax : SyntaxNode { }

/// <summary>An identifier, optionally computed with SRS and followed by a BUKKIT slot.</summary>
public sealed class IdentifierSyntax : SyntaxNode
{
    /// <summary>The direct identifier token, or <see langword="null"/> for SRS.</summary>
    public SyntaxToken? DirectToken { get; }

    /// <summary>The expression evaluated as the name for SRS identifiers.</summary>
    public ExpressionSyntax? NameExpression { get; }

    /// <summary>The next slot after <c>'Z</c>, if any.</summary>
    public IdentifierSyntax? Slot { get; }

    /// <summary>Creates an identifier syntax node.</summary>
    public IdentifierSyntax(SyntaxToken? directToken, ExpressionSyntax? nameExpression, IdentifierSyntax? slot)
    {
        DirectToken = directToken;
        NameExpression = nameExpression;
        Slot = slot;
    }

    /// <inheritdoc/>
    public override SyntaxKind Kind => SyntaxKind.IdentifierExpression;

    /// <inheritdoc/>
    public override TextSpan Span
    {
        get
        {
            int start = DirectToken?.Position ?? NameExpression!.Span.Start;
            int end = Slot?.Span.End ?? DirectToken?.Span.End ?? NameExpression!.Span.End;
            return TextSpan.FromBounds(start, end);
        }
    }
}

/// <summary>Root node of the syntax tree.</summary>
public sealed class CompilationUnitSyntax : SyntaxNode
{
    /// <summary>The program statement (HAI...KTHXBYE).</summary>
    public ProgramStatementSyntax Program { get; }

    /// <summary>The end-of-file token.</summary>
    public SyntaxToken EndOfFileToken { get; }

    public CompilationUnitSyntax(ProgramStatementSyntax program, SyntaxToken endOfFileToken)
    {
        Program = program;
        EndOfFileToken = endOfFileToken;
    }

    public override SyntaxKind Kind => SyntaxKind.CompilationUnit;
    public override TextSpan Span => TextSpan.FromBounds(Program.Span.Start, EndOfFileToken.Span.End);
}

/// <summary>HAI [version] ... KTHXBYE block.</summary>
public sealed class ProgramStatementSyntax : StatementSyntax
{
    public SyntaxToken HaiKeyword { get; }
    public SyntaxToken? VersionToken { get; }
    public ImmutableArray<StatementSyntax> Statements { get; }
    public SyntaxToken KthxbyeKeyword { get; }

    public ProgramStatementSyntax(
        SyntaxToken haiKeyword,
        SyntaxToken? versionToken,
        ImmutableArray<StatementSyntax> statements,
        SyntaxToken kthxbyeKeyword)
    {
        HaiKeyword = haiKeyword;
        VersionToken = versionToken;
        Statements = statements;
        KthxbyeKeyword = kthxbyeKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.ProgramStatement;
    public override TextSpan Span => TextSpan.FromBounds(HaiKeyword.Position, KthxbyeKeyword.Span.End);
}

/// <summary>I HAS A &lt;name&gt; [ITZ &lt;expr&gt;]</summary>
public sealed class VariableDeclarationSyntax : StatementSyntax
{
    public SyntaxToken NameToken { get; }
    public ExpressionSyntax? Initializer { get; }

    public VariableDeclarationSyntax(SyntaxToken nameToken, ExpressionSyntax? initializer)
    {
        NameToken = nameToken;
        Initializer = initializer;
    }

    public override SyntaxKind Kind => SyntaxKind.VariableDeclarationStatement;
    public override TextSpan Span
    {
        get
        {
            int end = Initializer?.Span.End ?? NameToken.Span.End;
            return TextSpan.FromBounds(NameToken.Position, end);
        }
    }
}

/// <summary>Declares a binding in the namespace selected by an identifier.</summary>
public sealed class ScopedDeclarationSyntax : StatementSyntax
{
        /// <summary>The destination namespace (<c>I</c>, <c>ME</c>, or a BUKKIT).</summary>
        public IdentifierSyntax Scope { get; }
        /// <summary>The identifier being declared.</summary>
        public IdentifierSyntax Name { get; }
        /// <summary>The optional initializer.</summary>
        public ExpressionSyntax? Initializer { get; }

        /// <summary>Creates a scoped declaration.</summary>
        public ScopedDeclarationSyntax(IdentifierSyntax scope, IdentifierSyntax name, ExpressionSyntax? initializer)
        {
            Scope = scope;
            Name = name;
            Initializer = initializer;
        }

        /// <inheritdoc/>
        public override SyntaxKind Kind => SyntaxKind.ScopedDeclarationStatement;
        /// <inheritdoc/>
        public override TextSpan Span => TextSpan.FromBounds(Scope.Span.Start, Initializer?.Span.End ?? Name.Span.End);
}

/// <summary>&lt;name&gt; R &lt;expr&gt;</summary>
public sealed class AssignmentStatementSyntax : StatementSyntax
{
    public SyntaxToken NameToken { get; }
    public ExpressionSyntax Expression { get; }

    public AssignmentStatementSyntax(SyntaxToken nameToken, ExpressionSyntax expression)
    {
        NameToken = nameToken;
        Expression = expression;
    }

    public override SyntaxKind Kind => SyntaxKind.AssignmentStatement;
    public override TextSpan Span => TextSpan.FromBounds(NameToken.Position, Expression.Span.End);
}

/// <summary>Assigns through a runtime-resolved identifier.</summary>
public sealed class IdentifierAssignmentSyntax : StatementSyntax
{
    /// <summary>The assignment target.</summary>
    public IdentifierSyntax Target { get; }
    /// <summary>The assigned expression.</summary>
    public ExpressionSyntax Expression { get; }

    /// <summary>Creates an identifier assignment.</summary>
    public IdentifierAssignmentSyntax(IdentifierSyntax target, ExpressionSyntax expression)
    {
        Target = target;
        Expression = expression;
    }

    /// <inheritdoc/>
    public override SyntaxKind Kind => SyntaxKind.AssignmentStatement;
    /// <inheritdoc/>
    public override TextSpan Span => TextSpan.FromBounds(Target.Span.Start, Expression.Span.End);
}

/// <summary>VISIBLE &lt;expr&gt;+ [!]</summary>
public sealed class VisibleStatementSyntax : StatementSyntax
{
    public SyntaxToken Keyword { get; }
    public ImmutableArray<ExpressionSyntax> Arguments { get; }
    public bool SuppressNewline { get; }

    public VisibleStatementSyntax(
        SyntaxToken keyword,
        ImmutableArray<ExpressionSyntax> arguments,
        bool suppressNewline)
    {
        Keyword = keyword;
        Arguments = arguments;
        SuppressNewline = suppressNewline;
    }

    public override SyntaxKind Kind => SyntaxKind.VisibleStatement;
    public override TextSpan Span
    {
        get
        {
            int end = Arguments.Length > 0 ? Arguments[^1].Span.End : Keyword.Span.End;
            return TextSpan.FromBounds(Keyword.Position, end);
        }
    }
}

/// <summary>GIMMEH &lt;name&gt;</summary>
public sealed class GimmehStatementSyntax : StatementSyntax
{
    /// <summary>Gets the GIMMEH keyword.</summary>
    public SyntaxToken Keyword { get; }
    /// <summary>Gets the identifier receiving the input.</summary>
    public IdentifierSyntax Target { get; }

    /// <summary>Creates a GIMMEH statement.</summary>
    public GimmehStatementSyntax(SyntaxToken keyword, IdentifierSyntax target)
    {
        Keyword = keyword;
        Target = target;
    }

    public override SyntaxKind Kind => SyntaxKind.GimmehStatement;
    public override TextSpan Span => TextSpan.FromBounds(Keyword.Position, Target.Span.End);
}

/// <summary>A bare expression statement (sets IT).</summary>
public sealed class ExpressionStatementSyntax : StatementSyntax
{
    public ExpressionSyntax Expression { get; }

    public ExpressionStatementSyntax(ExpressionSyntax expression)
    {
        Expression = expression;
    }

    public override SyntaxKind Kind => SyntaxKind.ExpressionStatement;
    public override TextSpan Span => Expression.Span;
}

/// <summary>O RLY? ... OIC conditional block.</summary>
public sealed class IfStatementSyntax : StatementSyntax
{
    public SyntaxToken ORlyKeyword { get; }
    public BlockStatementSyntax YaRlyBody { get; }
    public ImmutableArray<MebbeClauseSyntax> MebbeClauses { get; }
    public BlockStatementSyntax? NoWaiBody { get; }
    public SyntaxToken OicKeyword { get; }

    public IfStatementSyntax(
        SyntaxToken oRlyKeyword,
        BlockStatementSyntax yaRlyBody,
        ImmutableArray<MebbeClauseSyntax> mebbeClauses,
        BlockStatementSyntax? noWaiBody,
        SyntaxToken oicKeyword)
    {
        ORlyKeyword = oRlyKeyword;
        YaRlyBody = yaRlyBody;
        MebbeClauses = mebbeClauses;
        NoWaiBody = noWaiBody;
        OicKeyword = oicKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.IfStatement;
    public override TextSpan Span => TextSpan.FromBounds(ORlyKeyword.Position, OicKeyword.Span.End);
}

/// <summary>MEBBE &lt;expr&gt; ... clause inside an O RLY? block.</summary>
public sealed class MebbeClauseSyntax : SyntaxNode
{
    public SyntaxToken MebbeKeyword { get; }
    public ExpressionSyntax Condition { get; }
    public BlockStatementSyntax Body { get; }

    public MebbeClauseSyntax(SyntaxToken mebbeKeyword, ExpressionSyntax condition, BlockStatementSyntax body)
    {
        MebbeKeyword = mebbeKeyword;
        Condition = condition;
        Body = body;
    }

    public override SyntaxKind Kind => SyntaxKind.MebbeClause;
    public override TextSpan Span => TextSpan.FromBounds(MebbeKeyword.Position, Body.Span.End);
}

/// <summary>WTF? ... OIC switch block.</summary>
public sealed class SwitchStatementSyntax : StatementSyntax
{
    public SyntaxToken WtfKeyword { get; }
    public ImmutableArray<OmgClauseSyntax> OmgClauses { get; }
    public BlockStatementSyntax? OmgwtfBody { get; }
    public SyntaxToken OicKeyword { get; }

    public SwitchStatementSyntax(
        SyntaxToken wtfKeyword,
        ImmutableArray<OmgClauseSyntax> omgClauses,
        BlockStatementSyntax? omgwtfBody,
        SyntaxToken oicKeyword)
    {
        WtfKeyword = wtfKeyword;
        OmgClauses = omgClauses;
        OmgwtfBody = omgwtfBody;
        OicKeyword = oicKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.SwitchStatement;
    public override TextSpan Span => TextSpan.FromBounds(WtfKeyword.Position, OicKeyword.Span.End);
}

/// <summary>OMG &lt;literal&gt; ... case clause in WTF? block.</summary>
public sealed class OmgClauseSyntax : SyntaxNode
{
    public SyntaxToken OmgKeyword { get; }
    public ExpressionSyntax Value { get; }
    public BlockStatementSyntax Body { get; }

    public OmgClauseSyntax(SyntaxToken omgKeyword, ExpressionSyntax value, BlockStatementSyntax body)
    {
        OmgKeyword = omgKeyword;
        Value = value;
        Body = body;
    }

    public override SyntaxKind Kind => SyntaxKind.OmgClause;
    public override TextSpan Span => TextSpan.FromBounds(OmgKeyword.Position, Body.Span.End);
}

/// <summary>IM IN YR &lt;label&gt; [&lt;op&gt; YR &lt;var&gt; [TIL|WILE &lt;expr&gt;]] ... IM OUTTA YR &lt;label&gt;</summary>
public sealed class LoopStatementSyntax : StatementSyntax
{
    public SyntaxToken ImInKeyword { get; }
    public SyntaxToken LabelToken { get; }
    /// <summary>The built-in UPPIN or NERFIN operation, if present.</summary>
    public SyntaxToken? BuiltInOperationToken { get; }
    /// <summary>The custom unary function call used as the operation, if present.</summary>
    public FunctionCallExpressionSyntax? OperationCall { get; }
    /// <summary>The operation's representative token.</summary>
    public SyntaxToken? OperationToken => BuiltInOperationToken ?? OperationCall?.NameToken;
    public SyntaxToken? VariableToken { get; }
    public SyntaxToken? ConditionKeyword { get; }
    public ExpressionSyntax? Condition { get; }
    public BlockStatementSyntax Body { get; }
    public SyntaxToken ImOuttaKeyword { get; }
    public SyntaxToken EndLabelToken { get; }

    public LoopStatementSyntax(
        SyntaxToken imInKeyword,
        SyntaxToken labelToken,
        SyntaxToken? builtInOperationToken,
        FunctionCallExpressionSyntax? operationCall,
        SyntaxToken? variableToken,
        SyntaxToken? conditionKeyword,
        ExpressionSyntax? condition,
        BlockStatementSyntax body,
        SyntaxToken imOuttaKeyword,
        SyntaxToken endLabelToken)
    {
        ImInKeyword = imInKeyword;
        LabelToken = labelToken;
        BuiltInOperationToken = builtInOperationToken;
        OperationCall = operationCall;
        VariableToken = variableToken;
        ConditionKeyword = conditionKeyword;
        Condition = condition;
        Body = body;
        ImOuttaKeyword = imOuttaKeyword;
        EndLabelToken = endLabelToken;
    }

    public override SyntaxKind Kind => SyntaxKind.LoopStatement;
    public override TextSpan Span => TextSpan.FromBounds(ImInKeyword.Position, EndLabelToken.Span.End);
}

/// <summary>GTFO</summary>
public sealed class GtfoStatementSyntax : StatementSyntax
{
    public SyntaxToken Keyword { get; }

    public GtfoStatementSyntax(SyntaxToken keyword) => Keyword = keyword;

    public override SyntaxKind Kind => SyntaxKind.GtfoStatement;
    public override TextSpan Span => Keyword.Span;
}

/// <summary>HOW IZ I &lt;name&gt; [YR &lt;param&gt; [AN YR &lt;param&gt;]*] ... IF U SAY SO</summary>
public sealed class FunctionDeclarationSyntax : StatementSyntax
{
    /// <summary>The namespace in which the function is installed.</summary>
    public IdentifierSyntax Scope { get; }
    /// <summary>The runtime-resolved function name.</summary>
    public IdentifierSyntax Identifier { get; }
    public SyntaxToken NameToken { get; }
    public ImmutableArray<IdentifierSyntax> Parameters { get; }
    public BlockStatementSyntax Body { get; }
    public SyntaxToken EndKeyword { get; }

    public FunctionDeclarationSyntax(
        IdentifierSyntax scope,
        IdentifierSyntax identifier,
        SyntaxToken nameToken,
        ImmutableArray<IdentifierSyntax> parameters,
        BlockStatementSyntax body,
        SyntaxToken endKeyword)
    {
        Scope = scope;
        Identifier = identifier;
        NameToken = nameToken;
        Parameters = parameters;
        Body = body;
        EndKeyword = endKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.FunctionDeclarationStatement;
    public override TextSpan Span => TextSpan.FromBounds(NameToken.Position, EndKeyword.Span.End);
}

/// <summary>Defines and populates a BUKKIT with <c>O HAI IM ... KTHX</c>.</summary>
public sealed class ObjectDefinitionSyntax : StatementSyntax
{
    /// <summary>The name receiving the new BUKKIT.</summary>
    public IdentifierSyntax Name { get; }
    /// <summary>The optional prototype identifier.</summary>
    public IdentifierSyntax? Parent { get; }
    /// <summary>Mixin objects copied into the new BUKKIT.</summary>
    public ImmutableArray<IdentifierSyntax> Mixins { get; }
    /// <summary>The statements evaluated in the new BUKKIT namespace.</summary>
    public BlockStatementSyntax Body { get; }
    /// <summary>The closing KTHX token.</summary>
    public SyntaxToken EndKeyword { get; }

    /// <summary>Creates an alternate BUKKIT definition.</summary>
    public ObjectDefinitionSyntax(
        IdentifierSyntax name,
        IdentifierSyntax? parent,
        ImmutableArray<IdentifierSyntax> mixins,
        BlockStatementSyntax body,
        SyntaxToken endKeyword)
    {
        Name = name;
        Parent = parent;
        Mixins = mixins;
        Body = body;
        EndKeyword = endKeyword;
    }

    /// <inheritdoc/>
    public override SyntaxKind Kind => SyntaxKind.ObjectDefinitionStatement;
    /// <inheritdoc/>
    public override TextSpan Span => TextSpan.FromBounds(Name.Span.Start, EndKeyword.Span.End);
}

/// <summary>FOUND YR &lt;expr&gt;</summary>
public sealed class ReturnStatementSyntax : StatementSyntax
{
    public SyntaxToken Keyword { get; }
    public ExpressionSyntax Expression { get; }

    public ReturnStatementSyntax(SyntaxToken keyword, ExpressionSyntax expression)
    {
        Keyword = keyword;
        Expression = expression;
    }

    public override SyntaxKind Kind => SyntaxKind.ReturnStatement;
    public override TextSpan Span => TextSpan.FromBounds(Keyword.Position, Expression.Span.End);
}

/// <summary>&lt;name&gt; IS NOW A &lt;type&gt;</summary>
public sealed class CastStatementSyntax : StatementSyntax
{
    /// <summary>Gets the identifier whose value is replaced by the cast result.</summary>
    public IdentifierSyntax Target { get; }
    /// <summary>Gets the target type token.</summary>
    public SyntaxToken TypeToken { get; }

    /// <summary>Creates an in-place cast statement.</summary>
    public CastStatementSyntax(IdentifierSyntax target, SyntaxToken typeToken)
    {
        Target = target;
        TypeToken = typeToken;
    }

    public override SyntaxKind Kind => SyntaxKind.CastStatement;
    public override TextSpan Span => TextSpan.FromBounds(Target.Span.Start, TypeToken.Span.End);
}

/// <summary>A block of statements.</summary>
public sealed class BlockStatementSyntax : StatementSyntax
{
    public ImmutableArray<StatementSyntax> Statements { get; }

    public BlockStatementSyntax(ImmutableArray<StatementSyntax> statements)
    {
        Statements = statements;
    }

    public override SyntaxKind Kind => SyntaxKind.BlockStatement;
    public override TextSpan Span
    {
        get
        {
            if (Statements.Length == 0)
                return new TextSpan(0, 0);
            return TextSpan.FromBounds(Statements[0].Span.Start, Statements[^1].Span.End);
        }
    }
}

// ============ Expressions ============

/// <summary>Base class for expression syntax nodes.</summary>
public abstract class ExpressionSyntax : SyntaxNode { }

/// <summary>A literal value (NUMBR, NUMBAR, YARN, WIN, FAIL, NOOB).</summary>
public sealed class LiteralExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken Token { get; }
    public object? Value { get; }

    public LiteralExpressionSyntax(SyntaxToken token, object? value)
    {
        Token = token;
        Value = value;
    }

    public override SyntaxKind Kind => SyntaxKind.LiteralExpression;
    public override TextSpan Span => Token.Span;
}

/// <summary>A typed default value introduced by <c>A</c> and a primitive type keyword.</summary>
public sealed class TypeDefaultExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken AKeyword { get; }
    public SyntaxToken TypeToken { get; }

    public TypeDefaultExpressionSyntax(SyntaxToken aKeyword, SyntaxToken typeToken)
    {
        AKeyword = aKeyword;
        TypeToken = typeToken;
    }

    public override SyntaxKind Kind => SyntaxKind.TypeDefaultExpression;
    public override TextSpan Span => TextSpan.FromBounds(AKeyword.Position, TypeToken.Span.End);
}

/// <summary>A variable reference.</summary>
public sealed class VariableExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken NameToken { get; }

    public VariableExpressionSyntax(SyntaxToken nameToken)
    {
        NameToken = nameToken;
    }

    public override SyntaxKind Kind => SyntaxKind.VariableExpression;
    public override TextSpan Span => NameToken.Span;
}

/// <summary>NOT &lt;expr&gt;</summary>
public sealed class UnaryExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken OperatorToken { get; }
    public ExpressionSyntax Operand { get; }

    public UnaryExpressionSyntax(SyntaxToken operatorToken, ExpressionSyntax operand)
    {
        OperatorToken = operatorToken;
        Operand = operand;
    }

    public override SyntaxKind Kind => SyntaxKind.UnaryExpression;
    public override TextSpan Span => TextSpan.FromBounds(OperatorToken.Position, Operand.Span.End);
}

/// <summary>Binary operator: SUM OF x AN y, etc.</summary>
public sealed class BinaryExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken OperatorToken { get; }
    public ExpressionSyntax Left { get; }
    public ExpressionSyntax Right { get; }

    public BinaryExpressionSyntax(SyntaxToken operatorToken, ExpressionSyntax left, ExpressionSyntax right)
    {
        OperatorToken = operatorToken;
        Left = left;
        Right = right;
    }

    public override SyntaxKind Kind => SyntaxKind.BinaryExpression;
    public override TextSpan Span => TextSpan.FromBounds(OperatorToken.Position, Right.Span.End);
}

/// <summary>SMOOSH &lt;expr&gt;+ [MKAY]</summary>
public sealed class SmooshExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken Keyword { get; }
    public ImmutableArray<ExpressionSyntax> Operands { get; }

    public SmooshExpressionSyntax(SyntaxToken keyword, ImmutableArray<ExpressionSyntax> operands)
    {
        Keyword = keyword;
        Operands = operands;
    }

    public override SyntaxKind Kind => SyntaxKind.SmooshExpression;
    public override TextSpan Span
    {
        get
        {
            int end = Operands.Length > 0 ? Operands[^1].Span.End : Keyword.Span.End;
            return TextSpan.FromBounds(Keyword.Position, end);
        }
    }
}

/// <summary>ALL OF &lt;expr&gt;+ MKAY — variadic AND.</summary>
public sealed class AllOfExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken Keyword { get; }
    public ImmutableArray<ExpressionSyntax> Operands { get; }

    public AllOfExpressionSyntax(SyntaxToken keyword, ImmutableArray<ExpressionSyntax> operands)
    {
        Keyword = keyword;
        Operands = operands;
    }

    public override SyntaxKind Kind => SyntaxKind.AllOfExpression;
    public override TextSpan Span
    {
        get
        {
            int end = Operands.Length > 0 ? Operands[^1].Span.End : Keyword.Span.End;
            return TextSpan.FromBounds(Keyword.Position, end);
        }
    }
}

/// <summary>ANY OF &lt;expr&gt;+ MKAY — variadic OR.</summary>
public sealed class AnyOfExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken Keyword { get; }
    public ImmutableArray<ExpressionSyntax> Operands { get; }

    public AnyOfExpressionSyntax(SyntaxToken keyword, ImmutableArray<ExpressionSyntax> operands)
    {
        Keyword = keyword;
        Operands = operands;
    }

    public override SyntaxKind Kind => SyntaxKind.AnyOfExpression;
    public override TextSpan Span
    {
        get
        {
            int end = Operands.Length > 0 ? Operands[^1].Span.End : Keyword.Span.End;
            return TextSpan.FromBounds(Keyword.Position, end);
        }
    }
}

/// <summary>BOTH SAEM &lt;expr&gt; AN &lt;expr&gt;</summary>
public sealed class ComparisonExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken Keyword { get; }
    public ExpressionSyntax Left { get; }
    public ExpressionSyntax Right { get; }

    public ComparisonExpressionSyntax(SyntaxToken keyword, ExpressionSyntax left, ExpressionSyntax right)
    {
        Keyword = keyword;
        Left = left;
        Right = right;
    }

    public override SyntaxKind Kind => SyntaxKind.ComparisonExpression;
    public override TextSpan Span => TextSpan.FromBounds(Keyword.Position, Right.Span.End);
}

/// <summary>DIFFRINT &lt;expr&gt; AN &lt;expr&gt;</summary>
public sealed class DiffrintExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken Keyword { get; }
    public ExpressionSyntax Left { get; }
    public ExpressionSyntax Right { get; }

    public DiffrintExpressionSyntax(SyntaxToken keyword, ExpressionSyntax left, ExpressionSyntax right)
    {
        Keyword = keyword;
        Left = left;
        Right = right;
    }

    public override SyntaxKind Kind => SyntaxKind.DiffrintExpression;
    public override TextSpan Span => TextSpan.FromBounds(Keyword.Position, Right.Span.End);
}

/// <summary>MAEK &lt;expr&gt; [A] &lt;type&gt;</summary>
public sealed class CastExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken Keyword { get; }
    public ExpressionSyntax Operand { get; }
    public SyntaxToken TypeToken { get; }

    public CastExpressionSyntax(SyntaxToken keyword, ExpressionSyntax operand, SyntaxToken typeToken)
    {
        Keyword = keyword;
        Operand = operand;
        TypeToken = typeToken;
    }

    public override SyntaxKind Kind => SyntaxKind.CastExpression;
    public override TextSpan Span => TextSpan.FromBounds(Keyword.Position, TypeToken.Span.End);
}

/// <summary>I IZ &lt;name&gt; [YR &lt;expr&gt; [AN YR &lt;expr&gt;]*] MKAY</summary>
public sealed class FunctionCallExpressionSyntax : ExpressionSyntax
{
    /// <summary>The namespace from which the callable is selected.</summary>
    public IdentifierSyntax Scope { get; }
    /// <summary>The runtime-resolved callable identifier.</summary>
    public IdentifierSyntax Identifier { get; }
    public SyntaxToken NameToken { get; }
    public ImmutableArray<ExpressionSyntax> Arguments { get; }

    public FunctionCallExpressionSyntax(
        IdentifierSyntax scope,
        IdentifierSyntax identifier,
        SyntaxToken nameToken,
        ImmutableArray<ExpressionSyntax> arguments)
    {
        Scope = scope;
        Identifier = identifier;
        NameToken = nameToken;
        Arguments = arguments;
    }

    public override SyntaxKind Kind => SyntaxKind.FunctionCallExpression;
    public override TextSpan Span
    {
        get
        {
            int end = Arguments.Length > 0 ? Arguments[^1].Span.End : NameToken.Span.End;
            return TextSpan.FromBounds(NameToken.Position, end);
        }
    }
}

/// <summary>A runtime-resolved variable or BUKKIT slot reference.</summary>
public sealed class IdentifierExpressionSyntax : ExpressionSyntax
{
        /// <summary>The referenced identifier.</summary>
        public IdentifierSyntax Identifier { get; }

        /// <summary>Creates an identifier expression.</summary>
        public IdentifierExpressionSyntax(IdentifierSyntax identifier) => Identifier = identifier;

        /// <inheritdoc/>
        public override SyntaxKind Kind => SyntaxKind.IdentifierExpression;
        /// <inheritdoc/>
        public override TextSpan Span => Identifier.Span;
}

/// <summary>Creates a BUKKIT, optionally with a prototype and copied mixins.</summary>
public sealed class ObjectCreationExpressionSyntax : ExpressionSyntax
{
        /// <summary>The optional prototype.</summary>
        public IdentifierSyntax? Parent { get; }
        /// <summary>Mixin objects copied into the new BUKKIT.</summary>
        public ImmutableArray<IdentifierSyntax> Mixins { get; }
        /// <summary>The first token in the creation expression.</summary>
        public SyntaxToken StartToken { get; }

        /// <summary>Creates a BUKKIT creation expression.</summary>
        public ObjectCreationExpressionSyntax(
            SyntaxToken startToken,
            IdentifierSyntax? parent,
            ImmutableArray<IdentifierSyntax> mixins)
        {
            StartToken = startToken;
            Parent = parent;
            Mixins = mixins;
        }

        /// <inheritdoc/>
        public override SyntaxKind Kind => SyntaxKind.ObjectCreationExpression;
        /// <inheritdoc/>
        public override TextSpan Span
        {
            get
            {
                int end = Mixins.Length > 0 ? Mixins[^1].Span.End : Parent?.Span.End ?? StartToken.Span.End;
                return TextSpan.FromBounds(StartToken.Position, end);
        }
    }
}

/// <summary>Implicit IT variable reference.</summary>
public sealed class ItExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken Token { get; }

    public ItExpressionSyntax(SyntaxToken token) => Token = token;

    public override SyntaxKind Kind => SyntaxKind.ItExpression;
    public override TextSpan Span => Token.Span;
}
