using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace FluidLove.OldMac
{
    // Host-only debug key: F8 spawns Ol' Mac a few metres in front of you.
    public class OldMacRunner : MonoBehaviour
    {
        static OldMacRunner instance;

        public static void Ensure()
        {
            if (instance != null) return;
            var go = new GameObject("FluidLove_OldMacRunner");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<OldMacRunner>();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.f8Key.wasPressedThisFrame) return;

            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) { OldMacPlugin.Log("F8: only the host can spawn Ol' Mac"); return; }
            if (OldMacFactory.Prefab == null) { OldMacPlugin.Log("F8: Ol' Mac isn't built yet (he's built when a dive level loads)"); return; }

            var cam = Camera.main;
            if (cam == null) { OldMacPlugin.Log("F8: no camera"); return; }
            Vector3 flatFwd = cam.transform.forward; flatFwd.y = 0; flatFwd.Normalize();
            Vector3 pos = cam.transform.position + flatFwd * 5f;
            if (NavMesh.SamplePosition(pos, out var hit, 5f, NavMesh.AllAreas)) pos = hit.position;
            else OldMacPlugin.Log("F8: no walkable ground found there, spawning anyway");

            PhotonNetwork.Instantiate(OldMacPlugin.PrefabName, pos, Quaternion.LookRotation(-flatFwd));
            OldMacPlugin.Log("F8: spawned Ol' Mac at " + pos);
        }
    }
}
