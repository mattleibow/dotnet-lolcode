using System.Linq;
using Lolcode.Runtime;

namespace InteropSamples;

[LolcodeLibrary("ManagedTextPackage")]
public sealed class TextTools
{
    public string Repeat(string value, int count) =>
        string.Join(" ", Enumerable.Repeat(value, count));

    public int CountCharacters(string value) => value.Length;
}
