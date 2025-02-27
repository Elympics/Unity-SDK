#!/bin/sh

echo "Starting check_for_tmp_commits.sh"

commit_count=$(git --no-pager log origin/develop..HEAD --grep='^tmp:' |wc -w)

if [ "$commit_count" -gt 0 ]; then
    echo "Found commit(s) containing 'tmp:'"
    exit 1
else
    echo "No commits found containing 'tmp:'"
    exit 0
fi