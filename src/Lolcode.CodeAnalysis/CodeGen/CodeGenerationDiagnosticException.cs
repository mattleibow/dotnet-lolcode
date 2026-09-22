using Lolcode.CodeAnalysis.Text;

namespace Lolcode.CodeAnalysis.CodeGen;

internal sealed class CodeGenerationDiagnosticException(
    DiagnosticDescriptor descriptor,
    TextLocation location,
    params object[] messageArguments)
    : Exception(string.Format(descriptor.MessageFormat, messageArguments))
{
    public DiagnosticDescriptor Descriptor { get; } = descriptor;

    public TextLocation Location { get; } = location;

    public object[] MessageArguments { get; } = messageArguments;
}
