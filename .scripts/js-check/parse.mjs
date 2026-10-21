// Syntax gate for .jslib/.jspre: parses with the same acorn ecmaVersion that Unity 6.3's Emscripten (3.1.39) uses.
// tsc doesn't reject newer syntax (e.g. `||=`, class fields), so without this they would only fail in a WebGL build.
import { readFileSync } from 'node:fs';
import { parse } from 'acorn';

const ECMA_VERSION = 2020;
let failed = false;
for (const file of process.argv.slice(2)) {
    try {
        parse(readFileSync(file, 'utf8'), { ecmaVersion: ECMA_VERSION, sourceType: 'script', allowHashBang: false });
    } catch (e) {
        failed = true;
        console.error(`${file.replace(/\.js$/, '')}:${e.loc ? `${e.loc.line}:${e.loc.column + 1}` : ''} ${e.message} (ES${ECMA_VERSION})`);
    }
}
process.exit(failed ? 1 : 0);
