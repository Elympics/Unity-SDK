var parseChangelog = require('changelog-parser');

function usage() {
    console.log("Usage: ./node parseChangelog.js path/to/CHANGELOG.md [version]");
    console.log("[version] must be in format X.Y.Z");
}

function isCorrectVersion(string) {
    return /^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$/.test(string);
}

async function main() {
    args = process.argv.slice(2);
    if (args.length != 2 || !isCorrectVersion(args[1])) {
        usage();
        process.exit(1);
    }

    changelogPath = args[0];
    version = args[1];

    await parseChangelog({
        filePath: changelogPath,
        removeMarkdown: false
    })
        .then(function (result) {
            version = result.versions.filter(entry => entry.version == version)[0];
            console.log(version.body);
        });
}

main().catch((error) => {
    console.error(error);
    process.exitCode = 1;
})
