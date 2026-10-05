using Unity.Mathematics;
using Zorro.Settings;

namespace FluidLove.AlbumCases
{
    // Settings > Mods > "Album case volume" (each player sets their own)
    [ContentWarningSetting]
    public class AlbumCaseVolumeSetting : FloatSetting, IExposedSetting
    {
        public override void ApplyValue() { }
        protected override float GetDefaultValue() => 40;
        protected override float2 GetMinMaxValue() => new float2(0, 100);
        public SettingCategory GetSettingCategory() => SettingCategory.Mods;
        public string GetDisplayName() => "Album case volume";

        static AlbumCaseVolumeSetting cached;
        public static float Volume01
        {
            get
            {
                try { cached ??= GameHandler.Instance.SettingsHandler.GetSetting<AlbumCaseVolumeSetting>(); }
                catch { }
                return cached != null ? cached.Value / 100f : 0.4f;
            }
        }
    }
}
