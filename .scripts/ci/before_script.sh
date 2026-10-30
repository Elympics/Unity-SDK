#!/usr/bin/env bash

set -e
set -x
mkdir -p /root/.local/share/unity3d/Unity/
set +x

unity_license_destination=/root/.local/share/unity3d/Unity/Unity_lic.ulf

if [ -n "$UNITY_LICENSE" ]
then
  echo "Writing '\$UNITY_LICENSE' to license file ${unity_license_destination}"
  echo "${UNITY_LICENSE}" | tr -d '\r' > ${unity_license_destination}
else
  echo "'\$UNITY_LICENSE' env var not found"
fi

echo "Configuring git credentials"

GIT_CREDENTIALS_FILE=~/.git-credentials
BASE_URL=$(echo $CI_REPOSITORY_URL | sed "s;\/*$CI_PROJECT_PATH.*;;")
echo "$BASE_URL" > $GIT_CREDENTIALS_FILE
git config --global credential.helper store
