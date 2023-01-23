#!/usr/bin/env bash

if [ $# -ne 0 ]; then
  echo "Usage: ./semver_from_package.sh"
  exit 1
fi

cat ${PACKAGE_DIR:-.}/package.json | grep "\"version\":" | head -1 | awk '{split($0,a,"\""); print a[4]}'
