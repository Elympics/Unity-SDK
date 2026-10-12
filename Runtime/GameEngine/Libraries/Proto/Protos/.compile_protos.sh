#!/usr/bin/env bash
PROTOS_OUT_DIR="${PROTOS_OUT_DIR:-../ProtosCompiled}"
mkdir -p "$PROTOS_OUT_DIR"
for proto in *.proto
  do protoc --csharp_out="$PROTOS_OUT_DIR" "$proto"
done
