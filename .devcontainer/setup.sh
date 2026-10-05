#!/usr/bin/env bash
# One-off setup: system packages, SteamCMD, Unity Hub, ilspy tools. The game and Unity editor come later (get-game.sh).
set -euo pipefail
log(){ echo -e "\n=== $* ===\n"; }

log "System packages"
sudo dpkg --add-architecture i386
sudo apt-get update -y
sudo apt-get install -y --no-install-recommends \
  curl wget gpg ca-certificates xz-utils zip unzip p7zip-full cpio git rsync binutils jq \
  lib32gcc-s1 lib32stdc++6 \
  libgtk-3-0 libnss3 libasound2 libgbm1 libxss1 libxtst6 libsecret-1-0 gnome-keyring dbus-x11 \
  xvfb libglu1-mesa libgl1 libxcursor1 libxrandr2 libxinerama1 libxi6 libcanberra-gtk3-module \
  firefox-esr xdg-utils

log "libssl1.1 for Unity's compiler"
bash "$(dirname "$0")/../fix-libssl.sh" || true

log "SteamCMD"
if [ ! -x "$HOME/steamcmd/steamcmd.sh" ]; then
  mkdir -p "$HOME/steamcmd"
  curl -fsSL https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz | tar -xz -C "$HOME/steamcmd"
  "$HOME/steamcmd/steamcmd.sh" +quit || true   # first run self-updates
fi

log "Unity Hub (only used to sign in for the free licence)"
if ! command -v unityhub >/dev/null; then
  wget -qO - https://hub.unity3d.com/linux/keys/public | gpg --dearmor | sudo tee /usr/share/keyrings/Unity_Technologies_ApS.gpg >/dev/null
  echo "deb [signed-by=/usr/share/keyrings/Unity_Technologies_ApS.gpg] https://hub.unity3d.com/linux/repos/deb stable main" | sudo tee /etc/apt/sources.list.d/unityhub.list
  sudo apt-get update -y && sudo apt-get install -y unityhub
fi
xdg-settings set default-web-browser firefox-esr.desktop || true

chmod +x "$(dirname "$0")"/../*.sh || true
log "SETUP DONE. Next: ./get-game.sh"
