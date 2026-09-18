const { execFile } = require("node:child_process");
const { promisify } = require("node:util");
const vscode = require("vscode");

const execFileAsync = promisify(execFile);

/**
 * Registers commands that require access to the local .NET CLI.
 *
 * @param {vscode.ExtensionContext} context The extension context.
 */
function activate(context) {
  const output = vscode.window.createOutputChannel("LOLCODE");
  context.subscriptions.push(output);

  context.subscriptions.push(vscode.commands.registerCommand(
    "lolcode.installTemplates",
    async () => {
      output.clear();
      output.show(true);
      output.appendLine("Installing LOLCODE .NET templates...");

      try {
        const { stdout, stderr } = await execFileAsync(
          "dotnet",
          ["new", "install", "Lolcode.NET.Templates"],
          { windowsHide: true });
        output.append(stdout);
        output.append(stderr);
        vscode.window.showInformationMessage("LOLCODE .NET templates installed.");
      } catch (error) {
        const detail = error instanceof Error ? error.message : String(error);
        output.appendLine(detail);
        vscode.window.showErrorMessage(
          "Could not install LOLCODE .NET templates. See the LOLCODE output channel for details.");
      }
    }));
}

function deactivate() {}

module.exports = { activate, deactivate };
