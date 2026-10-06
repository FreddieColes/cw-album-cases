using Photon.Pun;
using UnityEngine;

namespace FluidLove.OldMac
{
    // Wraps the game's Photon prefab pool so "FluidLove_OldMac" can be network-spawned like a normal monster.
    public class OldMacPool : IPunPrefabPool
    {
        readonly IPunPrefabPool inner;
        OldMacPool(IPunPrefabPool inner) { this.inner = inner; }

        public static void Install()
        {
            var cur = PhotonNetwork.PrefabPool;
            if (cur is OldMacPool) return;
            PhotonNetwork.PrefabPool = new OldMacPool(cur);
            OldMacPlugin.Log("Prefab pool hooked (wrapping " + (cur != null ? cur.GetType().Name : "none") + ")");
        }

        public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
        {
            if (prefabId != OldMacPlugin.PrefabName) return inner.Instantiate(prefabId, position, rotation);
            var prefab = OldMacFactory.Prefab;
            if (prefab == null) { Debug.LogError("[OldMac] Asked to spawn Ol' Mac before he was built"); return null; }
            // Same as Photon's DefaultPool: hand back an inactive copy, Photon activates it after setting up the view
            prefab.SetActive(false);
            var go = Object.Instantiate(prefab, position, rotation);
            prefab.SetActive(true);
            go.name = OldMacPlugin.PrefabName;
            return go;
        }

        public void Destroy(GameObject gameObject)
        {
            if (gameObject != null && gameObject.name.StartsWith(OldMacPlugin.PrefabName)) Object.Destroy(gameObject);
            else inner.Destroy(gameObject);
        }
    }
}
