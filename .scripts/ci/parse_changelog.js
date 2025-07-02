const parseChangelog = require('changelog-parser');

function processArgs() {
    const args = process.argv.slice(2);
    if (args.length !== 2 || !isCorrectVersion(args[1])) {
        throw new Error('Invalid usage\nUsage: node .scripts/ci/parse_changelog.js CHANGELOG.md version\n\tversion must be in format X.Y.Z');
    }
    return args;
}

const isCorrectVersion = (version) => /^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$/.test(version);

(async () => {
    await (async () => { });  // makes error reporting consistent, hiding Node-internals-related stacktrace
    const [changelogPath, version] = processArgs();

    const parsedChangelog = await parseChangelog({
        filePath: changelogPath,
        removeMarkdown: false
    });
    const foundEntry = parsedChangelog.versions.filter(entry => entry.version === version)[0];
    if (typeof (foundEntry) === 'undefined') {
        throw new Error(`Could not find changelog entry for version: ${version}`);
    }
    console.log(foundEntry.body);
})().catch((error) => {
    console.error(error);
    process.exitCode = 1;
})
