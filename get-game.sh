#!/usr/bin/env bash
# Downloads Content Warning (Windows build) with your Steam login, works out its Unity version,
# installs that Unity editor + Windows build support, then sends an API report to the repo for Claude.
set -uo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"
WS=/workspaces
GAME="$WS/cw-game"
STEAMCMD="$HOME/steamcmd/steamcmd.sh"
log(){ echo -e "\n=== $* ===\n"; }

# ---------- 1. Game files ----------
if [ ! -f "$GAME/Content Warning_Data/Managed/Assembly-CSharp.dll" ]; then
  log "Steam login"
  echo "Steam will ask for your password, then a Steam Guard code (or approve it in the Steam app)."
  echo "Your login goes straight to Steam. Nothing is saved to the repo."
  read -rp "Steam username: " SUSER
  "$STEAMCMD" +@sSteamCmdForcePlatformType windows +force_install_dir "$GAME" \
    +login "$SUSER" +app_update 2881650 validate +quit
fi
[ -f "$GAME/Content Warning_Data/Managed/Assembly-CSharp.dll" ] || { echo "GAME DOWNLOAD FAILED. Run ./get-game.sh again, or send Claude the last lines above."; exit 1; }
echo "Game files OK"

# ---------- 2. Unity version ----------
log "Working out the game's Unity version"
VRE='(20[0-9]{2}|6000)\.[0-9]+\.[0-9]+[fp][0-9]+'
VER=""
for f in "$GAME/Content Warning_Data/globalgamemanagers" "$GAME/Content Warning_Data/data.unity3d" "$GAME/UnityPlayer.dll"; do
  [ -f "$f" ] || continue
  VER=$(strings -n 6 "$f" | grep -m1 -oE "$VRE" || true)
  [ -n "$VER" ] && break
done
[ -n "$VER" ] || { echo "Could not find the Unity version"; "$HERE/report.sh"; exit 1; }
echo "Content Warning uses Unity $VER"
echo "$VER" > "$WS/.unity-version"

CS=$(curl -fsSL "https://services.api.unity.com/unity/editor/release/v1/releases?version=$VER&limit=1" | jq -r '.results[0].shortRevision // empty' 2>/dev/null || true)
[ -z "$CS" ] && CS=$(strings -n 6 "$GAME/UnityPlayer.dll" | grep -m1 -oE "$VER \([0-9a-f]{12}\)" | grep -oE '[0-9a-f]{12}' || true)
[ -n "$CS" ] || { echo "Could not find the changeset for $VER"; "$HERE/report.sh"; exit 1; }
echo "Changeset $CS"
echo "$CS" > "$WS/.unity-changeset"

# ---------- 3. Unity editor + Windows build support ----------
ED="$WS/unity/$VER"
BASE="https://download.unity3d.com/download_unity/$CS"
if [ ! -x "$ED/Editor/Unity" ]; then
  log "Unity $VER editor (about 3 GB, be patient)"
  mkdir -p "$ED" /tmp/unitydl
  wget -q --show-progress -O /tmp/unitydl/editor.tar.xz "$BASE/LinuxEditorInstaller/Unity.tar.xz" || { echo "Editor download failed"; exit 1; }
  tar -xJf /tmp/unitydl/editor.tar.xz -C "$ED" && rm -f /tmp/unitydl/editor.tar.xz
  if [ ! -x "$ED/Editor/Unity" ]; then
    BIN=$(find "$ED" -maxdepth 4 -type f -name Unity -path '*/Editor/Unity' | head -1)
    [ -n "$BIN" ] && mv "$(dirname "$(dirname "$BIN")")"/* "$ED"/ 2>/dev/null || true
  fi
fi
[ -x "$ED/Editor/Unity" ] || { echo "UNITY INSTALL FAILED"; "$HERE/report.sh"; exit 1; }

PE="$ED/Editor/Data/PlaybackEngines"
if [ ! -d "$PE/WindowsStandaloneSupport" ]; then
  log "Windows build support (asset bundles for the PC game)"
  W=/tmp/unitydl/win; rm -rf "$W"; mkdir -p "$W"
  wget -q --show-progress -O "$W/win.tar.xz" "$BASE/LinuxEditorTargetInstaller/UnitySetup-Windows-Mono-Support-for-Editor-$VER.tar.xz" \
    && tar -xJf "$W/win.tar.xz" -C "$W" || echo "Windows support download failed"
  SRC=$(find "$W" -type d -name WindowsStandaloneSupport | head -1)
  [ -n "$SRC" ] && { mkdir -p "$PE"; mv "$SRC" "$PE/"; }
  rm -rf /tmp/unitydl
fi
[ -d "$PE/WindowsStandaloneSupport" ] && echo "Windows build support installed" || echo "WINDOWS SUPPORT MISSING (report will show it)"

# ---------- 4. API report for Claude ----------
log "Building the API report"
"$HERE/api-dump.sh"
log "DONE. Tell Claude: game done"
