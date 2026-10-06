#!/usr/bin/env bash
# Uploads dist-oldmac/FluidLoveOldMac to the Steam Workshop as FRIENDS ONLY, so your crew can test it.
# First run creates the Workshop item and saves its ID to workshop-id-oldmac.txt; later runs update it.
set -uo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"
PKG="$HERE/dist-oldmac/FluidLoveOldMac"
STEAMCMD="$HOME/steamcmd/steamcmd.sh"
[ -f "$PKG/FluidLoveOldMac.dll" ] || { echo "No build found. Run ./build.sh first."; exit 1; }
ID=$(cat "$HERE/workshop-id-oldmac.txt" 2>/dev/null || echo 0)
NOTE="${1:-Update}"
VDF="$HERE/dist-oldmac/workshop.vdf"
cat > "$VDF" <<VDF
"workshopitem"
{
  "appid" "2881650"
  "publishedfileid" "$ID"
  "contentfolder" "$PKG"
  "previewfile" "$HERE/oldmac/preview.png"
  "visibility" "1"
  "title" "Ol' Mac (Fluid Love)"
  "description" "Ol' Mac, a big banjo-playing chicken man from Fluid Love, now hunts you through the Old World. He chases, he hits, and he always turns to face your camera.

Everyone in the lobby needs this mod. Test builds: host presses F8 to spawn him."
  "changenote" "$NOTE"
}
VDF
read -rp "Steam username: " SUSER
"$STEAMCMD" +login "$SUSER" +workshop_build_item "$VDF" +quit
NEW=$(grep -oE '"publishedfileid"[[:space:]]+"[0-9]+"' "$VDF" | grep -oE '[0-9]+' | tail -1)
if [ -n "$NEW" ] && [ "$NEW" != "0" ]; then
  echo "$NEW" > "$HERE/workshop-id-oldmac.txt"
  git -C "$HERE" add workshop-id-oldmac.txt && git -C "$HERE" commit -qm "Workshop item $NEW" && git -C "$HERE" push -q || true
  echo; echo "UPLOADED: https://steamcommunity.com/sharedfiles/filedetails/?id=$NEW"
else
  echo; echo "Upload didn't return an ID. Send Claude the last lines above."
fi
