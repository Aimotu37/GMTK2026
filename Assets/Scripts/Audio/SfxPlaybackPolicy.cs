using System;
using System.Collections.Generic;

namespace GMTK.Audio
{
    public enum SfxCategory
    {
        UI = 0,
        Interaction = 1,
        Result = 2
    }

    public enum SfxRetriggerBehavior
    {
        Restart,
        Ignore
    }

    public enum SfxTriggerAction
    {
        Play,
        Restart,
        Ignore
    }

    public sealed class SfxProfile
    {
        public SfxProfile(
            string key,
            SfxCategory category,
            float gain,
            float cooldownSeconds,
            int maxSameKeyVoices,
            SfxRetriggerBehavior retriggerBehavior)
        {
            Key = key ?? string.Empty;
            Category = category;
            Gain = gain;
            CooldownSeconds = Math.Max(0f, cooldownSeconds);
            MaxSameKeyVoices = Math.Max(1, maxSameKeyVoices);
            RetriggerBehavior = retriggerBehavior;
        }

        public string Key { get; }
        public SfxCategory Category { get; }
        public float Gain { get; }
        public float CooldownSeconds { get; }
        public int MaxSameKeyVoices { get; }
        public SfxRetriggerBehavior RetriggerBehavior { get; }
    }

    public static class SfxPlaybackPolicy
    {
        private static readonly Dictionary<string, SfxProfile> Profiles =
            new Dictionary<string, SfxProfile>(StringComparer.Ordinal)
            {
                ["sfx_01_07_09"] = CreateUiProfile("sfx_01_07_09"),
                ["sfx_02_11_14"] = CreateUiProfile("sfx_02_11_14"),
                ["sfx_03_08"] = CreateInteractionProfile("sfx_03_08"),
                ["sfx_04_05_06"] = CreateInteractionProfile("sfx_04_05_06"),
                ["sfx_10_12"] = CreateResultProfile("sfx_10_12"),
                ["sfx_13"] = CreateResultProfile("sfx_13")
            };

        private static readonly SfxProfile FallbackProfile = new SfxProfile(
            string.Empty,
            SfxCategory.UI,
            1f,
            0f,
            1,
            SfxRetriggerBehavior.Ignore);

        public static bool TryGetProfile(string key, out SfxProfile profile)
        {
            if (string.IsNullOrEmpty(key))
            {
                profile = null;
                return false;
            }

            return Profiles.TryGetValue(key, out profile);
        }

        public static SfxProfile GetProfileOrFallback(string key)
        {
            return TryGetProfile(key, out SfxProfile profile)
                ? profile
                : FallbackProfile;
        }

        public static SfxTriggerAction DecideSameKeyTrigger(
            SfxProfile profile,
            bool isPlaying,
            float secondsSinceLastTrigger)
        {
            if (!isPlaying)
            {
                return SfxTriggerAction.Play;
            }

            if (profile == null)
            {
                return SfxTriggerAction.Ignore;
            }

            if (secondsSinceLastTrigger < profile.CooldownSeconds)
            {
                return SfxTriggerAction.Ignore;
            }

            return profile.RetriggerBehavior == SfxRetriggerBehavior.Restart
                ? SfxTriggerAction.Restart
                : SfxTriggerAction.Ignore;
        }

        public static bool CanReclaim(
            SfxCategory activeCategory,
            bool activeLooping,
            SfxCategory incomingCategory)
        {
            return !activeLooping && incomingCategory > activeCategory;
        }

        private static SfxProfile CreateUiProfile(string key)
        {
            return new SfxProfile(key, SfxCategory.UI, 1f, 0.07f, 1, SfxRetriggerBehavior.Restart);
        }

        private static SfxProfile CreateInteractionProfile(string key)
        {
            return new SfxProfile(
                key,
                SfxCategory.Interaction,
                1f,
                0.10f,
                1,
                SfxRetriggerBehavior.Restart);
        }

        private static SfxProfile CreateResultProfile(string key)
        {
            return new SfxProfile(key, SfxCategory.Result, 1f, 0f, 1, SfxRetriggerBehavior.Ignore);
        }
    }
}
