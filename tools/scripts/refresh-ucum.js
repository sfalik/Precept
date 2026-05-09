const fs = require("fs");
const https = require("https");
const path = require("path");

const workspaceRoot = path.resolve(__dirname, "..", "..");
const relativeTargetPath = path.join("src", "Precept", "Data", "Ucum", "ucum-essence.xml");
const targetPath = path.join(workspaceRoot, relativeTargetPath);
const sources = [
  {
    label: "ucum-org GitHub",
    url: "https://raw.githubusercontent.com/ucum-org/ucum/master/ucum-essence.xml"
  },
  {
    label: "ucum.org fallback",
    url: "https://ucum.org/ucum-essence.xml"
  }
];

process.stdout.write("Downloading UCUM essence XML...\n");
fs.mkdirSync(path.dirname(targetPath), { recursive: true });

downloadFrom(0);

function downloadFrom(index) {
  const source = sources[index];
  const request = https.get(source.url, (response) => {
    if (response.statusCode !== 200) {
      const message = `Failed to download UCUM XML from ${source.label}: HTTP ${response.statusCode}${response.statusMessage ? ` ${response.statusMessage}` : ""}`;
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
        console.error(`Failed to save UCUM XML: ${error instanceof Error ? error.message : String(error)}`);
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
  const message = `Failed to download UCUM XML from ${sourceLabel}: ${error.message}`;

  if (index + 1 < sources.length) {
    console.error(`${message}. Trying ${sources[index + 1].label}...`);
    downloadFrom(index + 1);
    return;
  }

  console.error(message);
  process.exit(1);
}
