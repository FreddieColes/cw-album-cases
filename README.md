# CW Album Cases

A Content Warning (Steam Workshop) mod by Fluid Love: four album jewel cases in the shop. Click one to play a random clip from that album, and everyone in the lobby hears it.

Albums: Ready For Business, Man Of The Cloth, Backwater Crimes, Pleasure Island DLC.

## Stage 1 (now): set up the Codespace and grab the game

1. Repo page → green **Code** button → **Codespaces** → **Create codespace on main**. Wait 10 to 15 minutes for setup to finish (it says SETUP DONE in `/workspaces/setup.log`).
2. In the terminal: `./get-game.sh`
   - Type your Steam username, password, then approve the login in the Steam app (or type the Steam Guard code).
   - It downloads Content Warning, finds its Unity version, installs that Unity plus Windows build support (a few GB, so it takes a while), then pushes a report to the repo.
3. Tell Claude: **game done**.

## Stage 2: build the plugin

No Unity needed after all: the jewel case is built in code and the art and clips are baked into the DLL.

1. In the Codespace terminal: `./build.sh`
2. If it fails it sends the errors to the repo automatically. Tell Claude: **report pushed**.
3. If it works you get `dist/FluidLoveAlbumCases.zip` (one DLL plus a preview image).

## Testing (needs a Windows PC with Content Warning)

1. Subscribe to Shop API on the Steam Workshop (required).
2. Unzip into `Content Warning/Plugins/` so you have `Plugins/FluidLoveAlbumCases/FluidLoveAlbumCases.dll`.
3. Launch, check the mod list, buy a case from the shop (Misc tab), click to play.
4. Logs: `%USERPROFILE%/AppData/LocalLow/Landfall Games/Content Warning/Player.log`, search for `[CWAlbum]`.

## What's where

- `plugin/` the C# mod (`CaseFactory.cs` builds the items, `AlbumCaseBehaviour.cs` click to play, synced)
- `assets/<Album>/` jewel case texture, shop icon and 9 clips per album (from album-blades)
- `build.sh` compile and package, `get-game.sh` Steam download, `report.sh` logs to repo
- `tools/ApiDump` lists game class signatures for Claude (no game code)

Game files live in `/workspaces/cw-game` and are never committed.
