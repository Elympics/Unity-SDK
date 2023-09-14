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
    const args = process.argv.slice(2);
    const version = args[0];
    const packagePath = args[1];
    const assemblyInfoPaths = args.slice(2);

    if (args.length < 3 || !isCorrectVersion(version)) {
        usage();
        process.exit(1);
    }

    const packageFileName = path.resolve(__dirname, packagePath);
    const packageJson = JSON.parse(await fs.promises.readFile(packageFileName, {encoding: 'utf8'}));
    packageJson.version = version;
    await fs.promises.writeFile(packageFileName, JSON.stringify(packageJson, null, 2));
    console.log(`Updated package version to ${version}`);

    await Promise.all(assemblyInfoPaths.map(async (assemblyInfoPath) => {
        const assemblyFileName = path.resolve(__dirname, assemblyInfoPath);
        const assemblyInfo = await fs.promises.readFile(assemblyFileName, {encoding: 'utf8'});
        const updatedAssemblyInfo = assemblyInfo.replace(/(?<prefix>AssemblyVersion\(").*(?<suffix>"\))/, `$<prefix>${version}.0$<suffix>`);
        await fs.promises.writeFile(assemblyFileName, updatedAssemblyInfo);
        console.log(`Updated ${assemblyFileName} to version ${version}`);
    }));
}

main().catch((error) => {
    console.error(error);
    process.exitCode = 1;
});
