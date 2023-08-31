const fs = require('fs');
const path = require('path');

function usage() {
  console.log("Usage: node bump_package.js ./path/to/package.json ./path/to/Runtime/AssemblyInfo.cs [version]");
  console.log("[version] must be in format X.Y.Z");
}

function isCorrectVersion(string) {
  return /^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$/.test(string);
}

async function main() {
  args = process.argv.slice(2);
  version = args[0];
  packagePath = args[1];
  assemblyPaths = args.slice(2);

  if (args.length < 3 || !isCorrectVersion(version)) {
    usage();
    process.exit(1);
  }

  const packageFileName = path.resolve(__dirname, packagePath);
  const packageJson = JSON.parse(fs.readFileSync(packageFileName));
  packageJson.version = version;
  fs.writeFileSync(packageFileName, JSON.stringify(packageJson, null, 2));
  console.log(`Updated package version to ${version}`);

  assemblyPaths.forEach(assemblyPath => {
    const assemblyFileName = path.resolve(__dirname, assemblyPath);
    const assemblyFile = fs.readFileSync(assemblyFileName)
        .toString()
        .replace(/(AssemblyVersion\(\").*(\.[0-9]+\"\))/, `$1${version}$2`);
    fs.writeFileSync(assemblyFileName, assemblyFile);
    console.log(`Updated ${assemblyFileName} Assembly version to ${version}`);
  });
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
