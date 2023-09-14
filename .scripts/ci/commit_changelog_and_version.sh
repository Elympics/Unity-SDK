#!/usr/bin/env bash
set -e

git commit -am "chore: Bump version and add changelog for version $BRANCH_VERSION"
git push gitlab_origin "HEAD:$CI_COMMIT_BRANCH" -o ci.skip
