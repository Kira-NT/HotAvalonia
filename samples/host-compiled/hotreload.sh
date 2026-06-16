#!/usr/bin/env bash
#
# hotreload.sh - one-shot launcher for HotAvalonia host-compiled hot reload (iOS).
#
# Starts the HARFS file server and the host-side XAML compiler (watch mode), each ONLY if it isn't
# already running, then waits. Leave it in a terminal; Ctrl-C stops whatever THIS script started
# (an already-running server/watcher is reused, not killed). iOS host-compiled hot reload is
# macOS-only - you can only build iOS apps on a Mac. It is NOT tied to your build: start it once,
# then edit .axaml files freely (no rebuild needed).
#
# Usage:
#   hotreload.sh --app-project <App.csproj> [options]
#
# Options:
#   --app-project <csproj>  The iOS app project (required; the compiler discovers the build closure from it).
#   --views-dir <dir>       Directory of .axaml files to watch (default: the app project's git repo root).
#   --bind <addr>           HARFS bind address (default: 0.0.0.0 = LAN/real device; use 127.0.0.1 for the simulator).
#   --port <n>              HARFS port (default: 9500).
#   --secret <s>            HARFS shared secret (default: hotavalonia-dev).
#   --no-harfs              Don't start HARFS (e.g. you run your own server).
#   --no-compiler           Don't start the host compiler watcher.
#
# Tool resolution: HotAvalonia.Remote.dll / HotAvalonia.HostCompiler.dll are taken from the newest cached
# `hotavalonia` NuGet package's tools/ folder. Override with the HARFS_DLL / HOSTCOMPILER_DLL env vars
# (e.g. to point at a local build output).
#
# Prerequisite: build + deploy the app once first, so the compiler can auto-discover its reference closure.
set -euo pipefail

APP_PROJECT=""
VIEWS_DIR=""
BIND="0.0.0.0"
PORT="9500"
SECRET="hotavalonia-dev"
START_HARFS=1
START_COMPILER=1

while [ $# -gt 0 ]; do
  case "$1" in
    --app-project) APP_PROJECT="$2"; shift 2;;
    --views-dir)   VIEWS_DIR="$2"; shift 2;;
    --bind)        BIND="$2"; shift 2;;
    --port)        PORT="$2"; shift 2;;
    --secret)      SECRET="$2"; shift 2;;
    --no-harfs)    START_HARFS=0; shift;;
    --no-compiler) START_COMPILER=0; shift;;
    -h|--help)     sed -n '2,30p' "$0"; exit 0;;
    *) echo "unknown option: $1" >&2; exit 2;;
  esac
done

[ -n "$APP_PROJECT" ] || { echo "error: --app-project <csproj> is required (use --help)" >&2; exit 2; }
[ -f "$APP_PROJECT" ] || { echo "error: app project not found: $APP_PROJECT" >&2; exit 2; }
APP_PROJECT="$(cd "$(dirname "$APP_PROJECT")" && pwd)/$(basename "$APP_PROJECT")"

REPO_ROOT="$(git -C "$(dirname "$APP_PROJECT")" rev-parse --show-toplevel 2>/dev/null || dirname "$APP_PROJECT")"
VIEWS_DIR="${VIEWS_DIR:-$REPO_ROOT}"

NUGET="${NUGET_PACKAGES:-$HOME/.nuget/packages}"
find_tool() { ls -d "$NUGET"/hotavalonia/*/tools/"$1" 2>/dev/null | sort -V | tail -1 || true; }
HARFS_DLL="${HARFS_DLL:-$(find_tool HotAvalonia.Remote.dll)}"
HOSTCOMPILER_DLL="${HOSTCOMPILER_DLL:-$(find_tool HotAvalonia.HostCompiler.dll)}"

PIDS=()
cleanup() { [ ${#PIDS[@]} -gt 0 ] && kill "${PIDS[@]}" 2>/dev/null || true; }
trap cleanup EXIT
trap 'exit 0' INT TERM HUP

# --- HARFS: start only if the port isn't already served ---
if [ "$START_HARFS" -eq 1 ]; then
  if lsof -nP -iTCP:"$PORT" -sTCP:LISTEN >/dev/null 2>&1; then
    echo ">>> HARFS: already listening on :$PORT - reusing it."
  else
    [ -n "$HARFS_DLL" ] && [ -f "$HARFS_DLL" ] || { echo "error: HotAvalonia.Remote.dll not found - set HARFS_DLL." >&2; exit 1; }
    echo ">>> HARFS: starting on $BIND:$PORT (root=$REPO_ROOT)"
    dotnet "$HARFS_DLL" --root "$REPO_ROOT" --endpoint "$BIND:$PORT" --secret "text:$SECRET" &
    PIDS+=($!)
  fi
fi

# --- host compiler watch: start only if not already watching this app ---
if [ "$START_COMPILER" -eq 1 ]; then
  LOCK="${TMPDIR:-/tmp}/hotavalonia-hostcompiler-$(echo "$APP_PROJECT" | shasum | cut -c1-8).pid"
  if [ -f "$LOCK" ] && kill -0 "$(cat "$LOCK" 2>/dev/null)" 2>/dev/null; then
    echo ">>> Host compiler: already watching this app (pid $(cat "$LOCK")) - skipping."
  else
    [ -n "$HOSTCOMPILER_DLL" ] && [ -f "$HOSTCOMPILER_DLL" ] || { echo "error: HotAvalonia.HostCompiler.dll not found - set HOSTCOMPILER_DLL." >&2; exit 1; }
    echo ">>> Host compiler: watching $VIEWS_DIR (app=$(basename "$APP_PROJECT"))"
    dotnet "$HOSTCOMPILER_DLL" watch "$VIEWS_DIR" --app-project "$APP_PROJECT" &
    CPID=$!; PIDS+=($CPID); echo "$CPID" > "$LOCK"
    ( wait "$CPID" 2>/dev/null; rm -f "$LOCK" ) &
  fi
fi

[ ${#PIDS[@]} -eq 0 ] && { echo "Nothing to start (already running, or both disabled)."; exit 0; }
echo ">>> Running. Ctrl-C to stop. Edit a Mobile .axaml to test."
wait
