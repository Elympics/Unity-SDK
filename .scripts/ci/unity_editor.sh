#!/usr/bin/env bash
# Runs the Unity Editor with the given arguments; pass -logFile /dev/stdout. After a fatal signal, Unity can hang until
# the job times out, so it's killed after UNITY_CRASH_GRACE_SECONDS and this script exits with 134, as for an abort.

GRACE=${UNITY_CRASH_GRACE_SECONDS:-30}
FLAG_DIR=$(mktemp -d)
CRASH_FLAG=$FLAG_DIR/crashed
LOG_PIPE=$FLAG_DIR/log
mkfifo "$LOG_PIPE"

while IFS= read -r line; do
  printf '%s\n' "$line"
  [[ $line == *"Caught fatal signal"* ]] && touch "$CRASH_FLAG" 2>/dev/null
done < "$LOG_PIPE" &
READER_PID=$!

# Its own session (and process group), so killing it also kills Xvfb and Unity's child processes.
setsid ${UNITY_EXECUTABLE:-xvfb-run --auto-servernum --server-args='-screen 0 640x480x24' unity-editor} "$@" \
  > "$LOG_PIPE" 2>&1 &
UNITY_PID=$!

(
  trap 'kill "$SLEEP_PID" 2>/dev/null; exit' TERM
  until [ -e "$CRASH_FLAG" ]; do sleep 1; done
  sleep "$GRACE" &
  SLEEP_PID=$!
  wait "$SLEEP_PID"
  echo "Unity didn't exit ${GRACE} s after a fatal signal; killing it" >&2
  kill -KILL -- "-$UNITY_PID" 2>/dev/null || kill -KILL "$UNITY_PID"
) &
WATCHDOG_PID=$!

# 2>/dev/null drops bash's "Killed" notice, which repeats the whole command.
wait "$UNITY_PID" 2>/dev/null
UNITY_EXIT_CODE=$?
kill "$WATCHDOG_PID" 2>/dev/null

# Leftover Unity processes can keep the log pipe open, so the reader gets at most 10 s to finish.
for _ in {1..100}; do
  kill -0 "$READER_PID" 2>/dev/null || break
  sleep 0.1
done
kill "$READER_PID" 2>/dev/null

if [ -e "$CRASH_FLAG" ] && [ "$UNITY_EXIT_CODE" -eq 137 ]; then
  UNITY_EXIT_CODE=134
fi
rm -rf "$FLAG_DIR"
exit "$UNITY_EXIT_CODE"
