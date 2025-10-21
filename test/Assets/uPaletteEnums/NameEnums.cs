using System;

namespace uPalette.Generated
{
public enum ColorTheme
    {
        Default,
    }

    public static class ColorThemeExtensions
    {
        public static string ToThemeId(this ColorTheme theme)
        {
            switch (theme)
            {
                case ColorTheme.Default:
                    return "77e5763a-2942-4db1-a380-dca78b8814ba";
                default:
                    throw new ArgumentOutOfRangeException(nameof(theme), theme, null);
            }
        }
    }

    public enum ColorEntry
    {
        Noun,
        Adjective,
        Verb,
    }

    public static class ColorEntryExtensions
    {
        public static string ToEntryId(this ColorEntry entry)
        {
            switch (entry)
            {
                case ColorEntry.Noun:
                    return "d8699eff-94e2-43ff-bf37-db29fa153608";
                case ColorEntry.Adjective:
                    return "b28f2111-121b-4bd1-bf33-d10c7709142c";
                case ColorEntry.Verb:
                    return "3d4a6bc4-4e0d-439f-9efc-0c40694d8c98";
                default:
                    throw new ArgumentOutOfRangeException(nameof(entry), entry, null);
            }
        }
    }

    public enum GradientTheme
    {
        Default,
    }

    public static class GradientThemeExtensions
    {
        public static string ToThemeId(this GradientTheme theme)
        {
            switch (theme)
            {
                case GradientTheme.Default:
                    return "5f706dd0-9f83-4dd2-a1b1-b760f5924c92";
                default:
                    throw new ArgumentOutOfRangeException(nameof(theme), theme, null);
            }
        }
    }

    public enum GradientEntry
    {
        Timer,
    }

    public static class GradientEntryExtensions
    {
        public static string ToEntryId(this GradientEntry entry)
        {
            switch (entry)
            {
                case GradientEntry.Timer:
                    return "14287626-0302-4373-a9a6-2ae5d017b6a1";
                default:
                    throw new ArgumentOutOfRangeException(nameof(entry), entry, null);
            }
        }
    }

    public enum CharacterStyleTheme
    {
        Default,
    }

    public static class CharacterStyleThemeExtensions
    {
        public static string ToThemeId(this CharacterStyleTheme theme)
        {
            switch (theme)
            {
                case CharacterStyleTheme.Default:
                    return "36820846-c4a3-4326-8f41-0649e1a561f8";
                default:
                    throw new ArgumentOutOfRangeException(nameof(theme), theme, null);
            }
        }
    }

    public enum CharacterStyleEntry
    {
    }

    public static class CharacterStyleEntryExtensions
    {
        public static string ToEntryId(this CharacterStyleEntry entry)
        {
            switch (entry)
            {
                default:
                    throw new ArgumentOutOfRangeException(nameof(entry), entry, null);
            }
        }
    }

    public enum CharacterStyleTMPTheme
    {
        Default,
    }

    public static class CharacterStyleTMPThemeExtensions
    {
        public static string ToThemeId(this CharacterStyleTMPTheme theme)
        {
            switch (theme)
            {
                case CharacterStyleTMPTheme.Default:
                    return "97b40c31-4821-4e7b-87ca-707436cbdfa5";
                default:
                    throw new ArgumentOutOfRangeException(nameof(theme), theme, null);
            }
        }
    }

    public enum CharacterStyleTMPEntry
    {
    }

    public static class CharacterStyleTMPEntryExtensions
    {
        public static string ToEntryId(this CharacterStyleTMPEntry entry)
        {
            switch (entry)
            {
                default:
                    throw new ArgumentOutOfRangeException(nameof(entry), entry, null);
            }
        }
    }
}
