using Photon.Pun;
using UnityEngine;

namespace FluidLove.AlbumCases
{
    // Click while holding: pick a random clip. The choice goes into an IntEntry on the item's synced data,
    // so every player's copy of the case sees the change and plays the same clip.
    // Value = playCount * 64 + clipIndex, so the same clip twice in a row still counts as a new play.
    public class AlbumCaseBehaviour : ItemInstanceBehaviour
    {
        IntEntry playEntry;
        int lastSeen;
        AlbumInfo album;

        public override void ConfigItem(ItemInstanceData data, PhotonView playerView)
        {
            album = Albums.For(itemInstance != null ? itemInstance.item : null);
            if (!data.TryGetEntry<IntEntry>(out playEntry))
            {
                playEntry = new IntEntry();
                data.AddDataEntry(playEntry);
            }
            lastSeen = playEntry.value; // don't replay an old clip when the case is picked up
        }

        void Update()
        {
            if (album == null || playEntry == null || album.Sfx == null || album.Sfx.Length == 0) return;

            var me = Player.localPlayer;
            if (isHeldByMe && me != null && !me.HasLockedInput() && me.input.clickWasPressed)
            {
                int clip = Random.Range(0, album.Sfx.Length);
                playEntry.value = (playEntry.value / 64 + 1) * 64 + clip;
                playEntry.SetDirty();
            }

            if (playEntry.value != lastSeen)
            {
                lastSeen = playEntry.value;
                int idx = lastSeen % 64;
                if (idx < album.Sfx.Length) album.Sfx[idx].Play(transform.position, false, 1f, transform);
            }
        }
    }
}
