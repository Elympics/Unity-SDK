#!/usr/bin/env bash
# Writes SKIP_CODE_JOBS=1 to $1 when the main commit is a release merge that brings no code
# beyond a develop commit whose pipeline ran test:editmode successfully.
# Reads the code paths from .code_paths in .gitlab-ci.yml (needs yq).
# ASSEMBLY_INFO_FILES: whitespace-separated files where the version bump changes AssemblyVersion; only that line may differ.

set -uo pipefail

OUTPUT_PATH=${1:-skip.env}

skip_code_jobs() {
  echo "SKIP_CODE_JOBS=$1" | tee "$OUTPUT_PATH"
  exit 0
}

run_all() {
  echo "Running build, test and verify jobs: $1"
  skip_code_jobs 0
}

subject=$(git log -1 --format=%s HEAD)
[[ $subject =~ ^Merge\ branch\ \'release/v[0-9]+\.[0-9]+\.[0-9]+\'\ into\ \'main\'$ ]] ||
  run_all "not a release merge ($subject)"
git rev-parse -q --verify HEAD^2 > /dev/null || run_all "HEAD has no second parent"
git merge-base --is-ancestor HEAD^1 HEAD^2 || run_all "main has commits that the release branch lacks"
git diff --quiet HEAD^2 HEAD || run_all "the merge commit changes files"

git fetch -q origin develop || run_all "cannot fetch develop"
cut_commit=$(git merge-base HEAD^2 FETCH_HEAD) || run_all "the release branch has no merge base with develop"
echo "Release branch was cut from develop at $cut_commit"

code_paths=$(yq '.".code_paths"[] | ":(glob)" + .' .gitlab-ci.yml) && [ -n "$code_paths" ] || run_all "cannot read .code_paths"
mapfile -t code_pathspecs <<< "$code_paths"
git ls-files -- "${code_pathspecs[@]}" | grep -qx .gitlab-ci.yml || run_all "the .code_paths pathspecs don't match .gitlab-ci.yml"
for assembly_info_file in ${ASSEMBLY_INFO_FILES:-}; do
  code_pathspecs+=(":!$assembly_info_file")
done
git diff --quiet "$cut_commit" HEAD^2 -- "${code_pathspecs[@]}" ':!package.json' ||
  run_all "the release branch changed code after the cut"
if [ -n "${ASSEMBLY_INFO_FILES:-}" ]; then
  assembly_info_changes=$(git diff -U0 "$cut_commit" HEAD^2 -- $ASSEMBLY_INFO_FILES |
    grep '^[-+]' | grep -v -e '^+++ ' -e '^--- ' -e '^[-+]\[assembly: AssemblyVersion(')
  [ -z "$assembly_info_changes" ] || run_all "the release branch changed AssemblyInfo.cs beyond the version"
fi
[ "$(git show "$cut_commit:package.json" | jq -S 'del(.version)')" = "$(git show HEAD^2:package.json | jq -S 'del(.version)')" ] ||
  run_all "the release branch changed package.json beyond the version"

[ -n "${GITLAB_ACCESS_TOKEN:-}" ] || run_all "GITLAB_ACCESS_TOKEN is not set"
api() {
  curl -sfg --header "PRIVATE-TOKEN: $GITLAB_ACCESS_TOKEN" "$CI_API_V4_URL/projects/$CI_PROJECT_ID/$1"
}
pipeline_ids=$(api "pipelines?sha=$cut_commit&ref=develop&status=success" | jq -r '.[].id') ||
  run_all "cannot list the develop pipelines"
for pipeline_id in $pipeline_ids; do
  if api "pipelines/$pipeline_id/jobs?scope[]=success&per_page=100" | jq -e 'any(.[]; .name == "test:editmode")' > /dev/null; then
    echo "Develop pipeline $pipeline_id already tested this code"
    skip_code_jobs 1
  fi
done
run_all "no successful develop pipeline ran test:editmode on $cut_commit"
