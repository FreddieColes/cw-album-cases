#!/usr/bin/env bash
# Lists the game's monster, AI, attack and spawning class signatures (no game code) for the Ol' Mac mod.
HERE="$(cd "$(dirname "$0")" && pwd)"
MANAGED="/workspaces/cw-game/Content Warning_Data/Managed"
git -C "$HERE" pull -q --rebase || true
mkdir -p "$HERE/reports"
dotnet run -c Release --project "$HERE/tools/ApiDump" -- "$MANAGED" "$HERE/reports/api-monsters.txt" \
  '^(Bot|Bot_Chaser|Bot_Zombie|Bot_Knifo|Bot_RagdollCharacter|Bot_Nav_Navmesh|Bot_SimpleMovement|Bot_LookY|SyncData|BotHandler|Attack_Punch|Attack_Melee|Attack_Stab|Attack_Shove|Attack_Animation|MonsterSpawner|RoundSpawner|Monster|MonsterWithWeight|MonsterSyncer|MonsterAnimationHandler|MonsterAnimationValues|BudgetCost|IBudgetCost|DeleteMonster|CurseOfMonsterSpawn|MonsterContentProvider|MonsterContentEvent|RagdollHandler|PlayerRagdoll|Bodypart|Rig|RigCreator|PhotonNetwork|PhotonView|Level|LevelHandler|SurfaceNetworkHandler|ContentProvider|ContentEvent|ContentEventIDMapper)$' all \
  && echo "Monster report written" || echo "Monster report failed"
cd "$HERE" && git add -f reports && git commit -qm "Monster report" && git push -q && echo "Report sent to the repo."
