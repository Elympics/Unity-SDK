#!/usr/bin/env bash
set -e

echo $BRANCH_VERSION
echo $PACKAGE_VERSION
if [ $PACKAGE_VERSION = $BRANCH_VERSION ]; then
  echo "Same versions detected"
  exit 0
fi

echo "Different versions! Running changelog generation..."

node ./scripts/ci/update_version.js ./package.json ./Runtime/AssemblyInfo.cs $BRANCH_VERSION

echo "Version updated"
echo "Generating changelog..."

conventional-changelog -p angular -i CHANGELOG.md -s -c .conventional-changelog-context.json -n .conventional-changelog-config.js

echo "Changelog generated..."

git add package.json Runtime/AssemblyInfo.cs CHANGELOG.md

git commit -m "chore: Bump version and add changelog for version $BRANCH_VERSION"

git push gitlab_origin HEAD:$CI_COMMIT_BRANCH -o ci.skip
