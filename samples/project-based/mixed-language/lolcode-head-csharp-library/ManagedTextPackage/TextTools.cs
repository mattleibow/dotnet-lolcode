using System.Linq;

namespace InteropSamples;

public static class TextTools
{
    public static string Repeat(string value, int count) =>
        string.Join(" ", Enumerable.Repeat(value, count));

    public static int CountCharacters(string value) => value.Length;
}
