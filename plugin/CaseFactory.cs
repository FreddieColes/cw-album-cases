using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ContentWarningShop;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FluidLove.AlbumCases
{
    // Builds the four items at runtime: clones a handheld game item as the base (so holding, dropping and
    // networking all work), hides its model, adds a textured jewel case and our click-to-play behaviour.
    internal static class CaseFactory
    {
        static readonly Assembly Asm = typeof(CaseFactory).Assembly;
        static readonly List<Object> Keep = new List<Object>();

        // Runtime-made assets get cleared by Unity's unused-asset cleanup on scene changes unless flagged
        static T Pin<T>(T o) where T : Object { o.hideFlags |= HideFlags.DontUnloadUnusedAsset; Keep.Add(o); return o; }

        public static void LogHealth(string when)
        {
            int dead = Keep.Count(o => o == null);
            AlbumCasesPlugin.Log($"Asset check ({when}): {Keep.Count - dead}/{Keep.Count} alive");
        }

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

            Albums.Mixer = template.itemObject.GetComponentInChildren<SoundPlayerItem>(true)?.sounds?
                .FirstOrDefault(s => s != null && s.settings != null)?.settings.mixerGroup;

            foreach (var album in Albums.All)
            {
                try { BuildOne(album, template, holder.transform); }
                catch (Exception e) { Debug.LogError($"[CWAlbum] {album.Key} failed: {e}"); }
            }
            return true;
        }

        static bool IsText(Renderer r) => r.GetComponent<TMP_Text>() != null;

        static void BuildOne(AlbumInfo album, Item template, Transform holder)
        {
            // --- audio
            album.Clips = Asm.GetManifestResourceNames()
                .Where(n => n.StartsWith(album.ClipPrefix) && n.EndsWith(".wav")).OrderBy(n => n)
                .Select(n => Pin(Wav.Load(n, ReadRes(n)))).ToArray();

            // --- textures
            var atlas = LoadTex(album.Key + "_Atlas.png", true);
            // CW shop icons are white line art on transparent; the shop screen only uses their shape
            var iconTex = LoadTex(album.Key + "_ShopIcon.png", false);

            // --- prefab
            var prefab = Object.Instantiate(template.itemObject, holder);
            prefab.name = "FluidLove_" + album.Key;

            var all = prefab.GetComponentsInChildren<Renderer>(true);
            var body = all.Where(r => !(r is ParticleSystemRenderer) && !IsText(r)).ToArray();
            int layer = body.Length > 0 ? body[0].gameObject.layer : prefab.layer;
            Material baseMat = body.Select(r => r.sharedMaterial).FirstOrDefault(m => m != null && !m.shader.name.Contains("TextMeshPro"));

            // Only the item's own body: skip long light beams etc. that stretch metres away
            var root = prefab.transform.position;
            var near = body.Where(r => r.bounds.size.magnitude < 1f && Vector3.Distance(r.bounds.center, root) < 0.6f).ToArray();
            Bounds b = new Bounds(root, Vector3.zero);
            for (int i = 0; i < near.Length; i++) { if (i == 0) b = near[i].bounds; else b.Encapsulate(near[i].bounds); }
            foreach (var r in all) r.enabled = false;
            foreach (var l in prefab.GetComponentsInChildren<Light>(true)) l.enabled = false;
            foreach (var old in prefab.GetComponentsInChildren<ItemInstanceBehaviour>(true)) Object.DestroyImmediate(old);
            prefab.AddComponent<AlbumCaseBehaviour>();

            const float size = 0.36f;                                // 1.8x the old 0.2m
            Vector3 handNudge = new Vector3(-0.06f, 0f, 0f);         // shift left so the hand holds the edge
            var caseGo = new GameObject("AlbumCase") { layer = layer };
            caseGo.transform.SetParent(prefab.transform, false);
            caseGo.transform.position = b.center;
            caseGo.transform.localPosition += handNudge;
            // The item points along +Z (away from the player), so turn the case round to show the cover to the holder
            caseGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            caseGo.AddComponent<MeshFilter>().sharedMesh = Pin(CaseMesh.Make(size, size, size * 0.08f));
            var mr = caseGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Pin(MakeMat(baseMat, atlas));
            caseGo.AddComponent<CaseMarker>();

            // Swap the flashlight's round collider for a flat box, so dropped cases lie flat instead of rolling
            var oldCols = prefab.GetComponentsInChildren<Collider>(true).Where(c => !c.isTrigger).ToArray();
            int colLayer = oldCols.Length > 0 ? oldCols[0].gameObject.layer : prefab.layer;
            foreach (var c in oldCols) Object.DestroyImmediate(c);
            var colGo = new GameObject("AlbumCaseCollider") { layer = colLayer };
            colGo.transform.SetParent(prefab.transform, false);
            colGo.transform.localPosition = caseGo.transform.localPosition;
            colGo.transform.localRotation = caseGo.transform.localRotation;
            colGo.AddComponent<BoxCollider>().size = new Vector3(size, size, Mathf.Max(size * 0.08f, 0.04f));

            // --- item
            var item = Pin(ScriptableObject.CreateInstance<Item>());
            item.name = album.Display;
            item.displayName = album.Display;
            item.persistentID = album.Guid;
            item.icon = Pin(Sprite.Create(iconTex, new Rect(0, 0, iconTex.width, iconTex.height), new Vector2(0.5f, 0.5f), 100f));
            item.icon.name = album.Key + "_Icon";
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
            AlbumCasesPlugin.Log($"{album.Display}: {album.Clips.Length} clips, atlas {atlas.width}x{atlas.height}, icon {iconTex.width}x{iconTex.height}, " +
                                 $"shader '{mr.sharedMaterial.shader.name}', layer '{LayerMask.LayerToName(layer)}', case {size:0.00}m at {caseGo.transform.localPosition}, " +
                                 $"replaced {oldCols.Length} colliders on layer '{LayerMask.LayerToName(colLayer)}', base renderers {near.Length} near / {body.Length} body / {all.Length} total");
        }

        static Material MakeMat(Material baseMat, Texture2D tex)
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            Material m = lit != null ? new Material(lit) : baseMat != null ? new Material(baseMat) : new Material(Shader.Find("Standard"));
            m.name = "AlbumCase_" + tex.name;
            foreach (var p in new[] { "_BaseMap", "_MainTex", "_BaseColorMap" }) if (m.HasProperty(p)) m.SetTexture(p, tex);
            foreach (var p in new[] { "_BaseColor", "_Color" }) if (m.HasProperty(p)) m.SetColor(p, Color.white);
            foreach (var p in new[] { "_BumpMap", "_EmissionMap", "_MetallicGlossMap", "_OcclusionMap" }) if (m.HasProperty(p)) m.SetTexture(p, null);
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
            m.DisableKeyword("_EMISSION"); m.DisableKeyword("_NORMALMAP"); m.DisableKeyword("_METALLICSPECGLOSSMAP");
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.7f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            return m;
        }

        static Texture2D LoadTex(string res, bool mips)
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, mips) { name = res };
            bool ok = ImageConversion.LoadImage(t, ReadRes(res), false);
            if (!ok) Debug.LogError("[CWAlbum] Could not decode " + res);
            t.wrapMode = TextureWrapMode.Clamp;
            return Pin(t);
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
