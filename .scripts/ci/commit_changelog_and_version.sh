#!/usr/bin/env bash
set -e

git add package.json Runtime/AssemblyInfo.cs CHANGELOG.md
git commit -m "chore: Bump version and add changelog for version $BRANCH_VERSION"
git push gitlab_origin "HEAD:$CI_COMMIT_BRANCH" -o ci.skip
