#!/usr/bin/env bash

PACKAGE_FOLDER=${PACKAGE_DIR}/

mv -f ./.editorconfig $PACKAGE_FOLDER

dotnet restore "./${TESTING_PROJECT_NAME}/${TESTING_PROJECT_NAME}.sln"
dotnet format --no-restore --verify-no-changes --verbosity diagnostic --severity info --report . "./${TESTING_PROJECT_NAME}/${TESTING_PROJECT_NAME}.sln" || true

jq -r 'def make_fingerprint: .FilePath + ":" + (.LineNumber | tostring) + ":" + (.CharNumber | tostring) + " " + .DiagnosticId;
    def has_severity(severity): .FormatDescription | ascii_downcase | startswith(severity);
    def make_severity: if . | has_severity("error") then "critical" else (if . | has_severity("info") then "minor" else "major" end) end;
    map(.FileChanges[] + { "FilePath": .FilePath }) | map({
        "description": .FormatDescription,
        "check_name": .DiagnosticId,
        "fingerprint": . | make_fingerprint,
        "severity": . | make_severity,
        "location": { "path": .FilePath, "lines": { "begin": .LineNumber, "begin_char": .CharNumber } }
    })' ./format-report.json > ./format-report-unsorted.codequality.json
jq -r 'def get_severity_index: if .severity == "critical" then 0 elif .severity == "major" then 1 else 2 end;
  sort_by(. | get_severity_index, .location.path, .location.lines.begin)' ./format-report-unsorted.codequality.json > ./format-report.codequality.json
sed -i 's@/Samples/@/Samples~/@' ./format-report.codequality.json
sed -i 's@'"$PACKAGE_FOLDER"'@@i' ./format-report.codequality.json

# Output summary to console
GREEN='\033[0;32m'
CLEAR='\033[0m'
echo -e "${GREEN}Summarizing the formatting check...${CLEAR}"
echo "===== BEGIN ====="
jq -r '.[] | "[" + .severity  + "] " + .location.path + ":" + (.location.lines.begin | tostring) + ":" + (.location.lines.begin_char | tostring) + "; " + .description' ./format-report.codequality.json
echo "=====  END  ====="

# Fail on any warning
dotnet format --no-restore --verify-no-changes --verbosity diagnostic --severity warn "./${TESTING_PROJECT_NAME}/${TESTING_PROJECT_NAME}.sln" > /dev/null 2> /dev/null
