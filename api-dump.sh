#!/usr/bin/env bash
# Lists the game's item, shop and audio classes (names and signatures only, no game code) so Claude can write the plugin.
HERE="$(cd "$(dirname "$0")" && pwd)"
MANAGED="/workspaces/cw-game/Content Warning_Data/Managed"
mkdir -p "$HERE/reports"
dotnet run -c Release --project "$HERE/tools/ApiDump" -- "$MANAGED" "$HERE/reports/api.txt" \
  || echo "API dump failed" > "$HERE/reports/api.txt"
"$HERE/report.sh"
