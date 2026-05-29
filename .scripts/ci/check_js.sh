#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PACKAGE_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
TOOLS_DIR="$SCRIPT_DIR/../js-check"

cd "$TOOLS_DIR" && npm ci
cd "$PACKAGE_ROOT"

export PATH="$TOOLS_DIR/node_modules/.bin:$PATH"

# create temporary .js copies for type-checking as tsc ignores non-standard extensions
declare -a TMPFILES=()
cleanup() { for f in "${TMPFILES[@]:-}"; do rm -f "$f"; done; }
trap cleanup EXIT INT TERM

while IFS= read -r -d '' file; do
    tmpfile="${file}.js"
    cp "$file" "$tmpfile"
    TMPFILES+=("$tmpfile")
done < <(find . \( -name "*.jslib" -o -name "*.jspre" \) -not -path "*/Samples~/*" -print0)

tsc -p jsconfig.json --noEmit 2>&1 | sed 's/.jslib.js/.jslib/gi'
