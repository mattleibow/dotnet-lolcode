namespace Lolcode.CodeAnalysis.Errors;

/// <summary>
/// Enumeration of all compiler error codes.
/// Lexer: LOL0xxx, Parser: LOL1xxx, Binder: LOL2xxx, Deployment: LOL3xxx, Internal: LOL9xxx.
/// </summary>
public enum ErrorCode
{
    // --- Lexer (LOL0xxx) ---

    /// <summary>Unexpected character in source.</summary>
    LOL0001,
    /// <summary>Unterminated string literal.</summary>
    LOL0002,
    /// <summary>Invalid number literal.</summary>
    LOL0003,
    /// <summary>Invalid escape sequence in string.</summary>
    LOL0004,
    /// <summary>Invalid line continuation.</summary>
    LOL0005,
    /// <summary>Multi-line comment must start on its own logical line.</summary>
    LOL0006,
    /// <summary>Multi-line comment must end on its own logical line.</summary>
    LOL0007,

    // --- Parser (LOL1xxx) ---

    /// <summary>Unexpected token.</summary>
    LOL1001,
    /// <summary>Expected token.</summary>
    LOL1002,
    /// <summary>Missing HAI.</summary>
    LOL1003,
    /// <summary>Missing KTHXBYE.</summary>
    LOL1004,
    /// <summary>Missing language version after HAI.</summary>
    LOL1005,
    /// <summary>VISIBLE requires at least one argument.</summary>
    LOL1006,

    // --- Binder (LOL2xxx) ---

    /// <summary>Undeclared variable.</summary>
    LOL2001,
    /// <summary>Variable already declared.</summary>
    LOL2002,
    /// <summary>Undefined function.</summary>
    LOL2003,
    /// <summary>Wrong argument count.</summary>
    LOL2004,
    /// <summary>Invalid cast.</summary>
    LOL2005,
    /// <summary>Invalid GTFO context.</summary>
    LOL2006,
    /// <summary>Invalid FOUND YR context.</summary>
    LOL2007,
    /// <summary>Duplicate OMG case value.</summary>
    LOL2008,
    /// <summary>OMG requires literal.</summary>
    LOL2009,
    /// <summary>Function already declared.</summary>
    LOL2010,
    /// <summary>Source files in one compilation use different language versions.</summary>
    LOL2011,

    // --- Deployment/static linking (LOL3xxx) ---

    /// <summary>Static import cannot be resolved from declared references.</summary>
    LOL3001,
    /// <summary>Static import contract is missing or incompatible.</summary>
    LOL3003,
    /// <summary>Dynamic resolution is incompatible with trimming or NativeAOT.</summary>
    LOL3005,
    /// <summary>Dynamic import has no declared static candidate set.</summary>
    LOL3007,
    // --- Internal (LOL9xxx) ---

    /// <summary>Internal compiler error.</summary>
    LOL9001,
    /// <summary>An obsolete output artifact could not be removed.</summary>
    LOL9002,
    /// <summary>A referenced provider assembly has invalid metadata.</summary>
    LOL9003,
}
