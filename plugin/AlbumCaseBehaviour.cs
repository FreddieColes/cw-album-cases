using Photon.Pun;
using UnityEngine;

namespace FluidLove.AlbumCases
{
    // Click while holding: if a clip is playing, stop it; otherwise play a random clip.
    // The holder writes the choice into an IntEntry on the item's synced data, and every player's
    // copy of the case reacts to the change, so the whole lobby hears (or stops) the same thing.
    // Value = actionCount * 64 + clipIndex, clipIndex 63 = stop.
    public class AlbumCaseBehaviour : ItemInstanceBehaviour
    {
        const int Stop = 63;
        IntEntry playEntry;
        int lastSeen;
        AlbumInfo album;
        AudioSource source;
        bool logged;

        public override void ConfigItem(ItemInstanceData data, PhotonView playerView)
        {
            album = Albums.For(itemInstance != null ? itemInstance.item : null);
            if (!data.TryGetEntry<IntEntry>(out playEntry))
            {
                playEntry = new IntEntry();
                data.AddDataEntry(playEntry);
            }
            lastSeen = playEntry.i; // don't replay an old clip when the case is picked up

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
                var mr = GetComponentInChildren<CaseMarker>(true);
                AlbumCasesPlugin.Log($"Case spawned: album={(album != null ? album.Key : "NONE")}, caseVisible={(mr != null && mr.Ok())}, layer={LayerMask.LayerToName(mr != null ? mr.gameObject.layer : gameObject.layer)}");
            }
        }

        void Update()
        {
            if (album == null || playEntry == null || source == null || album.Clips == null || album.Clips.Length == 0) return;

            var me = Player.localPlayer;
            if (isHeldByMe && me != null && !me.HasLockedInput() && me.input.clickWasPressed)
            {
                int next = source.isPlaying ? Stop : Random.Range(0, album.Clips.Length);
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
                    source.clip = album.Clips[idx];
                    source.volume = AlbumCaseVolumeSetting.Volume01;
                    source.Play();
                    // Let monsters hear it
                    try { SFX_Player.instance?.PlayNoise(transform.position, 20f, 1); } catch { }
                }
            }
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
