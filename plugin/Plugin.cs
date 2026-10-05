using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FluidLove.AlbumCases
{
    // vanillaCompatible false: everyone in the lobby needs the mod (new shop items).
    [ContentWarningPlugin("FluidLove.AlbumCases", "0.4.1", vanillaCompatible: false)]
    public class AlbumCasesPlugin
    {
        static bool built;

        static AlbumCasesPlugin()
        {
            Log("Loaded v0.4.1. Waiting for the item database...");
            TryBuild("plugin load");
            SceneManager.sceneLoaded += (s, m) =>
            {
                TryBuild("scene " + s.name);
                if (built) CaseFactory.LogHealth("scene " + s.name);
            };
        }

        static void TryBuild(string when)
        {
            if (built) return;
            try { built = CaseFactory.BuildAll(); }
            catch (Exception e) { built = true; Debug.LogError("[CWAlbum] Build failed (" + when + "): " + e); }
            if (built) Log("Album cases ready (" + when + ")");
        }

        internal static void Log(string msg) => Debug.Log("[CWAlbum] " + msg);
    }
}
