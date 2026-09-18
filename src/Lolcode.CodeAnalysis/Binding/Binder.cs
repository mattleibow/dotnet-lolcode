using System.Collections.Immutable;
using Lolcode.CodeAnalysis.BoundTree;
using Lolcode.CodeAnalysis.Symbols;
using Lolcode.CodeAnalysis.Syntax;
using Lolcode.CodeAnalysis.Text;
using Lolcode.Runtime;

namespace Lolcode.CodeAnalysis.Binding;

/// <summary>
/// Performs semantic analysis on a parsed syntax tree, producing a bound tree.
/// Resolves variable references, validates control flow, and checks types.
/// </summary>
internal sealed class Binder
{
    private readonly DiagnosticBag _diagnostics = new();
    private readonly SourceText _text;
    private BoundScope _scope;
    private readonly Stack<ControlFlowContext> _contextStack = new();
    private bool _runtimeIdentifiers;

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
        _scope = new BoundScope();
    }

    /// <summary>
    /// Binds a compilation unit to a <see cref="BoundBlockStatement"/>.
    /// </summary>
    public BoundBlockStatement BindCompilationUnit(CompilationUnitSyntax compilationUnit)
    {
        _runtimeIdentifiers = compilationUnit.Program.VersionToken?.Text is "1.3" or "1.4";
        return BindBlock(compilationUnit.Program.Statements);
    }

    private void CollectFunctions(ImmutableArray<StatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
            if (statement is FunctionDeclarationSyntax funcDecl)
            {
                if (funcDecl.Scope.DirectToken?.Text != "I" ||
                    funcDecl.Identifier.DirectToken is null ||
                    funcDecl.Identifier.Slot is not null)
                    continue;

                string name = funcDecl.Identifier.DirectToken.Text;
                var parameters = funcDecl.Parameters.Select((p, i) =>
                    new ParameterSymbol(p.DirectToken?.Text ?? $"arg{i}", i)).ToImmutableArray();

                var function = new FunctionSymbol(name, parameters);

                if (!_scope.TryDeclareFunction(function))
                {
                    var location = TextLocation.FromSpan(_text, funcDecl.NameToken.Span);
                    _diagnostics.ReportFunctionAlreadyDeclared(location, name);
                }
            }
        }
    }

    private BoundBlockStatement BindBlock(ImmutableArray<StatementSyntax> statements)
    {
        CollectFunctions(statements);
        var boundStatements = ImmutableArray.CreateBuilder<BoundStatement>();

        foreach (var statement in statements)
        {
            var bound = BindStatement(statement);
            if (bound != null)
                boundStatements.Add(bound);
        }

        return new BoundBlockStatement(boundStatements.ToImmutable());
    }

    private BoundBlockStatement BindNestedBlock(ImmutableArray<StatementSyntax> statements)
    {
        var outer = _scope;
        _scope = new BoundScope(outer, inheritsVariables: true);
        var result = BindBlock(statements);
        _scope = outer;
        return result;
    }

    private BoundStatement? BindStatement(StatementSyntax statement)
    {
        return statement switch
        {
            VariableDeclarationSyntax s => BindVariableDeclaration(s),
            ScopedDeclarationSyntax s => BindScopedDeclaration(s),
            AssignmentStatementSyntax s => BindAssignment(s),
            IdentifierAssignmentSyntax s => new BoundIdentifierAssignment(
                BindIdentifier(s.Target), BindExpression(s.Expression), syntax: s),
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
            ObjectDefinitionSyntax s => BindObjectDefinition(s),
            ImportStatementSyntax s => new BoundImportStatement(BindIdentifier(s.Library), s),
            _ => null,
        };
    }

    private BoundScopedDeclaration BindScopedDeclaration(ScopedDeclarationSyntax syntax)
    {
        if (syntax.Scope.DirectToken?.Text == "I" &&
            syntax.Scope.Slot is null &&
            syntax.Name.DirectToken is { } nameToken &&
            syntax.Name.Slot is null)
        {
            DeclareLocalVariable(nameToken);
        }

        return new BoundScopedDeclaration(
            BindIdentifier(syntax.Scope),
            BindIdentifier(syntax.Name),
            syntax.Initializer is null ? null : BindExpression(syntax.Initializer),
            syntax);
    }

    private BoundObjectDefinition BindObjectDefinition(ObjectDefinitionSyntax syntax)
    {
        if (syntax.Name.DirectToken is { } nameToken && syntax.Name.Slot is null)
            DeclareLocalVariable(nameToken);

        var name = BindIdentifier(syntax.Name);
        var parent = syntax.Parent is null ? null : BindIdentifier(syntax.Parent);
        var mixins = syntax.Mixins.Select(BindIdentifier).ToImmutableArray();
        var outerScope = _scope;
        _scope = new BoundScope(outerScope);
        var body = BindBlock(syntax.Body.Statements);
        _scope = outerScope;
        return new BoundObjectDefinition(
            name,
            parent,
            mixins,
            body,
            syntax);
    }

    private BoundIdentifier BindIdentifier(IdentifierSyntax syntax) =>
        new(
            syntax.DirectToken?.Text,
            syntax.NameExpression is null ? null : BindExpression(syntax.NameExpression),
            syntax.Slot is null ? null : BindIdentifier(syntax.Slot));

    private BoundVariableDeclaration BindVariableDeclaration(VariableDeclarationSyntax syntax)
    {
        string name = syntax.NameToken.Text;
        var variable = new VariableSymbol(name);

        if (!_scope.TryDeclareVariable(variable))
        {
            var location = TextLocation.FromSpan(_text, syntax.NameToken.Span);
            _diagnostics.ReportVariableAlreadyDeclared(location, name);
        }

        BoundExpression? initializer = null;
        if (syntax.Initializer != null)
            initializer = BindExpression(syntax.Initializer);

        return new BoundVariableDeclaration(variable, initializer, syntax: syntax);
    }

    private void DeclareLocalVariable(SyntaxToken nameToken)
    {
        if (_scope.TryDeclareVariable(new VariableSymbol(nameToken.Text)))
            return;

        var location = TextLocation.FromSpan(_text, nameToken.Span);
        _diagnostics.ReportVariableAlreadyDeclared(location, nameToken.Text);
    }

    private BoundAssignment BindAssignment(AssignmentStatementSyntax syntax)
    {
        string name = syntax.NameToken.Text;

        if (!_scope.TryLookupVariable(name, out var variable))
        {
            if (!_runtimeIdentifiers)
            {
                var location = TextLocation.FromSpan(_text, syntax.NameToken.Span);
                _diagnostics.ReportUndeclaredVariable(location, name);
            }
            variable = new VariableSymbol(name);
        }

        var expression = BindExpression(syntax.Expression);
        return new BoundAssignment(variable, expression, syntax: syntax);
    }

    private BoundVisibleStatement BindVisible(VisibleStatementSyntax syntax)
    {
        var args = syntax.Arguments.Select(BindExpression).ToImmutableArray();
        return new BoundVisibleStatement(
            args,
            syntax.SuppressNewline,
            syntax.WritesToStandardError,
            syntax);
    }

    private BoundGimmehStatement BindGimmeh(GimmehStatementSyntax syntax)
    {
        ValidateMutableTarget(syntax.Target);
        return new BoundGimmehStatement(BindIdentifier(syntax.Target), syntax: syntax);
    }

    private BoundExpressionStatement BindExpressionStatement(ExpressionStatementSyntax syntax)
    {
        var expression = BindExpression(syntax.Expression);
        return new BoundExpressionStatement(expression, syntax: syntax);
    }

    private BoundIfStatement BindIf(IfStatementSyntax syntax)
    {
        var thenBlock = BindNestedBlock(syntax.YaRlyBody.Statements);

        var mebbeClauses = syntax.MebbeClauses.Select(m =>
        {
            var condition = BindExpression(m.Condition);
            var body = BindNestedBlock(m.Body.Statements);
            return new BoundMebbeClause(condition, body, syntax: m);
        }).ToImmutableArray();

        BoundBlockStatement? elseBlock = null;
        if (syntax.NoWaiBody != null)
            elseBlock = BindNestedBlock(syntax.NoWaiBody.Statements);

        return new BoundIfStatement(thenBlock, mebbeClauses, elseBlock, syntax: syntax);
    }

    private BoundSwitchStatement BindSwitch(SwitchStatementSyntax syntax)
    {
        var seenValues = new HashSet<(Type? Type, object? Value)>();
        var omgClauses = ImmutableArray.CreateBuilder<BoundOmgClause>();

        _contextStack.Push(ControlFlowContext.Switch);

        foreach (var clause in syntax.OmgClauses)
        {
            var value = BindExpression(clause.Value);

            // Validate literal-only and uniqueness
            if (value is BoundLiteralExpression lit)
            {
                object? keyValue = lit.Value;
                if (keyValue is string yarn)
                {
                    try
                    {
                        keyValue = LolRuntime.ResolveYarnLiteral(yarn);
                    }
                    catch (LolRuntimeException)
                    {
                        // Invalid escapes remain runtime errors when the case is evaluated.
                    }
                }

                var key = (keyValue?.GetType(), keyValue);
                if (!seenValues.Add(key))
                {
                    var location = TextLocation.FromSpan(_text, clause.Value.Span);
                    _diagnostics.ReportDuplicateOmgLiteral(
                        location,
                        lit.Value?.ToString() ?? "NOOB");
                }
            }
            else
            {
                var location = TextLocation.FromSpan(_text, clause.Value.Span);
                _diagnostics.ReportOmgRequiresLiteral(location);
            }

            var body = BindNestedBlock(clause.Body.Statements);
            object? literalValue = value is BoundLiteralExpression l ? l.Value : null;
            omgClauses.Add(new BoundOmgClause(literalValue, body, syntax: clause));
        }

        BoundBlockStatement? defaultBlock = null;
        if (syntax.OmgwtfBody != null)
            defaultBlock = BindNestedBlock(syntax.OmgwtfBody.Statements);

        _contextStack.Pop();

        return new BoundSwitchStatement(omgClauses.ToImmutable(), defaultBlock, syntax: syntax);
    }

    private BoundLoopStatement BindLoop(LoopStatementSyntax syntax)
    {
        string label = syntax.LabelToken.Text;
        string? operation = syntax.BuiltInOperationToken?.Text;
        string? variableName = syntax.VariableToken?.Text;
        bool? isTil = null;
        BoundExpression? condition = null;
        VariableSymbol? loopVariable = null;
        BoundFunctionCallExpression? operationCall = null;
        var outerScope = _scope;
        _scope = new BoundScope(outerScope, inheritsVariables: true);

        if (variableName != null)
        {
            loopVariable = new VariableSymbol(variableName);
            _scope.TryDeclareVariable(loopVariable);

            if (syntax.OperationCall is not null)
                operationCall = BindFunctionCall(syntax.OperationCall);
        }

        try
        {
            if (syntax.ConditionKeyword != null)
            {
                isTil = syntax.ConditionKeyword.Kind == SyntaxKind.TilKeyword;
                condition = BindExpression(syntax.Condition!);
            }

            _contextStack.Push(ControlFlowContext.Loop);
            BoundBlockStatement body;
            try
            {
                body = BindNestedBlock(syntax.Body.Statements);
            }
            finally
            {
                _contextStack.Pop();
            }

            return new BoundLoopStatement(
                label, operation, operationCall, loopVariable,
                isTil, condition, body, syntax: syntax);
        }
        finally
        {
            _scope = outerScope;
        }
    }

    private BoundGtfoStatement BindGtfo(GtfoStatementSyntax syntax)
    {
        ControlFlowContext context;
        if (_contextStack.Count == 0)
        {
            var location = TextLocation.FromSpan(_text, syntax.Keyword.Span);
            _diagnostics.ReportInvalidGtfo(location);
            context = ControlFlowContext.None;
        }
        else
        {
            context = _contextStack.Peek();
        }

        return new BoundGtfoStatement(context, syntax: syntax);
    }

    private BoundFunctionDeclaration BindFunctionDeclaration(FunctionDeclarationSyntax syntax)
    {
        string name = syntax.NameToken.Text;

        if (!_scope.TryLookupLocalFunction(name, out var function))
        {
            // Should have been collected in first pass; create a placeholder
            var parameters = syntax.Parameters.Select((p, i) =>
                new ParameterSymbol(p.DirectToken?.Text ?? $"arg{i}", i)).ToImmutableArray();
            function = new FunctionSymbol(name, parameters);
        }

        // Create a new scope for the function (chained to global for function visibility)
        var outerScope = _scope;
        _scope = new BoundScope(outerScope);

        for (int index = 0; index < function.Parameters.Length; index++)
        {
            var parameter = function.Parameters[index];
            if (_scope.TryDeclareVariable(new VariableSymbol(parameter.Name)))
                continue;

            var location = TextLocation.FromSpan(_text, syntax.Parameters[index].Span);
            _diagnostics.ReportVariableAlreadyDeclared(location, parameter.Name);
        }

        _contextStack.Push(ControlFlowContext.Function);
        var body = BindBlock(syntax.Body.Statements);
        _contextStack.Pop();

        // Restore outer scope
        _scope = outerScope;

        return new BoundFunctionDeclaration(
            function,
            body,
            syntax: syntax,
            scope: BindIdentifier(syntax.Scope),
            identifier: BindIdentifier(syntax.Identifier),
            parameterIdentifiers: syntax.Parameters.Select(BindIdentifier).ToImmutableArray());
    }

    private BoundReturnStatement BindReturn(ReturnStatementSyntax syntax)
    {
        if (!_contextStack.Contains(ControlFlowContext.Function))
        {
            var location = TextLocation.FromSpan(_text, syntax.Keyword.Span);
            _diagnostics.ReportInvalidFoundYr(location);
        }

        var expression = BindExpression(syntax.Expression);
        return new BoundReturnStatement(expression, syntax: syntax);
    }

    private BoundCastStatement BindCastStatement(CastStatementSyntax syntax)
    {
        ValidateMutableTarget(syntax.Target);
        return new BoundCastStatement(
            BindIdentifier(syntax.Target), syntax.TypeToken.Text, syntax: syntax);
    }

    private void ValidateMutableTarget(IdentifierSyntax target)
    {
        if (target.DirectToken is not { } token || target.Slot is not null)
            return;
        if (_scope.TryLookupVariable(token.Text, out _) ||
            (_runtimeIdentifiers && _scope.TryLookupFunction(token.Text, out _)))
            return;

        var location = TextLocation.FromSpan(_text, token.Span);
        _diagnostics.ReportUndeclaredVariable(location, token.Text);
    }

    private BoundExpression BindExpression(ExpressionSyntax syntax)
    {
        return syntax switch
        {
            LiteralExpressionSyntax s => BindLiteral(s),
            TypeDefaultExpressionSyntax s => BindTypeDefault(s),
            VariableExpressionSyntax s => BindVariableExpression(s),
            UnaryExpressionSyntax s => BindUnary(s),
            BinaryExpressionSyntax s => BindBinary(s),
            SmooshExpressionSyntax s => BindSmoosh(s),
            AllOfExpressionSyntax s => BindAllOf(s),
            AnyOfExpressionSyntax s => BindAnyOf(s),
            ComparisonExpressionSyntax s => new BoundComparisonExpression(true, BindExpression(s.Left), BindExpression(s.Right), syntax: s),
            DiffrintExpressionSyntax s => new BoundComparisonExpression(false, BindExpression(s.Left), BindExpression(s.Right), syntax: s),
            CastExpressionSyntax s => new BoundCastExpression(BindExpression(s.Operand), s.TypeToken.Text, syntax: s),
            FunctionCallExpressionSyntax s => BindFunctionCall(s),
            IdentifierExpressionSyntax s => BindIdentifierExpression(s),
            ObjectCreationExpressionSyntax s => new BoundObjectCreationExpression(
                s.Parent is null ? null : BindIdentifier(s.Parent),
                s.Mixins.Select(BindIdentifier).ToImmutableArray(),
                syntax: s),
            SystemCommandExpressionSyntax s =>
                new BoundSystemCommandExpression(BindExpression(s.Command), s),
            ItExpressionSyntax s => new BoundItExpression(syntax: s),
            _ => new BoundLiteralExpression(null),
        };
    }

    private BoundExpression BindIdentifierExpression(IdentifierExpressionSyntax syntax)
    {
        if (syntax.Identifier.DirectToken is { } token && syntax.Identifier.Slot is null)
        {
            string name = token.Text;
            if (_scope.TryLookupVariable(name, out var variable))
                return new BoundVariableExpression(variable, syntax);
            if (_scope.TryLookupFunction(name, out _))
                return new BoundIdentifierExpression(BindIdentifier(syntax.Identifier), syntax);

            if (!_runtimeIdentifiers)
            {
                var location = TextLocation.FromSpan(_text, token.Span);
                _diagnostics.ReportUndeclaredVariable(location, name);
            }
        }
        return new BoundIdentifierExpression(BindIdentifier(syntax.Identifier), syntax);
    }

    private BoundExpression BindLiteral(LiteralExpressionSyntax syntax)
    {
        if (syntax.Value is string strValue && syntax.Token.InterpolationStarts.Length > 0)
        {
            return BindInterpolatedString(strValue, syntax.Token.InterpolationStarts, syntax);
        }
        return new BoundLiteralExpression(syntax.Value, syntax: syntax);
    }

    private static BoundExpression BindTypeDefault(TypeDefaultExpressionSyntax syntax)
    {
        object? value = syntax.TypeToken.Kind switch
        {
            SyntaxKind.NoobKeyword => null,
            SyntaxKind.TroofKeyword => false,
            SyntaxKind.NumbrKeyword => 0,
            SyntaxKind.NumbarKeyword => 0.0,
            SyntaxKind.YarnKeyword => string.Empty,
            _ => null,
        };

        return new BoundLiteralExpression(value, syntax: syntax);
    }

    private static BoundExpression BindInterpolatedString(
        string template,
        ImmutableArray<int> interpolationStarts,
        LiteralExpressionSyntax syntax)
    {
        var textParts = ImmutableArray.CreateBuilder<string>(interpolationStarts.Length + 1);
        var names = ImmutableArray.CreateBuilder<string>(interpolationStarts.Length);
        int pos = 0;

        foreach (int nextInterp in interpolationStarts)
        {
            int closingBrace = template.IndexOf('}', nextInterp + 2);
            if (closingBrace < 0)
            {
                break;
            }

            textParts.Add(template[pos..nextInterp]);
            names.Add(template[(nextInterp + 2)..closingBrace]);
            pos = closingBrace + 1;
        }

        textParts.Add(template[pos..]);

        return new BoundInterpolatedStringExpression(
            textParts.ToImmutable(),
            names.ToImmutable(),
            syntax);
    }

    private BoundExpression BindVariableExpression(VariableExpressionSyntax syntax)
    {
        string name = syntax.NameToken.Text;
        if (!_scope.TryLookupVariable(name, out var variable))
        {
            var location = TextLocation.FromSpan(_text, syntax.NameToken.Span);
            _diagnostics.ReportUndeclaredVariable(location, name);
            variable = new VariableSymbol(name);
        }
        return new BoundVariableExpression(variable, syntax: syntax);
    }

    private BoundUnaryExpression BindUnary(UnaryExpressionSyntax syntax)
    {
        var operand = BindExpression(syntax.Operand);
        return new BoundUnaryExpression(BoundUnaryOperatorKind.LogicalNot, operand, syntax: syntax);
    }

    private BoundBinaryExpression BindBinary(BinaryExpressionSyntax syntax)
    {
        string op = syntax.OperatorToken.Text;
        var left = BindExpression(syntax.Left);
        var right = BindExpression(syntax.Right);

        var kind = op switch
        {
            "SUM" => BoundBinaryOperatorKind.Addition,
            "DIFF" => BoundBinaryOperatorKind.Subtraction,
            "PRODUKT" => BoundBinaryOperatorKind.Multiplication,
            "QUOSHUNT" => BoundBinaryOperatorKind.Division,
            "MOD" => BoundBinaryOperatorKind.Modulo,
            "BIGGR" => BoundBinaryOperatorKind.Maximum,
            "SMALLR" => BoundBinaryOperatorKind.Minimum,
            "BOTH" => BoundBinaryOperatorKind.LogicalAnd,
            "EITHER" => BoundBinaryOperatorKind.LogicalOr,
            "WON" => BoundBinaryOperatorKind.LogicalXor,
            _ => throw new InvalidOperationException($"Unknown operator: {op}")
        };

        return new BoundBinaryExpression(kind, left, right, syntax: syntax);
    }

    private BoundSmooshExpression BindSmoosh(SmooshExpressionSyntax syntax)
    {
        var operands = syntax.Operands.Select(BindExpression).ToImmutableArray();
        return new BoundSmooshExpression(operands, syntax: syntax);
    }

    private BoundAllOfExpression BindAllOf(AllOfExpressionSyntax syntax)
    {
        var operands = syntax.Operands.Select(BindExpression).ToImmutableArray();
        return new BoundAllOfExpression(operands, syntax: syntax);
    }

    private BoundAnyOfExpression BindAnyOf(AnyOfExpressionSyntax syntax)
    {
        var operands = syntax.Operands.Select(BindExpression).ToImmutableArray();
        return new BoundAnyOfExpression(operands, syntax: syntax);
    }

    private BoundFunctionCallExpression BindFunctionCall(FunctionCallExpressionSyntax syntax)
    {
        string name = syntax.Identifier.DirectToken?.Text ?? "<dynamic>";

        if (syntax.Scope.DirectToken?.Text == "I" &&
            syntax.Scope.Slot is null &&
            syntax.Identifier.DirectToken is not null &&
            syntax.Identifier.Slot is null &&
            _scope.TryLookupFunction(name, out var knownFunction))
        {
            if (!_runtimeIdentifiers &&
                syntax.Arguments.Length != knownFunction.Parameters.Length)
            {
                var location = TextLocation.FromSpan(_text, syntax.NameToken.Span);
                _diagnostics.ReportWrongArgumentCount(location, name, knownFunction.Parameters.Length, syntax.Arguments.Length);
            }
            var knownArgs = syntax.Arguments.Select(BindExpression).ToImmutableArray();
            return new BoundFunctionCallExpression(
                knownFunction, knownArgs, syntax,
                BindIdentifier(syntax.Scope), BindIdentifier(syntax.Identifier),
                staticDispatch: !_runtimeIdentifiers);
        }

        if (!_runtimeIdentifiers)
        {
            var location = TextLocation.FromSpan(_text, syntax.NameToken.Span);
            _diagnostics.ReportUndefinedFunction(location, name);
        }

        var args = syntax.Arguments.Select(BindExpression).ToImmutableArray();
        var function = new FunctionSymbol(name, ImmutableArray<ParameterSymbol>.Empty);
        return new BoundFunctionCallExpression(
            function, args, syntax,
            BindIdentifier(syntax.Scope), BindIdentifier(syntax.Identifier));
    }
}
