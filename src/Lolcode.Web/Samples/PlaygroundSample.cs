using System.Collections.Immutable;

namespace Lolcode.Web.Samples;

internal sealed record PlaygroundSample(
    string Id,
    string Name,
    string Category,
    string Description,
    string Source,
    string StandardInput)
{
    public int LineCount => Math.Max(1, Source.Count(character => character == '\n') + 1);
}

internal static class PlaygroundSamples
{
    public static ImmutableArray<PlaygroundSample> All { get; } =
    [
        new(
            "hello",
            "Hello + input",
            "Basics",
            "Read one line with GIMMEH and use it in a greeting.",
            """
            HAI 1.2
              VISIBLE "OH HAI! WUT IZ UR NAME?"
              I HAS A name
              GIMMEH name
              VISIBLE "NICE 2 MEET U, " name "!"
            KTHXBYE
            """,
            "LOLCAT\n"),
        new(
            "fizzbuzz",
            "FizzBuzz",
            "Loops",
            "Loop from 1 to 100 and combine divisibility checks.",
            """
            HAI 1.2
              I HAS A i ITZ 1
              IM IN YR fizzbuzz UPPIN YR i TIL BOTH SAEM i AN 101
                I HAS A out ITZ ""
                BOTH SAEM MOD OF i AN 3 AN 0
                O RLY?
                  YA RLY, out R "Fizz"
                OIC
                BOTH SAEM MOD OF i AN 5 AN 0
                O RLY?
                  YA RLY, out R SMOOSH out AN "Buzz" MKAY
                OIC
                BOTH SAEM out AN ""
                O RLY?
                  YA RLY, VISIBLE i
                  NO WAI, VISIBLE out
                OIC
              IM OUTTA YR fizzbuzz
            KTHXBYE
            """,
            string.Empty),
        new(
            "fibonacci",
            "Fibonacci",
            "Functions",
            "Define a recursive function and print the tenth value.",
            """
            HAI 1.2
              HOW IZ I fib YR n
                BOTH SAEM n AN 0
                O RLY?
                  YA RLY, FOUND YR 0
                OIC
                BOTH SAEM n AN 1
                O RLY?
                  YA RLY, FOUND YR 1
                OIC
                FOUND YR SUM OF I IZ fib YR DIFF OF n AN 1 MKAY AN I IZ fib YR DIFF OF n AN 2 MKAY
              IF U SAY SO
              VISIBLE "FIB(10) = " I IZ fib YR 10 MKAY
            KTHXBYE
            """,
            string.Empty),
        new(
            "diagnostics",
            "Compile error",
            "Diagnostics",
            "Trigger an undeclared-variable compiler diagnostic.",
            """
            HAI 1.2
              VISIBLE missing
            KTHXBYE
            """,
            string.Empty),
        new(
            "runtime",
            "Runtime error",
            "Diagnostics",
            "Compile successfully, then trigger a NOOB arithmetic error.",
            """
            HAI 1.2
              VISIBLE "ABOUT 2 DO BAD MATH..."
              I HAS A value
              VISIBLE SUM OF value AN 1
            KTHXBYE
            """,
            string.Empty),
    ];

    public static ImmutableArray<PlaygroundSample> Filter(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return All;
        }

        var term = query.Trim();
        return
        [
            .. All.Where(sample =>
                sample.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || sample.Category.Contains(term, StringComparison.OrdinalIgnoreCase)
                || sample.Description.Contains(term, StringComparison.OrdinalIgnoreCase)),
        ];
    }
}
