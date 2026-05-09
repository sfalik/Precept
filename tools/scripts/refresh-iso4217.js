const fs = require("fs");
const https = require("https");
const path = require("path");

const workspaceRoot = path.resolve(__dirname, "..", "..");
const relativeTargetPath = path.join("src", "Precept", "Data", "Iso4217", "list-one.xml");
const targetPath = path.join(workspaceRoot, relativeTargetPath);
const sources = [
  {
    label: "SIX Group",
    url: "https://www.six-group.com/dam/download/financial-information/data-center/iso-4217/lists/list-one.xml"
  },
  {
    label: "SIX Group fallback",
    url: "https://www.six-group.com/dam/download/financial-information/data-center/iso-currrency/lists/list-one.xml"
  }
];

process.stdout.write("Downloading ISO 4217 from SIX Group...\n");
fs.mkdirSync(path.dirname(targetPath), { recursive: true });

downloadFrom(0);

function downloadFrom(index) {
  const source = sources[index];
  const request = https.get(source.url, (response) => {
    if (response.statusCode !== 200) {
      const message = `Failed to download ISO 4217 XML from ${source.label}: HTTP ${response.statusCode}${response.statusMessage ? ` ${response.statusMessage}` : ""}`;
      response.resume();

      if (index + 1 < sources.length) {
        console.error(`${message}. Trying ${sources[index + 1].label}...`);
        downloadFrom(index + 1);
        return;
      }

      console.error(message);
      process.exit(1);
      return;
    }

    const chunks = [];
    response.on("data", (chunk) => {
      chunks.push(chunk);
    });
    response.on("end", () => {
      try {
        fs.writeFileSync(targetPath, Buffer.concat(chunks));
        process.stdout.write(`Saved to ${relativeTargetPath.replace(/\\/g, "/")}\n`);
      } catch (error) {
        console.error(`Failed to save ISO 4217 XML: ${error instanceof Error ? error.message : String(error)}`);
        process.exit(1);
      }
    });
    response.on("error", (error) => {
      handleDownloadError(index, source.label, error);
    });
  });

  request.on("error", (error) => {
    handleDownloadError(index, source.label, error);
  });
}

function handleDownloadError(index, sourceLabel, error) {
  const message = `Failed to download ISO 4217 XML from ${sourceLabel}: ${error.message}`;

  if (index + 1 < sources.length) {
    console.error(`${message}. Trying ${sources[index + 1].label}...`);
    downloadFrom(index + 1);
    return;
  }

  console.error(message);
  process.exit(1);
}
