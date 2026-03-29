using Godot;
using QuestceSpire.Core;

namespace QuestceSpire.UI;

/// <summary>
/// Centralized design tokens for the overlay UI.
/// All colors, font sizes, spacing, and radii are defined here.
/// </summary>
public static class OverlayTheme
{
    // ── Colors: Backgrounds ──────────────────────────────────────

    public static readonly Color BgPanel = new(0.034f, 0.057f, 0.11f, 0.97f);
    public static readonly Color BgEntry = new(0.05f, 0.07f, 0.13f, 0.55f);
    public static readonly Color BgEntryHover = new(0.08f, 0.10f, 0.18f, 0.75f);
    public static readonly Color BgEntryBest = new(0.831f, 0.714f, 0.357f, 0.10f);
    public static readonly Color BgEntryBestHover = new(0.831f, 0.714f, 0.357f, 0.15f);
    public static readonly Color BgEntrySTierHover = new(0.831f, 0.714f, 0.357f, 0.18f);
    public static readonly Color BgChip = new(0.02f, 0.03f, 0.07f, 0.7f);
    public static readonly Color BgThumbnail = new(0.02f, 0.02f, 0.04f, 1f);
    public static readonly Color BgSkipBadge = new(0.2f, 0.15f, 0.3f);
    public static readonly Color BgScoreBarEmpty = new(0.08f, 0.08f, 0.12f, 0.3f);

    // ── Colors: Borders & Chrome ─────────────────────────────────

    public static readonly Color Border = new(0.624f, 0.490f, 0.322f);
    public static readonly Color Outline = new(0.02f, 0.02f, 0.04f);
    public static readonly Color Shadow = new(0f, 0f, 0f, 0.6f);

    // ── Colors: Text ─────────────────────────────────────────────

    public static readonly Color TextHeader = new(0.92f, 0.78f, 0.35f);
    public static readonly Color TextAccent = new(0.831f, 0.714f, 0.357f);
    public static readonly Color TextBody = new(0.92f, 0.88f, 0.78f);      // Cream
    public static readonly Color TextSub = new(0.580f, 0.545f, 0.404f);
    public static readonly Color TextNotes = new(0.72f, 0.68f, 0.6f);

    // ── Colors: Semantic ─────────────────────────────────────────

    public static readonly Color Positive = new(0.3f, 0.8f, 0.4f);
    public static readonly Color Negative = new(0.9f, 0.35f, 0.3f);
    public static readonly Color Warning = new(1f, 0.6f, 0.3f);            // Expensive/orange
    public static readonly Color Info = new(0.529f, 0.808f, 0.922f);       // Aqua
    public static readonly Color Skip = new(0.557f, 0.212f, 0.882f);
    public static readonly Color SkipSub = new(0.6f, 0.6f, 0.8f);
    public static readonly Color Hover = new(0.1f, 0.12f, 0.2f, 0.8f);

    // ── Colors: Card Types (canonical — use these everywhere) ────

    public static readonly Color CardAttack = new(0.9f, 0.40f, 0.33f);
    public static readonly Color CardSkill = new(0.40f, 0.65f, 0.95f);
    public static readonly Color CardPower = new(0.95f, 0.85f, 0.35f);

    public static Color GetCardTypeColor(string type) => type?.ToLowerInvariant() switch
    {
        "attack" => CardAttack,
        "skill" => CardSkill,
        "power" => CardPower,
        _ => TextBody
    };

    // ── Colors: Tier Grades ──────────────────────────────────────

    public static Color GetTierColor(TierGrade grade) => grade switch
    {
        TierGrade.S => new Color(1f, 0.84f, 0f),          // Gold
        TierGrade.A => new Color(0.2f, 0.8f, 0.2f),       // Emerald green
        TierGrade.B => new Color(0.3f, 0.71f, 0.66f),     // Teal — distinct from skill blue
        TierGrade.C => new Color(0.6f, 0.6f, 0.6f),       // Grey
        TierGrade.D => new Color(0.65f, 0.47f, 0.35f),    // Tan brown — distinct from warning orange
        TierGrade.F => new Color(0.9f, 0.2f, 0.2f),       // Red
        _ => new Color(0.6f, 0.6f, 0.6f),
    };

    public static Color GetTierTextColor(TierGrade grade) => grade switch
    {
        TierGrade.S or TierGrade.A => new Color(0.05f, 0.05f, 0.05f),
        _ => new Color(0.95f, 0.95f, 0.95f),
    };

    // ── Colors: Archetype bars ───────────────────────────────────

    public static readonly Color[] ArchColors =
    {
        new(0.4f, 0.8f, 0.95f),   // cyan
        new(0.95f, 0.6f, 0.3f),   // orange
        new(0.7f, 0.5f, 0.95f),   // purple
        new(0.3f, 0.9f, 0.5f),    // green
    };

    // ── Font Sizes ───────────────────────────────────────────────

    public const int FontTitle = 22;        // App title only (Spectral Bold)
    public const int FontH1 = 18;           // Section headers
    public const int FontH2 = 16;           // Collapsible headers, card/relic names
    public const int FontBody = 14;         // Meta lines, tooltips, body text
    public const int FontSmall = 13;        // Stat rows, settings, secondary info
    public const int FontCaption = 12;      // Patch badges, pile counts, debug
    public const int FontBadgeLarge = 20;   // Single-char grade badge
    public const int FontBadgeSmall = 17;   // Multi-char grade badge (A+, B-)
    public const int FontSkipBadge = 22;    // Skip em-dash

    // ── Spacing (4px grid) ───────────────────────────────────────

    public const int SpaceXS = 2;           // Tight inner (score bar segments)
    public const int SpaceSM = 4;           // Closely related items
    public const int SpaceMD = 8;           // Standard separation
    public const int SpaceLG = 12;          // Between entries, sections
    public const int SpaceXL = 16;          // Major section gaps
    public const int SpaceXXL = 20;         // Panel content margins

    // ── Border Radii ─────────────────────────────────────────────

    public const int RadiusSM = 6;          // Decision entries, small panels
    public const int RadiusMD = 10;         // Card/relic entries, badges
    public const int RadiusLG = 16;         // Chips, pills
    public const int RadiusPanel = 18;      // Main panel corners

    // ── Opacity Tokens ───────────────────────────────────────────

    public const float OpBorderSubtle = 0.4f;
    public const float OpBorderAccent = 0.9f;
    public const float OpBorderNormal = 0.7f;
    public const float OpShadowNormal = 0.4f;
    public const float OpShadowAccent = 0.3f;
    public const float OpScoreBarFill = 0.7f;
    public const float OpChipBorder = 0.3f;
    public const float OpChipBg = 0.12f;

    // ── Sizes ────────────────────────────────────────────────────

    public const float ThumbnailSize = 52f;
    public const float BadgeSize = 34f;
    public const float BadgeInnerDefault = 30f;
    public const float BadgeInnerWide = 38f;
    public const float GoldIconSize = 12f;
    public const float ScoreBarHeight = 4f;     // Was 3px, increased for visibility
    public const float TypeBarHeight = 8f;
}
