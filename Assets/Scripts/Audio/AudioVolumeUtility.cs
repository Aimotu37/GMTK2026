using System;

namespace GMTK.Audio
{
    public static class AudioVolumeUtility
    {
        public const float DefaultMuteFloorDb = -80f;

        public static float NormalizedToDecibels(
            float normalized,
            float muteFloorDb = DefaultMuteFloorDb)
        {
            float clamped = Math.Min(1f, Math.Max(0f, normalized));
            if (clamped <= 0.0001f)
            {
                return muteFloorDb;
            }

            return Math.Max(muteFloorDb, 20f * (float)Math.Log10(clamped));
        }
    }
}
