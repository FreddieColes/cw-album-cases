using System;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FluidLove.OldMac
{
    // Everyone in the lobby needs this (new networked monster).
    [ContentWarningPlugin("FluidLove.OldMac", "0.1.0", vanillaCompatible: false)]
    public class OldMacPlugin
    {
        // TEST MODE: Ol' Mac replaces every other monster in dives. Set false for release.
        public const bool TestMode = true;
        public const string PrefabName = "FluidLove_OldMac";

        static OldMacPlugin()
        {
            Log("Loaded v0.1.0 (test mode " + (TestMode ? "ON" : "off") + "). Host: press F8 to spawn Ol' Mac.");
            OldMacPool.Install();
            OldMacRunner.Ensure();
            SceneManager.sceneLoaded += (s, m) =>
            {
                try { OldMacPool.Install(); OldMacRunner.Ensure(); OldMacFactory.OnSceneLoaded(s); }
                catch (Exception e) { Debug.LogError("[OldMac] Scene setup failed: " + e); }
            };
        }

        internal static void Log(string msg) => Debug.Log("[OldMac] " + msg);
    }
}
