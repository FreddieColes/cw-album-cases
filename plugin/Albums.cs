using System.Collections.Generic;

namespace FluidLove.AlbumCases
{
    internal class AlbumInfo
    {
        public string Key, Display, ClipPrefix, Guid;
        public int Price;
        public SFX_Instance[] Sfx;
        public AlbumInfo(string key, string display, string prefix, string guid, int price)
        { Key = key; Display = display; ClipPrefix = prefix; Guid = guid; Price = price; }
    }

    internal static class Albums
    {
        public static readonly AlbumInfo[] All =
        {
            new AlbumInfo("ReadyForBusiness",  "Ready For Business",  "REA_", "6f1d2c10-5a1e-4c7e-9b1a-0f10e0000001", 40),
            new AlbumInfo("ManOfTheCloth",     "Man Of The Cloth",    "MAN_", "6f1d2c10-5a1e-4c7e-9b1a-0f10e0000002", 40),
            new AlbumInfo("BackwaterCrimes",   "Backwater Crimes",    "BAC_", "6f1d2c10-5a1e-4c7e-9b1a-0f10e0000003", 40),
            new AlbumInfo("PleasureIslandDLC", "Pleasure Island DLC", "PID_", "6f1d2c10-5a1e-4c7e-9b1a-0f10e0000004", 40),
        };

        static readonly Dictionary<string, AlbumInfo> byGuid = new Dictionary<string, AlbumInfo>();
        public static void Register(AlbumInfo a) => byGuid[a.Guid] = a;
        public static AlbumInfo For(Item item) =>
            item != null && item.persistentID != null && byGuid.TryGetValue(item.persistentID, out var a) ? a : null;
    }
}
