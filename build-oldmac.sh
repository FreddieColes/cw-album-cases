#!/usr/bin/env bash
# Compiles the plugin (album art + clips baked in) and packages it to dist-oldmac/FluidLoveOldMac.zip
set -uo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"
MANAGED="/workspaces/cw-game/Content Warning_Data/Managed"
OUT="$HERE/dist-oldmac"; LOG="$OUT/build.log"
rm -rf "$OUT"; mkdir -p "$OUT" "$HERE/reports"
[ -f "$MANAGED/Assembly-CSharp.dll" ] || { echo "Game files missing. Run ./get-game.sh first."; exit 1; }

echo "Getting the latest code"
git -C "$HERE" pull -q --rebase || true

echo "Compiling the plugin..."
dotnet build "$HERE/oldmac/OldMac.csproj" -c Release -p:CWDir=/workspaces/cw-game -o "$OUT/bin" 2>&1 | tee "$LOG"
CODE=${PIPESTATUS[0]}

# Extra signatures for Claude (input, item data entries, serializers)
dotnet run -c Release --project "$HERE/tools/ApiDump" -- "$MANAGED" "$HERE/reports/api-extra.txt" \
  '^(PlayerInput|IntEntry|OnOffEntry|IntRangeEntry|BoolEntry|BinarySerializer|BinaryDeserializer|ItemDataSyncer|ObjectDatabaseAsset.*|SingletonAsset.*)$' all >/dev/null 2>&1 || true

if [ "$CODE" -ne 0 ] || [ ! -f "$OUT/bin/FluidLoveOldMac.dll" ]; then
  echo; echo "BUILD FAILED. Sending the errors to the repo for Claude..."
  "$HERE/report.sh"; exit 1
fi

PKG="$OUT/FluidLoveOldMac"; mkdir -p "$PKG"
cp "$OUT/bin/FluidLoveOldMac.dll" "$HERE/oldmac/preview.png" "$PKG/"
(cd "$OUT" && zip -qr FluidLoveOldMac.zip FluidLoveOldMac)
ls -lh "$PKG"
"$HERE/report.sh"
echo; echo "SUCCESS: dist-oldmac/FluidLoveOldMac.zip is ready. Tell Claude: build done"
