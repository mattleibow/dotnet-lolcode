using System.Collections.Immutable;
using Lolcode.Compiler.Syntax;
using Lolcode.Compiler.Text;

namespace Lolcode.Compiler.Binding;

/// <summary>
/// Context tracking for GTFO resolution.
/// </summary>
internal enum ControlFlowContext
{
    None,
    Loop,
    Switch,
    Function
}

/// <summary>
/// Performs semantic analysis on a parsed syntax tree, producing a bound tree.
/// Resolves variable references, validates control flow, and checks types.
/// </summary>
public sealed class Binder
{
    private readonly DiagnosticBag _diagnostics = new();
    private readonly SourceText _text;
    private readonly Dictionary<string, bool> _variables = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _functions = new(StringComparer.Ordinal);
    private readonly Stack<ControlFlowContext> _contextStack = new();

    /// <summary>
    /// Gets the diagnostics produced during binding.
    /// </summary>
    public IEnumerable<Diagnostic> Diagnostics => _diagnostics;

    /// <summary>
    /// Creates a new binder for the given source text.
    /// </summary>
    public Binder(SourceText text)
    {
        _text = text;
        // IT is always available
        _variables["IT"] = true;
    }

    /// <summary>
    /// Binds a compilation unit to a <see cref="BoundBlockStatement"/>.
    /// </summary>
    public BoundBlockStatement BindCompilationUnit(CompilationUnitSyntax compilationUnit)
    {
        // First pass: collect all function declarations
        CollectFunctions(compilationUnit.Program.Statements);

        // Second pass: bind all statements
        return BindBlock(compilationUnit.Program.Statements);
    }

    private void CollectFunctions(ImmutableArray<StatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
            if (statement is FunctionDeclarationSyntax funcDecl)
            {
                string name = funcDecl.NameToken.Text;
                int paramCount = funcDecl.Parameters.Length;

                if (_functions.ContainsKey(name))
                {
                    var location = TextLocation.FromSpan(_text, funcDecl.NameToken.Span);
                    _diagnostics.ReportVariableAlreadyDeclared(location, name);
                }
                else
                {
                    _functions[name] = paramCount;
                }
            }
        }
    }

    private BoundBlockStatement BindBlock(ImmutableArray<StatementSyntax> statements)
    {
        var boundStatements = ImmutableArray.CreateBuilder<BoundStatement>();

        foreach (var statement in statements)
        {
            var bound = BindStatement(statement);
            if (bound != null)
                boundStatements.Add(bound);
        }

        return new BoundBlockStatement(boundStatements.ToImmutable());
    }

    private BoundStatement? BindStatement(StatementSyntax statement)
    {
        return statement switch
        {
            VariableDeclarationSyntax s => BindVariableDeclaration(s),
            AssignmentStatementSyntax s => BindAssignment(s),
            VisibleStatementSyntax s => BindVisible(s),
            GimmehStatementSyntax s => BindGimmeh(s),
            ExpressionStatementSyntax s => BindExpressionStatement(s),
            IfStatementSyntax s => BindIf(s),
            SwitchStatementSyntax s => BindSwitch(s),
            LoopStatementSyntax s => BindLoop(s),
            GtfoStatementSyntax s => BindGtfo(s),
            FunctionDeclarationSyntax s => BindFunctionDeclaration(s),
            ReturnStatementSyntax s => BindReturn(s),
            CastStatementSyntax s => BindCastStatement(s),
            _ => null,
        };
    }

    private BoundVariableDeclaration BindVariableDeclaration(VariableDeclarationSyntax syntax)
    {
        string name = syntax.NameToken.Text;

        if (_variables.ContainsKey(name))
        {
            var location = TextLocation.FromSpan(_text, syntax.NameToken.Span);
            _diagnostics.ReportVariableAlreadyDeclared(location, name);
        }

        _variables[name] = true;

        BoundExpression? initializer = null;
        if (syntax.Initializer != null)
            initializer = BindExpression(syntax.Initializer);

        return new BoundVariableDeclaration(name, initializer);
    }

    private BoundAssignment BindAssignment(AssignmentStatementSyntax syntax)
    {
        string name = syntax.NameToken.Text;

        if (!_variables.ContainsKey(name))
        {
            var location = TextLocation.FromSpan(_text, syntax.NameToken.Span);
            _diagnostics.ReportUndeclaredVariable(location, name);
        }

        var expression = BindExpression(syntax.Expression);
        return new BoundAssignment(name, expression);
    }

    private BoundVisibleStatement BindVisible(VisibleStatementSyntax syntax)
    {
        var args = syntax.Arguments.Select(BindExpression).ToImmutableArray();
        return new BoundVisibleStatement(args, syntax.SuppressNewline);
    }

    private BoundGimmehStatement BindGimmeh(GimmehStatementSyntax syntax)
    {
        string name = syntax.NameToken.Text;
        if (!_variables.ContainsKey(name))
        {
            var location = TextLocation.FromSpan(_text, syntax.NameToken.Span);
            _diagnostics.ReportUndeclaredVariable(location, name);
        }
        return new BoundGimmehStatement(name);
    }

    private BoundExpressionStatement BindExpressionStatement(ExpressionStatementSyntax syntax)
    {
        var expression = BindExpression(syntax.Expression);
        return new BoundExpressionStatement(expression);
    }

    private BoundIfStatement BindIf(IfStatementSyntax syntax)
    {
        var thenBlock = BindBlock(syntax.YaRlyBody.Statements);

        var mebbeClauses = syntax.MebbeClauses.Select(m =>
        {
            var condition = BindExpression(m.Condition);
            var body = BindBlock(m.Body.Statements);
            return new BoundMebbeClause(condition, body);
        }).ToImmutableArray();

        BoundBlockStatement? elseBlock = null;
        if (syntax.NoWaiBody != null)
            elseBlock = BindBlock(syntax.NoWaiBody.Statements);

        return new BoundIfStatement(thenBlock, mebbeClauses, elseBlock);
    }

    private BoundSwitchStatement BindSwitch(SwitchStatementSyntax syntax)
    {
        var seenValues = new HashSet<string>();
        var omgClauses = ImmutableArray.CreateBuilder<BoundOmgClause>();

        _contextStack.Push(ControlFlowContext.Switch);

        foreach (var clause in syntax.OmgClauses)
        {
            var value = BindExpression(clause.Value);

            // Validate literal-only and uniqueness
            if (value is BoundLiteralExpression lit)
            {
                string key = lit.Value?.ToString() ?? "NOOB";
                if (!seenValues.Add(key))
                {
                    var location = TextLocation.FromSpan(_text, clause.Value.Span);
                    _diagnostics.ReportDuplicateOmgLiteral(location, key);
                }
            }
            else
            {
                var location = TextLocation.FromSpan(_text, clause.Value.Span);
                _diagnostics.ReportOmgRequiresLiteral(location);
            }

            var body = BindBlock(clause.Body.Statements);
            object? literalValue = value is BoundLiteralExpression l ? l.Value : null;
            omgClauses.Add(new BoundOmgClause(literalValue, body));
        }

        BoundBlockStatement? defaultBlock = null;
        if (syntax.OmgwtfBody != null)
            defaultBlock = BindBlock(syntax.OmgwtfBody.Statements);

        _contextStack.Pop();

        return new BoundSwitchStatement(omgClauses.ToImmutable(), defaultBlock);
    }

    private BoundLoopStatement BindLoop(LoopStatementSyntax syntax)
    {
        string label = syntax.LabelToken.Text;
        string? operation = syntax.OperationToken?.Text;
        string? variableName = syntax.VariableToken?.Text;
        bool? isTil = null;
        BoundExpression? condition = null;

        // Loop variable is local to the loop
        if (variableName != null)
            _variables[variableName] = true;

        if (syntax.ConditionKeyword != null)
        {
            isTil = syntax.ConditionKeyword.Kind == SyntaxKind.TilKeyword;
            condition = BindExpression(syntax.Condition!);
        }

        _contextStack.Push(ControlFlowContext.Loop);
        var body = BindBlock(syntax.Body.Statements);
        _contextStack.Pop();

        return new BoundLoopStatement(label, operation, variableName, isTil, condition, body);
    }

    private BoundGtfoStatement BindGtfo(GtfoStatementSyntax syntax)
    {
        string context;
        if (_contextStack.Count == 0)
        {
            // In top-level code — invalid
            var location = TextLocation.FromSpan(_text, syntax.Keyword.Span);
            _diagnostics.ReportInvalidGtfo(location);
            context = "none";
        }
        else
        {
            context = _contextStack.Peek() switch
            {
                ControlFlowContext.Loop => "loop",
                ControlFlowContext.Switch => "switch",
                ControlFlowContext.Function => "function",
                _ => "none"
            };
        }

        return new BoundGtfoStatement(context);
    }

    private BoundFunctionDeclaration BindFunctionDeclaration(FunctionDeclarationSyntax syntax)
    {
        string name = syntax.NameToken.Text;
        var parameters = syntax.Parameters.Select(p => p.Text).ToImmutableArray();

        // Create a new scope for the function
        var savedVariables = new Dictionary<string, bool>(_variables);
        _variables.Clear();
        _variables["IT"] = true;

        foreach (var param in parameters)
            _variables[param] = true;

        _contextStack.Push(ControlFlowContext.Function);
        var body = BindBlock(syntax.Body.Statements);
        _contextStack.Pop();

        // Restore outer scope
        _variables.Clear();
        foreach (var kv in savedVariables)
            _variables[kv.Key] = kv.Value;

        return new BoundFunctionDeclaration(name, parameters, body);
    }

    private BoundReturnStatement BindReturn(ReturnStatementSyntax syntax)
    {
        if (!_contextStack.Contains(ControlFlowContext.Function))
        {
            var location = TextLocation.FromSpan(_text, syntax.Keyword.Span);
            _diagnostics.ReportInvalidFoundYr(location);
        }

        var expression = BindExpression(syntax.Expression);
        return new BoundReturnStatement(expression);
    }

    private BoundCastStatement BindCastStatement(CastStatementSyntax syntax)
    {
        string name = syntax.NameToken.Text;
        if (!_variables.ContainsKey(name))
        {
            var location = TextLocation.FromSpan(_text, syntax.NameToken.Span);
            _diagnostics.ReportUndeclaredVariable(location, name);
        }

        string targetType = syntax.TypeToken.Text;
        return new BoundCastStatement(name, targetType);
    }

    private BoundExpression BindExpression(ExpressionSyntax syntax)
    {
        return syntax switch
        {
            LiteralExpressionSyntax s => new BoundLiteralExpression(s.Value),
            VariableExpressionSyntax s => BindVariableExpression(s),
            UnaryExpressionSyntax s => BindUnary(s),
            BinaryExpressionSyntax s => BindBinary(s),
            SmooshExpressionSyntax s => BindSmoosh(s),
            AllOfExpressionSyntax s => BindAllOf(s),
            AnyOfExpressionSyntax s => BindAnyOf(s),
            ComparisonExpressionSyntax s => new BoundComparisonExpression(true, BindExpression(s.Left), BindExpression(s.Right)),
            DiffrintExpressionSyntax s => new BoundComparisonExpression(false, BindExpression(s.Left), BindExpression(s.Right)),
            CastExpressionSyntax s => new BoundCastExpression(BindExpression(s.Operand), s.TypeToken.Text),
            FunctionCallExpressionSyntax s => BindFunctionCall(s),
            ItExpressionSyntax => new BoundItExpression(),
            _ => new BoundLiteralExpression(null),
        };
    }

    private BoundExpression BindVariableExpression(VariableExpressionSyntax syntax)
    {
        string name = syntax.NameToken.Text;
        if (!_variables.ContainsKey(name))
        {
            var location = TextLocation.FromSpan(_text, syntax.NameToken.Span);
            _diagnostics.ReportUndeclaredVariable(location, name);
        }
        return new BoundVariableExpression(name);
    }

    private BoundUnaryExpression BindUnary(UnaryExpressionSyntax syntax)
    {
        var operand = BindExpression(syntax.Operand);
        return new BoundUnaryExpression(operand);
    }

    private BoundBinaryExpression BindBinary(BinaryExpressionSyntax syntax)
    {
        string op = syntax.OperatorToken.Text;
        var left = BindExpression(syntax.Left);
        var right = BindExpression(syntax.Right);
        return new BoundBinaryExpression(op, left, right);
    }

    private BoundSmooshExpression BindSmoosh(SmooshExpressionSyntax syntax)
    {
        var operands = syntax.Operands.Select(BindExpression).ToImmutableArray();
        return new BoundSmooshExpression(operands);
    }

    private BoundAllOfExpression BindAllOf(AllOfExpressionSyntax syntax)
    {
        var operands = syntax.Operands.Select(BindExpression).ToImmutableArray();
        return new BoundAllOfExpression(operands);
    }

    private BoundAnyOfExpression BindAnyOf(AnyOfExpressionSyntax syntax)
    {
        var operands = syntax.Operands.Select(BindExpression).ToImmutableArray();
        return new BoundAnyOfExpression(operands);
    }

    private BoundFunctionCallExpression BindFunctionCall(FunctionCallExpressionSyntax syntax)
    {
        string name = syntax.NameToken.Text;

        if (!_functions.ContainsKey(name))
        {
            var location = TextLocation.FromSpan(_text, syntax.NameToken.Span);
            _diagnostics.ReportUndefinedFunction(location, name);
        }
        else
        {
            int expectedArgs = _functions[name];
            if (syntax.Arguments.Length != expectedArgs)
            {
                var location = TextLocation.FromSpan(_text, syntax.NameToken.Span);
                _diagnostics.ReportWrongArgumentCount(location, name, expectedArgs, syntax.Arguments.Length);
            }
        }

        var args = syntax.Arguments.Select(BindExpression).ToImmutableArray();
        return new BoundFunctionCallExpression(name, args);
    }
}
