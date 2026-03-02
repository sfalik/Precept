import * as path from "path";
import * as vscode from "vscode";
import {
  LanguageClient,
  LanguageClientOptions,
  ServerOptions
} from "vscode-languageclient/node";

let client: LanguageClient | undefined;

export async function activate(context: vscode.ExtensionContext): Promise<void> {
  const projectPath = path.resolve(
    context.extensionPath,
    "..",
    "StateMachine.Dsl.LanguageServer",
    "StateMachine.Dsl.LanguageServer.csproj"
  );
  const serverWorkingDirectory = path.resolve(context.extensionPath, "..", "..");

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
