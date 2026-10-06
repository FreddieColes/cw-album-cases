using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FluidLove.OldMac
{
    // Builds Ol' Mac by cloning a chasing monster from the level's spawn list (its AI, attacks and networking),
    // hiding its body and adding the sprite visual.
    internal static class OldMacFactory
    {
        public static GameObject Prefab;
        public static Texture2D[] Walk, Stand;
        static readonly Assembly Asm = typeof(OldMacFactory).Assembly;
        static Transform holder;

        public static void OnSceneLoaded(Scene scene)
        {
            var spawners = Object.FindObjectsByType<RoundSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (spawners.Length == 0) return;
            OldMacPlugin.Log($"Level '{scene.name}' has {spawners.Length} monster spawner(s)");

            if (Prefab == null) Build(spawners[0].possibleSpawns);
            if (Prefab == null) return;

            foreach (var rs in spawners)
            {
                if (OldMacPlugin.TestMode) rs.possibleSpawns = new[] { Prefab };
                else if (!rs.possibleSpawns.Contains(Prefab)) rs.possibleSpawns = rs.possibleSpawns.Concat(new[] { Prefab }).ToArray();
            }
            OldMacPlugin.Log(OldMacPlugin.TestMode ? "Test mode: Ol' Mac is the only monster this dive" : "Ol' Mac added to the spawn list");
        }

        static void Build(GameObject[] spawns)
        {
            if (spawns == null || spawns.Length == 0) { OldMacPlugin.Log("Spawn list empty"); return; }
            foreach (var s in spawns.Where(s => s != null))
            {
                var types = string.Join(",", s.GetComponentsInChildren<MonoBehaviour>(true).Select(m => m != null ? m.GetType().Name : "")
                    .Where(n => n.StartsWith("Bot_") || n.StartsWith("Attack")).Distinct());
                OldMacPlugin.Log($"  spawnable: {s.name} [{types}]");
            }

            var baseMonster =
                spawns.FirstOrDefault(s => s && s.GetComponentInChildren<Bot_Zombie>(true) && s.GetComponentInChildren<Attack_Punch>(true)) ??
                spawns.FirstOrDefault(s => s && s.GetComponentInChildren<Bot_Chaser>(true)) ??
                spawns.FirstOrDefault(s => s && s.GetComponentInChildren<Bot_Zombie>(true));
            if (baseMonster == null) { OldMacPlugin.Log("No chasing monster found to base Ol' Mac on"); return; }
            OldMacPlugin.Log("Basing Ol' Mac on: " + baseMonster.name);

            Walk = Enumerable.Range(1, 5).Select(i => LoadTex($"walk_{i}.png")).ToArray();
            Stand = Enumerable.Range(1, 6).Select(i => LoadTex($"stand_{i}.png")).ToArray();

            var h = new GameObject("FluidLove_OldMacPrefab");
            h.SetActive(false); // children keep activeSelf = true but never wake up here
            Object.DontDestroyOnLoad(h);
            holder = h.transform;

            var p = Object.Instantiate(baseMonster, holder);
            p.name = OldMacPlugin.PrefabName;
            int hidden = 0;
            foreach (var r in p.GetComponentsInChildren<Renderer>(true)) { r.enabled = false; hidden++; }
            p.AddComponent<OldMacVisual>();
            Prefab = p;
            OldMacPlugin.Log($"Ol' Mac built ({hidden} body renderers hidden, {Walk.Length} walk + {Stand.Length} stand frames)");
        }

        static Texture2D LoadTex(string res)
        {
            using var s = Asm.GetManifestResourceStream(res) ?? throw new FileNotFoundException("Missing " + res);
            using var ms = new MemoryStream(); s.CopyTo(ms);
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, true) { name = res, wrapMode = TextureWrapMode.Clamp };
            if (!ImageConversion.LoadImage(t, ms.ToArray(), false)) Debug.LogError("[OldMac] Could not decode " + res);
            t.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            return t;
        }
    }
}
