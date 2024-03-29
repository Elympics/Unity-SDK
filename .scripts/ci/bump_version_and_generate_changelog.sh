#!/usr/bin/env bash
set -e

echo "Branch: $BRANCH_VERSION"
echo "Package: $PACKAGE_VERSION"

function verlte {
  [  "$1" = "$(echo -e "$1\n$2" | sort -V | head -n1)" ]
}

if verlte "$BRANCH_VERSION" "$PACKAGE_VERSION"; then
  echo "Trying to release invalid version: $BRANCH_VERSION"
  exit 1
fi

echo "Running changelog generation..."

node ./.scripts/ci/update_version.js "$BRANCH_VERSION" ../../package.json \
  ../../Runtime/AssemblyInfo.cs ../../Editor/AssemblyInfo.cs ../../Tests/Runtime/AssemblyInfo.cs

echo "Version updated"
echo "Generating changelog..."

conventional-changelog -p angular -i CHANGELOG.md -s -c .conventional-changelog-context.json -n .conventional-changelog-config.js

echo "Changelog generated..."
