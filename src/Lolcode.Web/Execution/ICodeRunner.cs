namespace Lolcode.Web.Execution;

internal interface ICodeRunner
{
    string LanguageName { get; }

    Task<CodeRunResult> RunAsync(CodeRunRequest request, CancellationToken cancellationToken = default);
}
