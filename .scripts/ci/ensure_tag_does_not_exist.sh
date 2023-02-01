#!/usr/bin/env bash
#set -e

if [ $# -ne 1 ]; then
  echo "Usage: ./ensure_tag_does_not_exist v0.2.4"
  exit 1
fi

TAG=$1

git rev-parse -q --verify $TAG

RESULT=$?
if [ $RESULT -ne 0 ]; then
  exit 0
else
  exit 1
fi
