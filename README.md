# CW Album Cases

A Content Warning (Steam Workshop) mod by Fluid Love: four album jewel cases in the shop. Click one to play a random clip from that album, and everyone in the lobby hears it.

Albums: Ready For Business, Man Of The Cloth, Backwater Crimes, Pleasure Island DLC.

## Stage 1 (now): set up the Codespace and grab the game

1. Repo page → green **Code** button → **Codespaces** → **Create codespace on main**. Wait 10 to 15 minutes for setup to finish (it says SETUP DONE in `/workspaces/setup.log`).
2. In the terminal: `./get-game.sh`
   - Type your Steam username, password, then approve the login in the Steam app (or type the Steam Guard code).
   - It downloads Content Warning, finds its Unity version, installs that Unity plus Windows build support (a few GB, so it takes a while), then pushes a report to the repo.
3. Tell Claude: **game done**.

## Stage 2 (next): plugin + asset bundle + build

Claude writes the plugin and Unity builder once the report is in. Then you'll run `./start-hub.sh` once to add the free Unity licence, and `./build.sh`.

## What's where

- `assets/<Album>/Art` jewel case texture and shop icon (from album-blades)
- `assets/<Album>/Audio` 9 clips per album, 2.6s, already loud
- `get-game.sh` Steam download + Unity install
- `api-dump.sh` / `tools/ApiDump` lists the game's class signatures for Claude (no game code)
- `report.sh` sends logs to the repo

Game files live in `/workspaces/cw-game` and are never committed.
