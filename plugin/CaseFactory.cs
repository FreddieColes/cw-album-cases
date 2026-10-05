using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ContentWarningShop;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FluidLove.AlbumCases
{
    // Builds the four items at runtime: clones a handheld game item as the base (so holding, dropping and
    // networking all work), hides its model, adds a textured jewel case and our click-to-play behaviour.
    internal static class CaseFactory
    {
        static readonly Assembly Asm = typeof(CaseFactory).Assembly;

        public static bool BuildAll()
        {
            var items = Resources.FindObjectsOfTypeAll<Item>();
            if (items.Length == 0) return false;

            Item template =
                items.FirstOrDefault(i => i.itemObject && i.itemObject.GetComponentInChildren<SoundPlayerItem>(true)) ??
                items.FirstOrDefault(i => i.itemObject && i.itemObject.GetComponentInChildren<Flashlight>(true));
            if (template == null) { AlbumCasesPlugin.Log("No template item found yet"); return false; }
            AlbumCasesPlugin.Log($"Using '{template.name}' as the base item ({items.Length} items loaded)");

            var holder = new GameObject("FluidLove_AlbumCasePrefabs");
            holder.SetActive(false); // children stay 'active' themselves, so instantiated copies are active
            Object.DontDestroyOnLoad(holder);

            SFX_Settings baseSettings = template.itemObject.GetComponentInChildren<SoundPlayerItem>(true)?.sounds?
                .FirstOrDefault(s => s != null)?.settings;

            foreach (var album in Albums.All)
            {
                try { BuildOne(album, template, holder.transform, baseSettings); }
                catch (Exception e) { Debug.LogError($"[CWAlbum] {album.Key} failed: {e}"); }
            }
            return true;
        }

        static void BuildOne(AlbumInfo album, Item template, Transform holder, SFX_Settings baseSettings)
        {
            // --- audio
            var clipNames = Asm.GetManifestResourceNames().Where(n => n.StartsWith(album.ClipPrefix) && n.EndsWith(".wav")).OrderBy(n => n).ToList();
            album.Sfx = clipNames.Select(n => MakeSfx(Wav.Load(n, ReadRes(n)), baseSettings)).ToArray();

            // --- textures
            var atlas = LoadTex(album.Key + "_Atlas.png");
            var iconTex = LoadTex(album.Key + "_Icon.png");

            // --- prefab
            var prefab = Object.Instantiate(template.itemObject, holder);
            prefab.name = "FluidLove_" + album.Key;

            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            Material baseMat = renderers.OfType<MeshRenderer>().Select(r => r.sharedMaterial).FirstOrDefault(m => m != null);
            int layer = renderers.Length > 0 ? renderers[0].gameObject.layer : prefab.layer;
            Bounds b = new Bounds(prefab.transform.position, Vector3.one * 0.15f);
            bool first = true;
            foreach (var r in renderers)
            {
                if (r is ParticleSystemRenderer) continue;
                if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
                r.enabled = false;
            }
            foreach (var l in prefab.GetComponentsInChildren<Light>(true)) l.enabled = false;
            foreach (var old in prefab.GetComponentsInChildren<ItemInstanceBehaviour>(true)) Object.DestroyImmediate(old);
            prefab.AddComponent<AlbumCaseBehaviour>();

            float size = Mathf.Clamp(Mathf.Max(b.size.x, b.size.y, b.size.z), 0.14f, 0.3f);
            var caseGo = new GameObject("AlbumCase") { layer = layer };
            caseGo.transform.SetParent(prefab.transform, false);
            caseGo.transform.position = b.center;
            caseGo.AddComponent<MeshFilter>().sharedMesh = CaseMesh.Make(size, size, size * 0.08f);
            var mr = caseGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MakeMat(baseMat, atlas);

            // --- item
            var item = ScriptableObject.CreateInstance<Item>();
            item.name = album.Display;
            item.displayName = album.Display;
            item.persistentID = album.Guid;
            item.icon = Sprite.Create(iconTex, new Rect(0, 0, iconTex.width, iconTex.height), new Vector2(0.5f, 0.5f));
            item.itemObject = prefab;
            item.itemType = Item.ItemType.Tool;
            item.purchasable = true;
            item.Category = ShopItemCategory.Misc;
            item.price = album.Price;
            item.quantity = 1;
            item.spawnable = false;
            item.mass = template.mass;
            item.groundSizeMultiplier = template.groundSizeMultiplier;
            item.groundMassMultiplier = template.groundMassMultiplier;
            item.holdPos = template.holdPos;
            item.holdRotation = template.holdRotation;
            item.useAlternativeHoldingPos = template.useAlternativeHoldingPos;
            item.alternativeHoldPos = template.alternativeHoldPos;
            item.useAlternativeHoldingRot = template.useAlternativeHoldingRot;
            item.alternativeHoldRot = template.alternativeHoldRot;
            item.content = template.content;
            item.Tooltips = new List<ItemKeyTooltip>();

            Albums.Register(album);
            Shop.RegisterItem(item);
            AlbumCasesPlugin.Log($"{album.Display}: {album.Sfx.Length} clips, case {size:0.00}m, registered");
        }

        static SFX_Instance MakeSfx(AudioClip clip, SFX_Settings src)
        {
            var sfx = ScriptableObject.CreateInstance<SFX_Instance>();
            sfx.name = clip.name;
            sfx.clips = new[] { clip };
            var s = new SFX_Settings();
            if (src != null)
            {
                s.occlusion = src.occlusion; s.reflections = src.reflections; s.transmission = src.transmission;
                s.obstructability = src.obstructability; s.dopplerLevel = src.dopplerLevel; s.mixerGroup = src.mixerGroup;
                s.nonSpatializedForLocalPlayer = src.nonSpatializedForLocalPlayer;
            }
            s.spatialize = true;
            s.spatialBlend = 1f;
            s.volume = 1f; s.volume_Variation = 0f;
            s.pitch = 1f; s.pitch_Variation = 0f;
            s.range = 45f; s.minRange = 4f;
            s.noiseDistance = 25;   // monsters can hear it
            s.cooldown = 0f;
            s.maxInstances = 3;
            sfx.settings = s;
            return sfx;
        }

        static Material MakeMat(Material baseMat, Texture2D tex)
        {
            var shader = baseMat != null ? baseMat.shader : Shader.Find("Universal Render Pipeline/Lit");
            var m = baseMat != null ? new Material(baseMat) : new Material(shader);
            foreach (var p in new[] { "_BaseMap", "_MainTex", "_BaseColorMap" }) if (m.HasProperty(p)) m.SetTexture(p, tex);
            foreach (var p in new[] { "_BaseColor", "_Color" }) if (m.HasProperty(p)) m.SetColor(p, Color.white);
            foreach (var p in new[] { "_BumpMap", "_EmissionMap", "_MetallicGlossMap", "_OcclusionMap" }) if (m.HasProperty(p)) m.SetTexture(p, null);
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
            m.DisableKeyword("_EMISSION"); m.DisableKeyword("_NORMALMAP"); m.DisableKeyword("_METALLICSPECGLOSSMAP");
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.7f);
            return m;
        }

        static Texture2D LoadTex(string res)
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, true) { name = res };
            ImageConversion.LoadImage(t, ReadRes(res), false);
            return t;
        }

        static byte[] ReadRes(string name)
        {
            using var s = Asm.GetManifestResourceStream(name) ?? throw new FileNotFoundException("Missing embedded file " + name);
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
