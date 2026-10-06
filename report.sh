#!/usr/bin/env bash
# Sends diagnostics to the repo so Claude can read them.
HERE="$(cd "$(dirname "$0")" && pwd)"
R="$HERE/reports"; mkdir -p "$R"
GAME=/workspaces/cw-game
{
  echo "== date"; date
  echo "== disk / memory"; df -h /workspaces | tail -1; free -h | sed -n 2p
  echo "== unity version / changeset"; cat /workspaces/.unity-version /workspaces/.unity-changeset 2>/dev/null
  echo "== editors"; ls /workspaces/unity 2>/dev/null; ls /workspaces/unity/*/Editor/Data/PlaybackEngines 2>/dev/null
  echo "== game root"; ls "$GAME" 2>/dev/null | head -30
  echo "== Managed DLLs"; ls "$GAME/Content Warning_Data/Managed" 2>/dev/null
  echo "== setup.log tail"; tail -30 /workspaces/setup.log 2>/dev/null | cut -c1-200
  echo "== build log key lines (album cases)"; grep -nE "error CS|Exception|error|licen|\[CWAlbum\]" "$HERE/dist/build.log" 2>/dev/null | tail -40 | cut -c1-300
  echo "== build log key lines (Ol' Mac)"; grep -nE "error CS|Exception|error" "$HERE/dist-oldmac/build.log" 2>/dev/null | tail -60 | cut -c1-300
} > "$R/diag.txt" 2>&1
cd "$HERE" && git add -f reports && git commit -qm "Report $(date +%H:%M)" && git pull -q --rebase && git push -q && echo "Report sent to the repo."
