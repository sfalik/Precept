import * as fs from "fs";
import * as path from "path";
import * as vscode from "vscode";
import {
  LanguageClient,
  LanguageClientOptions,
  RevealOutputChannelOn,
  ServerOptions
} from "vscode-languageclient/node";

let client: LanguageClient | undefined;

export async function activate(context: vscode.ExtensionContext): Promise<void> {
  const outputChannel = vscode.window.createOutputChannel("StateMachine DSL");
  context.subscriptions.push(outputChannel);

  const projectPath = resolveLanguageServerProjectPath(context, outputChannel);

  if (!projectPath) {
    outputChannel.appendLine("Language server project not found.");
    void vscode.window.showErrorMessage(
      "StateMachine DSL: could not locate tools/StateMachine.Dsl.LanguageServer/StateMachine.Dsl.LanguageServer.csproj from the current workspace."
    );
    outputChannel.show(true);
    return;
  }

  const projectDirectory = path.dirname(projectPath);
  const serverWorkingDirectory = path.resolve(projectDirectory, "..", "..");

  outputChannel.appendLine(`Language server project: ${projectPath}`);
  outputChannel.appendLine(`Language server cwd: ${serverWorkingDirectory}`);

  const serverOptions: ServerOptions = {
    run: {
      command: "dotnet",
      args: ["run", "--project", projectPath],
      options: {
        cwd: serverWorkingDirectory
      }
    },
    debug: {
      command: "dotnet",
      args: ["run", "--project", projectPath],
      options: {
        cwd: serverWorkingDirectory
      }
    }
  };

  const clientOptions: LanguageClientOptions = {
    outputChannel,
    revealOutputChannelOn: RevealOutputChannelOn.Error,
    documentSelector: [
      {
        scheme: "file",
        language: "state-machine-dsl"
      }
    ],
    synchronize: {
      fileEvents: vscode.workspace.createFileSystemWatcher("**/*.sm")
    }
  };

  client = new LanguageClient(
    "stateMachineDslLanguageServer",
    "StateMachine DSL Language Server",
    serverOptions,
    clientOptions
  );

  client.onDidChangeState((state) => {
    outputChannel.appendLine(`Language client state: ${state.oldState} -> ${state.newState}`);
  });

  context.subscriptions.push(client);
  await client.start();
}

export async function deactivate(): Promise<void> {
  if (!client) {
    return;
  }

  await client.stop();
  client = undefined;
}

function resolveLanguageServerProjectPath(context: vscode.ExtensionContext, output: vscode.OutputChannel): string | undefined {
  const relativeProjectPath = path.join(
    "tools",
    "StateMachine.Dsl.LanguageServer",
    "StateMachine.Dsl.LanguageServer.csproj"
  );

  const workspaceFolders = vscode.workspace.workspaceFolders ?? [];
  const candidates: string[] = [];

  for (const folder of workspaceFolders) {
    const workspaceRoot = folder.uri.fsPath;

    candidates.push(path.join(workspaceRoot, relativeProjectPath));
    candidates.push(
      path.resolve(
        workspaceRoot,
        "..",
        "StateMachine.Dsl.LanguageServer",
        "StateMachine.Dsl.LanguageServer.csproj"
      )
    );
  }

  candidates.push(
    path.resolve(
      context.extensionPath,
      "..",
      "StateMachine.Dsl.LanguageServer",
      "StateMachine.Dsl.LanguageServer.csproj"
    )
  );

  for (const candidate of candidates) {
    output.appendLine(`Server project candidate: ${candidate}`);
  }

  return candidates.find((candidate) => fs.existsSync(candidate));
}
