using System.Collections.Immutable;
using Lolcode.CodeAnalysis.Text;

namespace Lolcode.CodeAnalysis.Syntax;

/// <summary>
/// Recursive descent parser for LOLCODE 1.2.
/// Produces a <see cref="CompilationUnitSyntax"/> from a list of tokens.
/// </summary>
internal sealed class Parser
{
    private readonly IReadOnlyList<SyntaxToken> _tokens;
    private readonly SourceText _text;
    private readonly DiagnosticBag _diagnostics = new();
    private int _position;

    /// <summary>
    /// Gets the diagnostics produced during parsing.
    /// </summary>
    public IEnumerable<Diagnostic> Diagnostics => _diagnostics;

    /// <summary>
    /// Creates a new parser for the given tokens and source text.
    /// </summary>
    public Parser(IReadOnlyList<SyntaxToken> tokens, SourceText text)
    {
        _tokens = tokens;
        _text = text;
    }

    private SyntaxToken Current => Peek(0);

    private SyntaxToken Peek(int offset)
    {
        int index = _position + offset;
        if (index >= _tokens.Count)
            return _tokens[^1]; // EOF
        return _tokens[index];
    }

    private SyntaxToken Advance()
    {
        var current = Current;
        _position++;
        return current;
    }

    private SyntaxToken Match(SyntaxKind kind)
    {
        if (Current.Kind == kind)
            return Advance();

        var location = GetCurrentLocation();
        _diagnostics.ReportUnexpectedToken(location, Current.Text, kind.ToString());
        return new SyntaxToken(kind, Current.Position, "", null);
    }

    private bool Check(SyntaxKind kind) => Current.Kind == kind;

    private bool CheckText(int offset, string text) => Peek(offset).Text == text;

    private SyntaxToken MatchIdentifier(string expectedText)
    {
        if (Current.Kind == SyntaxKind.IdentifierToken && Current.Text == expectedText)
            return Advance();

        var location = GetCurrentLocation();
        _diagnostics.ReportUnexpectedToken(location, Current.Text, expectedText);
        return new SyntaxToken(SyntaxKind.IdentifierToken, Current.Position, "", null);
    }

    private bool CheckSequence(params SyntaxKind[] kinds)
    {
        for (int i = 0; i < kinds.Length; i++)
        {
            if (Peek(i).Kind != kinds[i])
                return false;
        }
        return true;
    }

    private void SkipNewlines()
    {
        while (Current.Kind == SyntaxKind.EndOfLineToken)
            _position++;
    }

    private void ExpectEndOfLine()
    {
        if (Current.Kind == SyntaxKind.EndOfLineToken || Current.Kind == SyntaxKind.EndOfFileToken)
        {
            if (Current.Kind == SyntaxKind.EndOfLineToken)
                _position++;
        }
        else
        {
            var location = GetCurrentLocation();
            _diagnostics.ReportExpectedToken(location, "end of line");
        }
    }

    private TextLocation GetCurrentLocation()
    {
        return TextLocation.FromSpan(_text, Current.Span);
    }

    /// <summary>
    /// Parses the token stream into a <see cref="CompilationUnitSyntax"/>.
    /// </summary>
    public CompilationUnitSyntax Parse()
    {
        SkipNewlines();
        var program = ParseProgram();
        var eof = Match(SyntaxKind.EndOfFileToken);
        return new CompilationUnitSyntax(program, eof);
    }

    private ProgramStatementSyntax ParseProgram()
    {
        var hai = Match(SyntaxKind.HaiKeyword);

        SyntaxToken? version = null;
        if (Current.Kind is not (SyntaxKind.EndOfLineToken or SyntaxKind.EndOfFileToken))
        {
            version = Advance();
        }
        else
        {
            _diagnostics.ReportMissingVersion(GetCurrentLocation());
        }

        ExpectEndOfLine();
        SkipNewlines();

        var statements = ParseStatements(isTopLevel: true);

        var kthxbye = Match(SyntaxKind.KthxbyeKeyword);
        // Skip any trailing newlines after KTHXBYE
        while (Current.Kind == SyntaxKind.EndOfLineToken)
            _position++;

        return new ProgramStatementSyntax(hai, version, statements, kthxbye);
    }

    private ImmutableArray<StatementSyntax> ParseStatements(
        bool isTopLevel = false,
        bool inYaRly = false,
        bool inNoWai = false,
        bool inMebbe = false,
        bool inOmg = false,
        bool inOmgwtf = false,
        bool inLoop = false,
        bool inFunction = false,
        bool inObject = false)
    {
        var statements = ImmutableArray.CreateBuilder<StatementSyntax>();

        while (true)
        {
            SkipNewlines();

            if (Current.Kind == SyntaxKind.EndOfFileToken)
                break;

            // Termination conditions based on context
            if (isTopLevel && Current.Kind == SyntaxKind.KthxbyeKeyword)
                break;

            if ((inYaRly || inMebbe) &&
                (Current.Kind == SyntaxKind.MebbeKeyword ||
                 (Current.Kind == SyntaxKind.NoKeyword && Peek(1).Kind == SyntaxKind.WaiKeyword) ||
                 Current.Kind == SyntaxKind.OicKeyword))
                break;

            if (inNoWai && Current.Kind == SyntaxKind.OicKeyword)
                break;

            if (inOmg &&
                (Current.Kind == SyntaxKind.OmgKeyword ||
                 Current.Kind == SyntaxKind.OmgwtfKeyword ||
                 Current.Kind == SyntaxKind.OicKeyword))
                break;

            if (inOmgwtf && Current.Kind == SyntaxKind.OicKeyword)
                break;

            if (inLoop && CheckSequence(SyntaxKind.ImKeyword, SyntaxKind.OuttaKeyword))
                break;

            if (inFunction && CheckSequence(SyntaxKind.IfKeyword, SyntaxKind.UKeyword))
                break;

            if (inObject && Current.Kind == SyntaxKind.KthxKeyword)
                break;

            var statement = ParseStatement();
            if (statement != null)
                statements.Add(statement);
        }

        return statements.ToImmutable();
    }

    private StatementSyntax? ParseStatement()
    {
        if (CheckSequence(SyntaxKind.OKeyword, SyntaxKind.HaiKeyword, SyntaxKind.ImKeyword))
            return FinishStatement(ParseObjectDefinition());

        StatementSyntax? result = Current.Kind switch
        {
            SyntaxKind.VisibleKeyword => ParseVisible(),
            SyntaxKind.GimmehKeyword => ParseGimmeh(),

            // O RLY?
            SyntaxKind.OKeyword when Peek(1).Kind == SyntaxKind.RlyKeyword => ParseIf(),

            // WTF?
            SyntaxKind.WtfKeyword => ParseSwitch(),

            // IM IN YR
            SyntaxKind.ImKeyword when Peek(1).Kind == SyntaxKind.InKeyword => ParseLoop(),

            SyntaxKind.GtfoKeyword => ParseGtfo(),

            // HOW IZ I
            SyntaxKind.HowKeyword when Peek(1).Kind == SyntaxKind.IzKeyword => ParseFunctionDeclaration(),

            // FOUND YR
            SyntaxKind.FoundKeyword when Peek(1).Kind == SyntaxKind.YrKeyword => ParseReturn(),

            _ => ParseIdentifierLedStatement(),
        };

        return FinishStatement(result);
    }

    private StatementSyntax? FinishStatement(StatementSyntax? result)
    {
        // Each statement should end with a newline (or EOF)
        if (result != null && Current.Kind != SyntaxKind.EndOfFileToken)
        {
            if (Current.Kind == SyntaxKind.EndOfLineToken)
                _position++;
        }

        return result;
    }

    private StatementSyntax ParseIdentifierLedStatement()
    {
        if (!IsIdentifierStart(Current.Kind))
            return ParseExpressionStatement();

        int start = _position;
        var identifier = ParseIdentifier();

        if (Current.Kind == SyntaxKind.HasKeyword)
            return ParseScopedDeclaration(identifier);

        if (Current.Kind == SyntaxKind.RKeyword)
        {
            Advance();
            var expression = ParseExpression();
            if (identifier.DirectToken is { } direct && identifier.Slot is null)
                return new AssignmentStatementSyntax(direct, expression);
            return new IdentifierAssignmentSyntax(identifier, expression);
        }

        if (Current.Kind == SyntaxKind.IsKeyword)
        {
            _position = start;
            return ParseCastStatement();
        }

        _position = start;
        return ParseExpressionStatement();
    }

    private StatementSyntax ParseScopedDeclaration(IdentifierSyntax scope)
    {
        Match(SyntaxKind.HasKeyword);
        if (Current.Kind is SyntaxKind.AKeyword or SyntaxKind.AnKeyword)
            Advance();
        else if (Current.Kind != SyntaxKind.SrsKeyword)
            _diagnostics.ReportExpectedToken(GetCurrentLocation(), "A or AN");

        var name = ParseIdentifier();
        var initializer = ParseOptionalInitializer();
        if (scope.DirectToken?.Text == "I" && scope.Slot is null &&
            name.DirectToken is { } direct && name.Slot is null)
            return new VariableDeclarationSyntax(direct, initializer);
        return new ScopedDeclarationSyntax(scope, name, initializer);
    }

    private ExpressionSyntax? ParseOptionalInitializer()
    {
        if (Current.Kind != SyntaxKind.ItzKeyword)
            return null;

        Advance();
        if (Current.Kind == SyntaxKind.LiekKeyword)
        {
            var start = Advance();
            if (Current.Kind == SyntaxKind.AKeyword)
                Advance();
            var parent = ParseIdentifier();
            return ParseObjectCreationTail(start, parent);
        }

        if (Current.Kind == SyntaxKind.AKeyword && Peek(1).Kind == SyntaxKind.BukkitKeyword)
        {
            var start = Advance();
            Advance();
            return ParseObjectCreationTail(start, parent: null);
        }

        if (Current.Kind == SyntaxKind.AKeyword && IsIdentifierStart(Peek(1).Kind))
        {
            var start = Advance();
            var parent = ParseIdentifier();
            return ParseObjectCreationTail(start, parent);
        }

        if (Current.Kind == SyntaxKind.AKeyword && SyntaxFacts.IsTypeKeyword(Peek(1).Kind))
        {
            var a = Advance();
            return new TypeDefaultExpressionSyntax(a, ParseTypeKeyword());
        }

        return ParseExpression();
    }

    private ObjectCreationExpressionSyntax ParseObjectCreationTail(SyntaxToken start, IdentifierSyntax? parent)
    {
        var mixins = ImmutableArray.CreateBuilder<IdentifierSyntax>();
        if (Current.Kind == SyntaxKind.SmooshKeyword)
        {
            Advance();
            mixins.Add(ParseIdentifier());
            while (Current.Kind == SyntaxKind.AnKeyword)
            {
                Advance();
                mixins.Add(ParseIdentifier());
            }
        }
        return new ObjectCreationExpressionSyntax(start, parent, mixins.ToImmutable());
    }

    private AssignmentStatementSyntax ParseAssignment()
    {
        var name = Advance(); // identifier or IT
        Match(SyntaxKind.RKeyword); // R
        var expr = ParseExpression();
        return new AssignmentStatementSyntax(name, expr);
    }

    private CastStatementSyntax ParseCastStatement()
    {
        var target = ParseIdentifier();
        Match(SyntaxKind.IsKeyword);  // IS
        Match(SyntaxKind.NowKeyword); // NOW
        Match(SyntaxKind.AKeyword);   // A
        var type = ParseTypeKeyword();
        return new CastStatementSyntax(target, type);
    }

    private SyntaxToken ParseTypeKeyword()
    {
        if (Current.Kind is SyntaxKind.TroofKeyword or SyntaxKind.NumbrKeyword
            or SyntaxKind.NumbarKeyword or SyntaxKind.YarnKeyword or SyntaxKind.NoobKeyword)
        {
            return Advance();
        }

        var location = GetCurrentLocation();
        _diagnostics.ReportExpectedToken(location, "type keyword (TROOF, NUMBR, NUMBAR, YARN, or NOOB)");
        return new SyntaxToken(SyntaxKind.BadToken, Current.Position, Current.Text);
    }

    private VisibleStatementSyntax ParseVisible()
    {
        var keyword = Advance(); // VISIBLE
        var args = ImmutableArray.CreateBuilder<ExpressionSyntax>();
        bool suppressNewline = false;

        // Parse arguments until end of line or !
        while (Current.Kind != SyntaxKind.EndOfLineToken &&
               Current.Kind != SyntaxKind.EndOfFileToken)
        {
            if (Current.Kind == SyntaxKind.ExclamationToken)
            {
                suppressNewline = true;
                Advance();
                break;
            }

            // Skip optional AN between arguments
            if (args.Count > 0 && Current.Kind == SyntaxKind.AnKeyword)
                Advance();

            // Check again for end/exclamation after skipping AN
            if (Current.Kind == SyntaxKind.EndOfLineToken ||
                Current.Kind == SyntaxKind.EndOfFileToken)
                break;

            if (Current.Kind == SyntaxKind.ExclamationToken)
            {
                suppressNewline = true;
                Advance();
                break;
            }

            args.Add(ParseExpression());
        }

        if (args.Count == 0)
            _diagnostics.ReportVisibleRequiresArgument(TextLocation.FromSpan(_text, keyword.Span));

        return new VisibleStatementSyntax(keyword, args.ToImmutable(), suppressNewline);
    }

    private GimmehStatementSyntax ParseGimmeh()
    {
        var keyword = Advance(); // GIMMEH
        var target = ParseIdentifier();
        return new GimmehStatementSyntax(keyword, target);
    }

    private IfStatementSyntax ParseIf()
    {
        var o = Advance(); // O
        Match(SyntaxKind.RlyKeyword); // RLY?
        ExpectEndOfLine();
        SkipNewlines();

        // YA RLY (note: RLY here has no ?, so it's an identifier)
        Match(SyntaxKind.YaKeyword);
        MatchIdentifier("RLY"); // RLY without ?
        ExpectEndOfLine();

        var yaRlyStatements = ParseStatements(inYaRly: true);
        var yaRlyBody = new BlockStatementSyntax(yaRlyStatements);

        // MEBBE clauses
        var mebbeClauses = ImmutableArray.CreateBuilder<MebbeClauseSyntax>();
        while (Current.Kind == SyntaxKind.MebbeKeyword)
        {
            var mebbeKeyword = Advance();
            var condition = ParseExpression();
            ExpectEndOfLine();
            var mebbeStatements = ParseStatements(inMebbe: true);
            var mebbeBody = new BlockStatementSyntax(mebbeStatements);
            mebbeClauses.Add(new MebbeClauseSyntax(mebbeKeyword, condition, mebbeBody));
        }

        // NO WAI (optional)
        BlockStatementSyntax? noWaiBody = null;
        if (Current.Kind == SyntaxKind.NoKeyword && Peek(1).Kind == SyntaxKind.WaiKeyword)
        {
            Advance(); // NO
            Advance(); // WAI
            ExpectEndOfLine();
            var noWaiStatements = ParseStatements(inNoWai: true);
            noWaiBody = new BlockStatementSyntax(noWaiStatements);
        }

        var oic = Match(SyntaxKind.OicKeyword);
        return new IfStatementSyntax(o, yaRlyBody, mebbeClauses.ToImmutable(), noWaiBody, oic);
    }

    private SwitchStatementSyntax ParseSwitch()
    {
        var wtf = Advance(); // WTF?
        ExpectEndOfLine();
        SkipNewlines();

        var omgClauses = ImmutableArray.CreateBuilder<OmgClauseSyntax>();

        while (Current.Kind == SyntaxKind.OmgKeyword)
        {
            var omgKeyword = Advance();
            var value = ParseExpression();
            ExpectEndOfLine();
            var omgStatements = ParseStatements(inOmg: true);
            var omgBody = new BlockStatementSyntax(omgStatements);
            omgClauses.Add(new OmgClauseSyntax(omgKeyword, value, omgBody));
        }

        // OMGWTF (optional default)
        BlockStatementSyntax? omgwtfBody = null;
        if (Current.Kind == SyntaxKind.OmgwtfKeyword)
        {
            Advance(); // OMGWTF
            ExpectEndOfLine();
            var omgwtfStatements = ParseStatements(inOmgwtf: true);
            omgwtfBody = new BlockStatementSyntax(omgwtfStatements);
        }

        var oic = Match(SyntaxKind.OicKeyword);
        return new SwitchStatementSyntax(wtf, omgClauses.ToImmutable(), omgwtfBody, oic);
    }

    private LoopStatementSyntax ParseLoop()
    {
        var im = Advance(); // IM
        Match(SyntaxKind.InKeyword); // IN
        Match(SyntaxKind.YrKeyword); // YR
        var label = Match(SyntaxKind.IdentifierToken);

        SyntaxToken? builtInOperation = null;
        FunctionCallExpressionSyntax? operationCall = null;
        SyntaxToken? variable = null;
        SyntaxToken? conditionKeyword = null;
        ExpressionSyntax? condition = null;

        // Optional: UPPIN/NERFIN YR <var>, or <scope> IZ <name> YR <var> MKAY.
        if (IsIdentifierStart(Current.Kind))
        {
            int start = _position;
            var scope = ParseIdentifier(terminateBeforeIz: true);
            if (Current.Kind == SyntaxKind.IzKeyword)
            {
                Advance();
                var identifier = ParseIdentifier();
                var name = identifier.DirectToken
                    ?? new SyntaxToken(SyntaxKind.IdentifierToken, identifier.Span.Start, "");
                Match(SyntaxKind.YrKeyword);
                variable = Match(SyntaxKind.IdentifierToken);
                Match(SyntaxKind.MkayKeyword);
                var argumentIdentifier = new IdentifierSyntax(variable, null, null);
                operationCall = new FunctionCallExpressionSyntax(
                    scope,
                    identifier,
                    name,
                    [new IdentifierExpressionSyntax(argumentIdentifier)]);
            }
            else
            {
                _position = start;
            }
        }

        if (operationCall is null &&
            Current.Kind is SyntaxKind.UppinKeyword or SyntaxKind.NerfinKeyword)
        {
            builtInOperation = Advance();
            Match(SyntaxKind.YrKeyword); // YR
            variable = Match(SyntaxKind.IdentifierToken);
        }

        if (variable != null &&
            (Current.Kind == SyntaxKind.TilKeyword || Current.Kind == SyntaxKind.WileKeyword))
        {
            conditionKeyword = Advance();
            condition = ParseExpression();
        }

        ExpectEndOfLine();
        var bodyStatements = ParseStatements(inLoop: true);
        var body = new BlockStatementSyntax(bodyStatements);

        Match(SyntaxKind.ImKeyword);    // IM
        Match(SyntaxKind.OuttaKeyword); // OUTTA
        Match(SyntaxKind.YrKeyword);    // YR
        var endLabel = Match(SyntaxKind.IdentifierToken);

        return new LoopStatementSyntax(
            im, label, builtInOperation, operationCall, variable,
            conditionKeyword, condition, body, im, endLabel);
    }

    private GtfoStatementSyntax ParseGtfo()
    {
        var keyword = Advance();
        return new GtfoStatementSyntax(keyword);
    }

    private FunctionDeclarationSyntax ParseFunctionDeclaration()
    {
        Match(SyntaxKind.HowKeyword); // HOW
        Match(SyntaxKind.IzKeyword);  // IZ
        var scope = ParseIdentifier(terminateBeforeIz: true);
        var identifier = ParseIdentifier();
        var name = identifier.DirectToken
            ?? new SyntaxToken(SyntaxKind.IdentifierToken, identifier.Span.Start, "");
        var parameters = ImmutableArray.CreateBuilder<IdentifierSyntax>();

        // Optional parameters: YR <param> [AN YR <param>]*
        if (Current.Kind == SyntaxKind.YrKeyword)
        {
            Advance(); // YR
            parameters.Add(ParseIdentifier());

            while (Current.Kind == SyntaxKind.AnKeyword && Peek(1).Kind == SyntaxKind.YrKeyword)
            {
                Advance(); // AN
                Advance(); // YR
                parameters.Add(ParseIdentifier());
            }
        }

        ExpectEndOfLine();
        var bodyStatements = ParseStatements(inFunction: true);
        var body = new BlockStatementSyntax(bodyStatements);

        Match(SyntaxKind.IfKeyword);  // IF
        Match(SyntaxKind.UKeyword);   // U
        Match(SyntaxKind.SayKeyword); // SAY
        var so = Match(SyntaxKind.SoKeyword);  // SO

        return new FunctionDeclarationSyntax(scope, identifier, name, parameters.ToImmutable(), body, so);
    }

    private ObjectDefinitionSyntax ParseObjectDefinition()
    {
        Match(SyntaxKind.OKeyword);
        Match(SyntaxKind.HaiKeyword);
        Match(SyntaxKind.ImKeyword);
        var name = ParseIdentifier();
        IdentifierSyntax? parent = null;
        var mixins = ImmutableArray.CreateBuilder<IdentifierSyntax>();
        if (Current.Kind == SyntaxKind.ImKeyword && Peek(1).Kind == SyntaxKind.LiekKeyword)
        {
            Advance();
            Advance();
            parent = ParseIdentifier();
            if (Current.Kind == SyntaxKind.SmooshKeyword)
            {
                Advance();
                mixins.Add(ParseIdentifier());
                while (Current.Kind == SyntaxKind.AnKeyword)
                {
                    Advance();
                    mixins.Add(ParseIdentifier());
                }
            }
        }

        ExpectEndOfLine();
        var body = new BlockStatementSyntax(ParseStatements(inObject: true));
        var end = Match(SyntaxKind.KthxKeyword);
        return new ObjectDefinitionSyntax(name, parent, mixins.ToImmutable(), body, end);
    }

    private ReturnStatementSyntax ParseReturn()
    {
        var found = Advance(); // FOUND
        Match(SyntaxKind.YrKeyword); // YR
        var expr = ParseExpression();
        return new ReturnStatementSyntax(found, expr);
    }

    private StatementSyntax ParseExpressionOrFunctionCallStatement()
    {
        var expr = ParseExpression();
        return new ExpressionStatementSyntax(expr);
    }

    private StatementSyntax ParseExpressionStatement()
    {
        var expr = ParseExpression();
        return new ExpressionStatementSyntax(expr);
    }

    /// <summary>
    /// Parses an expression.
    /// </summary>
    public ExpressionSyntax ParseExpression() => ParseExpressionCore(terminateBeforeIz: false);

    private ExpressionSyntax ParseExpressionCore(bool terminateBeforeIz)
    {
        return Current.Kind switch
        {
            // SUM OF, DIFF OF, PRODUKT OF, QUOSHUNT OF, MOD OF, BIGGR OF, SMALLR OF
            SyntaxKind.SumKeyword or SyntaxKind.DiffKeyword or SyntaxKind.ProduktKeyword or
            SyntaxKind.QuoshuntKeyword or SyntaxKind.ModKeyword or SyntaxKind.BiggrKeyword or
            SyntaxKind.SmallrKeyword => ParseBinaryExpression(),

            // BOTH OF (boolean AND)
            SyntaxKind.BothKeyword when Peek(1).Kind == SyntaxKind.OfKeyword => ParseBinaryExpression(),

            // BOTH SAEM (equality)
            SyntaxKind.BothKeyword when Peek(1).Kind == SyntaxKind.SaemKeyword => ParseComparison(),

            // EITHER OF (boolean OR)
            SyntaxKind.EitherKeyword => ParseBinaryExpression(),

            // WON OF (boolean XOR)
            SyntaxKind.WonKeyword => ParseBinaryExpression(),

            // NOT (unary)
            SyntaxKind.NotKeyword => ParseUnary(),

            // ALL OF (variadic AND)
            SyntaxKind.AllKeyword => ParseAllOf(),

            // ANY OF (variadic OR)
            SyntaxKind.AnyKeyword => ParseAnyOf(),

            // DIFFRINT (inequality)
            SyntaxKind.DiffrintKeyword => ParseDiffrint(),

            // SMOOSH (concatenation)
            SyntaxKind.SmooshKeyword => ParseSmoosh(),

            // MAEK (cast expression)
            SyntaxKind.MaekKeyword => ParseCastExpression(),

            // IT (implicit variable)
            SyntaxKind.ItKeyword => ParseIt(),

            SyntaxKind.IdentifierToken or SyntaxKind.IKeyword or SyntaxKind.MeKeyword or SyntaxKind.SrsKeyword
                => ParseIdentifierOrCallExpression(terminateBeforeIz),

            // Literals and variables
            _ => ParsePrimary(),
        };
    }

    private ExpressionSyntax ParseBinaryExpression()
    {
        var op = Advance(); // SUM, DIFF, PRODUKT, QUOSHUNT, MOD, BIGGR, SMALLR, BOTH, EITHER, WON
        Match(SyntaxKind.OfKeyword); // OF
        var left = ParseExpression();

        // Optional AN
        if (Current.Kind == SyntaxKind.AnKeyword)
            Advance();

        var right = ParseExpression();
        return new BinaryExpressionSyntax(op, left, right);
    }

    private ExpressionSyntax ParseComparison()
    {
        var both = Advance(); // BOTH
        Match(SyntaxKind.SaemKeyword); // SAEM
        var left = ParseExpression();

        // Optional AN
        if (Current.Kind == SyntaxKind.AnKeyword)
            Advance();

        var right = ParseExpression();
        return new ComparisonExpressionSyntax(both, left, right);
    }

    private ExpressionSyntax ParseDiffrint()
    {
        var keyword = Advance(); // DIFFRINT
        var left = ParseExpression();

        if (Current.Kind == SyntaxKind.AnKeyword)
            Advance();

        var right = ParseExpression();
        return new DiffrintExpressionSyntax(keyword, left, right);
    }

    private ExpressionSyntax ParseUnary()
    {
        var op = Advance(); // NOT
        var operand = ParseExpression();
        return new UnaryExpressionSyntax(op, operand);
    }

    private ExpressionSyntax ParseAllOf()
    {
        var keyword = Advance(); // ALL
        Match(SyntaxKind.OfKeyword); // OF
        var operands = ParseVariadicArgs();
        return new AllOfExpressionSyntax(keyword, operands);
    }

    private ExpressionSyntax ParseAnyOf()
    {
        var keyword = Advance(); // ANY
        Match(SyntaxKind.OfKeyword); // OF
        var operands = ParseVariadicArgs();
        return new AnyOfExpressionSyntax(keyword, operands);
    }

    private ExpressionSyntax ParseSmoosh()
    {
        var keyword = Advance(); // SMOOSH
        var operands = ParseVariadicArgs();
        return new SmooshExpressionSyntax(keyword, operands);
    }

    private ImmutableArray<ExpressionSyntax> ParseVariadicArgs()
    {
        var args = ImmutableArray.CreateBuilder<ExpressionSyntax>();

        while (Current.Kind != SyntaxKind.MkayKeyword &&
               Current.Kind != SyntaxKind.EndOfLineToken &&
               Current.Kind != SyntaxKind.EndOfFileToken)
        {
            if (args.Count > 0 && Current.Kind == SyntaxKind.AnKeyword)
                Advance();

            if (Current.Kind == SyntaxKind.MkayKeyword ||
                Current.Kind == SyntaxKind.EndOfLineToken ||
                Current.Kind == SyntaxKind.EndOfFileToken)
                break;

            args.Add(ParseExpression());
        }

        // MKAY is optional at end of line
        if (Current.Kind == SyntaxKind.MkayKeyword)
            Advance();

        return args.ToImmutable();
    }

    private ExpressionSyntax ParseCastExpression()
    {
        var keyword = Advance(); // MAEK
        var operand = ParseExpression();

        // Optional A
        if (Current.Kind == SyntaxKind.AKeyword)
            Advance();

        var type = ParseTypeKeyword();
        return new CastExpressionSyntax(keyword, operand, type);
    }

    private ExpressionSyntax ParseFunctionCall()
    {
        var scope = ParseIdentifier(terminateBeforeIz: true);
        Match(SyntaxKind.IzKeyword);
        var identifier = ParseIdentifier();
        var name = identifier.DirectToken
            ?? new SyntaxToken(SyntaxKind.IdentifierToken, identifier.Span.Start, "");
        var args = ImmutableArray.CreateBuilder<ExpressionSyntax>();

        // YR <expr> [AN YR <expr>]*
        if (Current.Kind == SyntaxKind.YrKeyword)
        {
            Advance(); // YR
            args.Add(ParseExpression());

            while (Current.Kind == SyntaxKind.AnKeyword && Peek(1).Kind == SyntaxKind.YrKeyword)
            {
                Advance(); // AN
                Advance(); // YR
                args.Add(ParseExpression());
            }
        }

        // MKAY optional at end of line
        if (Current.Kind == SyntaxKind.MkayKeyword)
            Advance();

        return new FunctionCallExpressionSyntax(scope, identifier, name, args.ToImmutable());
    }

    private ExpressionSyntax ParseIdentifierOrCallExpression(bool terminateBeforeIz = false)
    {
        int start = _position;
        ParseIdentifier(terminateBeforeIz);
        bool isCall = Current.Kind == SyntaxKind.IzKeyword &&
            (!terminateBeforeIz ||
             CurrentIdentifierStartsCanonicalCallScope(start) ||
             HasCallTerminatorBeforeEnclosingIz());

        if (!terminateBeforeIz &&
            !isCall &&
            HasContextualSrsScopeCandidate(start))
        {
            _position = start;
            ParseIdentifier(terminateBeforeIz: true);
            isCall = Current.Kind == SyntaxKind.IzKeyword;
        }

        _position = start;
        if (isCall)
            return ParseFunctionCall();
        return new IdentifierExpressionSyntax(ParseIdentifier(terminateBeforeIz));
    }

    private bool CurrentIdentifierStartsCanonicalCallScope(int start) =>
        _tokens[start].Kind is SyntaxKind.IKeyword or SyntaxKind.MeKeyword;

    private bool HasContextualSrsScopeCandidate(int start)
    {
        for (int index = start;
             index + 1 < _tokens.Count &&
             _tokens[index].Kind is not SyntaxKind.EndOfLineToken and not SyntaxKind.EndOfFileToken;
             index++)
        {
            if (_tokens[index].Kind == SyntaxKind.SrsKeyword &&
                _tokens[index + 1].Kind is not SyntaxKind.IKeyword and not SyntaxKind.MeKeyword)
                return true;
        }

        return false;
    }

    private IdentifierSyntax ParseIdentifier(bool terminateBeforeIz = false)
    {
        SyntaxToken? direct = null;
        ExpressionSyntax? expression = null;
        if (Current.Kind == SyntaxKind.SrsKeyword)
        {
            Advance();
            expression = ParseExpressionCore(terminateBeforeIz);
        }
        else if (IsIdentifierStart(Current.Kind) && Current.Kind != SyntaxKind.SrsKeyword)
        {
            direct = Advance();
        }
        else
        {
            _diagnostics.ReportExpectedToken(GetCurrentLocation(), "identifier");
            direct = new SyntaxToken(SyntaxKind.IdentifierToken, Current.Position, "");
        }

        IdentifierSyntax? slot = null;
        if (Current.Kind == SyntaxKind.ApostrophezToken)
        {
            Advance();
            slot = ParseIdentifier(terminateBeforeIz);
        }
        return new IdentifierSyntax(direct, expression, slot);
    }

    private bool HasCallTerminatorBeforeEnclosingIz()
    {
        for (int index = _position + 1;
             index + 1 < _tokens.Count &&
             _tokens[index].Kind is not SyntaxKind.EndOfLineToken and not SyntaxKind.EndOfFileToken;
             index++)
        {
            if (_tokens[index].Kind == SyntaxKind.MkayKeyword &&
                _tokens[index + 1].Kind == SyntaxKind.IzKeyword)
                return true;
        }

        return false;
    }

    private static bool IsIdentifierStart(SyntaxKind kind) => kind is
        SyntaxKind.IdentifierToken or SyntaxKind.IKeyword or SyntaxKind.ItKeyword or
        SyntaxKind.MeKeyword or SyntaxKind.SrsKeyword;

    private ExpressionSyntax ParseIt()
    {
        var token = Advance(); // IT
        return new ItExpressionSyntax(token);
    }

    private ExpressionSyntax ParsePrimary()
    {
        switch (Current.Kind)
        {
            case SyntaxKind.NumbrLiteralToken:
            {
                var token = Advance();
                return new LiteralExpressionSyntax(token, token.Value);
            }
            case SyntaxKind.NumbarLiteralToken:
            {
                var token = Advance();
                return new LiteralExpressionSyntax(token, token.Value);
            }
            case SyntaxKind.YarnLiteralToken:
            {
                var token = Advance();
                return new LiteralExpressionSyntax(token, token.Value);
            }
            case SyntaxKind.WinKeyword:
            {
                var token = Advance();
                return new LiteralExpressionSyntax(token, true);
            }
            case SyntaxKind.FailKeyword:
            {
                var token = Advance();
                return new LiteralExpressionSyntax(token, false);
            }
            case SyntaxKind.NoobKeyword:
            {
                var token = Advance();
                return new LiteralExpressionSyntax(token, null);
            }
            case SyntaxKind.IdentifierToken:
            {
                return ParseIdentifierOrCallExpression();
            }
            default:
            {
                var location = GetCurrentLocation();
                _diagnostics.ReportExpectedToken(location, "expression");
                var token = Advance();
                return new LiteralExpressionSyntax(token, null);
            }
        }
    }
}
