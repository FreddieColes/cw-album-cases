using Photon.Pun;
using UnityEngine;

namespace FluidLove.AlbumCases
{
    // Click while holding: if a clip is playing, stop it; otherwise play a random clip (never the same one twice in a row).
    // The holder writes the choice into an IntEntry on the item's synced data, and every player's
    // copy of the case reacts to the change, so the whole lobby hears (or stops) the same thing.
    // Value = actionCount * 64 + clipIndex, clipIndex 63 = stop.
    public class AlbumCaseBehaviour : ItemInstanceBehaviour
    {
        const int Stop = 63;
        IntEntry playEntry;
        int lastSeen;
        int lastClip = -1;
        AlbumInfo album;
        AudioSource source;
        bool logged;

        public override void ConfigItem(ItemInstanceData data, PhotonView playerView)
        {
            album = Albums.For(itemInstance != null ? itemInstance.item : null);
            if (!data.TryGetEntry<IntEntry>(out playEntry)) { playEntry = new IntEntry(); data.AddDataEntry(playEntry); }
            lastSeen = playEntry.i; // don't replay an old clip when the case is picked up
            int prev = lastSeen % 64;
            if (prev != Stop && lastSeen != 0) lastClip = prev;

            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 2f;
            source.maxDistance = 30f;
            source.dopplerLevel = 0f;
            if (Albums.Mixer != null) source.outputAudioMixerGroup = Albums.Mixer;

            if (!logged)
            {
                logged = true;
                var marker = GetComponentInChildren<CaseMarker>(true);
                AlbumCasesPlugin.Log($"Case spawned: album={(album != null ? album.Key : "NONE")}, caseVisible={(marker != null && marker.Ok())}, " +
                                     $"layer={LayerMask.LayerToName(marker != null ? marker.gameObject.layer : gameObject.layer)}, held={isHeld}");
            }
        }

        void Update()
        {
            if (album == null || playEntry == null || source == null || album.Clips == null || album.Clips.Length == 0) return;

            var me = Player.localPlayer;
            if (isHeldByMe && me != null && !me.HasLockedInput() && me.input.clickWasPressed)
            {
                int next = source.isPlaying ? Stop : PickClip();
                playEntry.i = (playEntry.i / 64 + 1) * 64 + next;
                playEntry.SetDirty();
            }

            if (playEntry.i != lastSeen)
            {
                lastSeen = playEntry.i;
                int idx = lastSeen % 64;
                source.Stop();
                if (idx != Stop && idx < album.Clips.Length)
                {
                    lastClip = idx;
                    source.clip = album.Clips[idx];
                    source.volume = AlbumCaseVolumeSetting.Volume01;
                    source.Play();
                    // Let monsters hear it
                    try { SFX_Player.instance?.PlayNoise(transform.position, 20f, 1); } catch { }
                }
            }
        }

        // Random clip, never the same as the last one played
        int PickClip()
        {
            int n = album.Clips.Length;
            if (n <= 1) return 0;
            if (lastClip < 0 || lastClip >= n) return Random.Range(0, n);
            int r = Random.Range(0, n - 1);
            return r >= lastClip ? r + 1 : r;
        }
    }

    // Tags the jewel case mesh so we can check it's still alive in the logs
    public class CaseMarker : MonoBehaviour
    {
        public bool Ok()
        {
            var mf = GetComponent<MeshFilter>(); var r = GetComponent<MeshRenderer>();
            return mf != null && mf.sharedMesh != null && r != null && r.enabled && r.sharedMaterial != null;
        }
    }
}
