#!/usr/bin/env bash

if [ $# -ne 1 ]; then
  echo "Usage: ./semver_from_package.sh path/to/package.json"
  exit 1
fi

PACKAGE_PATH=$1

cat ${PACKAGE_DIR:-.}/$PACKAGE_PATH | jq -r '.version'
