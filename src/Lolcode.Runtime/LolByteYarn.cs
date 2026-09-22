using System.Reflection;
using System.Text;

namespace Lolcode.Runtime;

internal sealed record LolByteYarn(byte[] Bytes);

internal static class YarnByteSink
{
    internal static void Write(TextWriter writer, byte[] bytes, bool suppressNewline)
    {
        if (TryGetStream(writer, out Stream stream))
        {
            writer.Flush();
            stream.Write(bytes);
            if (!suppressNewline)
                stream.Write(Encoding.UTF8.GetBytes(writer.NewLine));
            stream.Flush();
            return;
        }

        string text = Encoding.UTF8.GetString(bytes);
        if (suppressNewline)
            writer.Write(text);
        else
            writer.WriteLine(text);
    }

    private static bool TryGetStream(TextWriter writer, out Stream stream)
    {
        var visited = new HashSet<TextWriter>(ReferenceEqualityComparer.Instance);
        TextWriter? current = writer;
        while (current is not null && visited.Add(current))
        {
            if (current is StreamWriter streamWriter)
            {
                stream = streamWriter.BaseStream;
                return true;
            }

            current = current.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(static field => typeof(TextWriter).IsAssignableFrom(field.FieldType))
                .Select(field => field.GetValue(current) as TextWriter)
                .FirstOrDefault(static nested => nested is not null);
        }

        stream = null!;
        return false;
    }
}
