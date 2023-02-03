#!/usr/bin/env bash

if [ $# -ne 1 ]; then
  echo "Usage: ./semver_from_brach release/v0.2.4"
  exit 1
fi

DESCRIBE=$1

if [[ "${DESCRIBE}" =~ ^release/v[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  FULL_VERSION=`echo $DESCRIBE | awk '{split($0,a,"/v"); print a[2]}'`
  MAJOR=`echo $FULL_VERSION | awk '{split($0,a,"."); print a[1]}'`
  MINOR=`echo $FULL_VERSION | awk '{split($0,a,"."); print a[2]}'`
  PATCH=`echo $FULL_VERSION | awk '{split($0,a,"."); print a[3]}'`

  echo ${MAJOR}.${MINOR}.${PATCH}
else
  echo "Invalid version. Expected format release/vX.X.X"
  exit 1
fi
