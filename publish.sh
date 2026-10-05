#!/usr/bin/env bash
# Uploads dist/FluidLoveAlbumCases to the Steam Workshop as FRIENDS ONLY, so your crew can test it.
# First run creates the Workshop item and saves its ID to workshop-id.txt; later runs update it.
set -uo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"
PKG="$HERE/dist/FluidLoveAlbumCases"
STEAMCMD="$HOME/steamcmd/steamcmd.sh"
[ -f "$PKG/FluidLoveAlbumCases.dll" ] || { echo "No build found. Run ./build.sh first."; exit 1; }
ID=$(cat "$HERE/workshop-id.txt" 2>/dev/null || echo 0)
NOTE="${1:-Update}"
VDF="$HERE/dist/workshop.vdf"
cat > "$VDF" <<VDF
"workshopitem"
{
  "appid" "2881650"
  "publishedfileid" "$ID"
  "contentfolder" "$PKG"
  "previewfile" "$HERE/plugin/preview.png"
  "visibility" "1"
  "title" "Fluid Love Album Cases"
  "description" "Four Fluid Love albums as jewel cases in the shop (Misc tab): Ready For Business, Man Of The Cloth, Backwater Crimes and Pleasure Island DLC. Click while holding one to play a random clip from that album. Everyone nearby hears it, and so do the monsters.\n\nRequires Shop API. Everyone in the lobby needs this mod."
  "changenote" "$NOTE"
}
VDF
read -rp "Steam username: " SUSER
"$STEAMCMD" +login "$SUSER" +workshop_build_item "$VDF" +quit
NEW=$(grep -oE '"publishedfileid"[[:space:]]+"[0-9]+"' "$VDF" | grep -oE '[0-9]+' | tail -1)
if [ -n "$NEW" ] && [ "$NEW" != "0" ]; then
  echo "$NEW" > "$HERE/workshop-id.txt"
  git -C "$HERE" add workshop-id.txt && git -C "$HERE" commit -qm "Workshop item $NEW" && git -C "$HERE" push -q || true
  echo; echo "UPLOADED: https://steamcommunity.com/sharedfiles/filedetails/?id=$NEW"
else
  echo; echo "Upload didn't return an ID. Send Claude the last lines above."
fi
