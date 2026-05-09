const path = require("path");
const esbuild = require("esbuild");

const extensionRoot = __dirname;

esbuild.build({
  entryPoints: [path.join(extensionRoot, "src", "extension.ts")],
  outfile: path.join(extensionRoot, "out", "extension.js"),
  bundle: true,
  platform: "node",
  format: "cjs",
  target: "node20",
  sourcemap: true,
  minify: true,
  legalComments: "none",
  external: ["vscode"],
  tsconfig: path.join(extensionRoot, "tsconfig.json")
}).catch((error) => {
  console.error(error);
  process.exit(1);
});
