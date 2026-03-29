using Godot;
using QuestceSpire.Core;

namespace QuestceSpire.UI;

/// <summary>
/// Factory methods for common StyleBoxFlat patterns.
/// Eliminates repeated inline style construction across UI files.
/// </summary>
public static class OverlayStyles
{
    // ── Main Panel ───────────────────────────────────────────────

    public static StyleBoxFlat CreatePanelStyle()
    {
        var sb = new StyleBoxFlat();
        sb.BgColor = OverlayTheme.BgPanel;
        sb.BorderWidthTop = 3;
        sb.BorderWidthLeft = 3;
        sb.BorderWidthRight = 1;
        sb.BorderWidthBottom = 1;
        sb.BorderColor = OverlayTheme.Border;
        sb.CornerRadiusTopLeft = 0;
        sb.CornerRadiusTopRight = OverlayTheme.RadiusPanel;
        sb.CornerRadiusBottomLeft = OverlayTheme.RadiusPanel;
        sb.CornerRadiusBottomRight = 0;
        sb.ContentMarginLeft = sb.ContentMarginRight = OverlayTheme.SpaceXXL;
        sb.ContentMarginTop = sb.ContentMarginBottom = OverlayTheme.SpaceXXL;
        sb.ShadowSize = 16;
        sb.ShadowColor = OverlayTheme.Shadow;
        return sb;
    }

    // ── Entry Panels ─────────────────────────────────────────────

    public static StyleBoxFlat CreateEntryStyle()
    {
        var sb = new StyleBoxFlat();
        sb.BgColor = OverlayTheme.BgEntry;
        SetAllCornerRadius(sb, OverlayTheme.RadiusMD);
        sb.ContentMarginLeft = sb.ContentMarginRight = 14f;
        sb.ContentMarginTop = sb.ContentMarginBottom = 8f;
        return sb;
    }

    public static StyleBoxFlat CreateEntryHoverStyle()
    {
        var sb = CreateEntryStyle();
        sb.BgColor = OverlayTheme.BgEntryHover;
        sb.BorderWidthLeft = 3;
        sb.BorderColor = OverlayTheme.TextAccent;
        return sb;
    }

    public static StyleBoxFlat CreateBestEntryStyle()
    {
        var sb = CreateEntryStyle();
        sb.BgColor = OverlayTheme.BgEntryBest;
        sb.BorderWidthLeft = 4;
        sb.BorderColor = OverlayTheme.TextAccent;
        return sb;
    }

    public static StyleBoxFlat CreateBestEntryHoverStyle()
    {
        var sb = CreateBestEntryStyle();
        sb.BgColor = OverlayTheme.BgEntryBestHover;
        return sb;
    }

    public static StyleBoxFlat CreateSTierStyle()
    {
        var sb = CreateBestEntryStyle();
        sb.BorderWidthLeft = 5;
        sb.ShadowSize = 10;
        sb.ShadowColor = new Color(OverlayTheme.TextAccent, 0.6f);
        return sb;
    }

    public static StyleBoxFlat CreateSTierHoverStyle()
    {
        var sb = CreateSTierStyle();
        sb.BgColor = OverlayTheme.BgEntrySTierHover;
        return sb;
    }

    // ── Chip / Tag ───────────────────────────────────────────────

    public static StyleBoxFlat CreateChipStyle()
    {
        var sb = new StyleBoxFlat();
        sb.BgColor = OverlayTheme.BgChip;
        SetAllCornerRadius(sb, OverlayTheme.RadiusLG);
        sb.ContentMarginLeft = sb.ContentMarginRight = 12f;
        sb.ContentMarginTop = sb.ContentMarginBottom = 4f;
        sb.BorderWidthBottom = sb.BorderWidthTop = 1;
        sb.BorderColor = new Color(OverlayTheme.TextAccent, OverlayTheme.OpChipBorder);
        return sb;
    }

    public static StyleBoxFlat CreateArchTagChipStyle(Color tagColor)
    {
        var sb = new StyleBoxFlat();
        sb.BgColor = new Color(tagColor, OverlayTheme.OpChipBg);
        SetAllCornerRadius(sb, 12);
        sb.ContentMarginLeft = sb.ContentMarginRight = 10f;
        sb.ContentMarginTop = sb.ContentMarginBottom = 2f;
        SetAllBorderWidth(sb, 1);
        sb.BorderColor = new Color(tagColor, OverlayTheme.OpBorderSubtle);
        return sb;
    }

    // ── Thumbnail ────────────────────────────────────────────────

    public static StyleBoxFlat CreateThumbnailStyle(bool isBest)
    {
        var sb = new StyleBoxFlat();
        sb.BgColor = OverlayTheme.BgThumbnail;
        SetAllCornerRadius(sb, OverlayTheme.SpaceMD);
        SetAllBorderWidth(sb, 2);
        sb.BorderColor = isBest
            ? new Color(OverlayTheme.TextAccent, OverlayTheme.OpBorderAccent)
            : new Color(OverlayTheme.Border, OverlayTheme.OpBorderNormal);
        sb.ShadowSize = isBest ? 6 : 3;
        sb.ShadowColor = isBest
            ? new Color(OverlayTheme.TextAccent, OverlayTheme.OpShadowAccent)
            : new Color(0f, 0f, 0f, OverlayTheme.OpShadowNormal);
        return sb;
    }

    // ── Badge ────────────────────────────────────────────────────

    public static StyleBoxFlat CreateBadgeStyle(TierGrade grade)
    {
        Color badgeColor = OverlayTheme.GetTierColor(grade);
        var sb = new StyleBoxFlat { BgColor = badgeColor };
        sb.CornerRadiusTopLeft = 0;
        sb.CornerRadiusTopRight = OverlayTheme.RadiusMD;
        sb.CornerRadiusBottomLeft = OverlayTheme.RadiusMD;
        sb.CornerRadiusBottomRight = 0;
        SetAllBorderWidth(sb, 1);
        sb.BorderColor = badgeColor.Darkened(0.3f);
        return sb;
    }

    public static StyleBoxFlat CreateSkipBadgeStyle()
    {
        var sb = new StyleBoxFlat { BgColor = OverlayTheme.BgSkipBadge };
        sb.CornerRadiusTopLeft = 0;
        sb.CornerRadiusTopRight = OverlayTheme.RadiusMD;
        sb.CornerRadiusBottomLeft = OverlayTheme.RadiusMD;
        sb.CornerRadiusBottomRight = 0;
        SetAllBorderWidth(sb, 1);
        sb.BorderColor = OverlayTheme.Skip;
        return sb;
    }

    // ── Decision Entry (Stats) ───────────────────────────────────

    public static StyleBoxFlat CreateDecisionEntryStyle(Color borderColor)
    {
        var sb = new StyleBoxFlat();
        sb.BgColor = new Color(borderColor, 0.08f);
        sb.CornerRadiusTopRight = OverlayTheme.RadiusSM;
        sb.CornerRadiusBottomRight = OverlayTheme.RadiusSM;
        sb.BorderWidthLeft = 3;
        sb.BorderColor = borderColor;
        sb.ContentMarginLeft = 10f;
        sb.ContentMarginRight = 8f;
        sb.ContentMarginTop = sb.ContentMarginBottom = 4f;
        return sb;
    }

    // ── Separator ────────────────────────────────────────────────

    public static StyleBoxLine CreateSeparatorStyle(float opacity = -1f)
    {
        return new StyleBoxLine
        {
            Color = new Color(OverlayTheme.Border, opacity >= 0 ? opacity : OverlayTheme.OpBorderSubtle),
            Thickness = 1
        };
    }

    // ── Helpers ──────────────────────────────────────────────────

    public static void SetAllCornerRadius(StyleBoxFlat sb, int radius)
    {
        sb.CornerRadiusTopLeft = sb.CornerRadiusTopRight =
            sb.CornerRadiusBottomLeft = sb.CornerRadiusBottomRight = radius;
    }

    public static void SetAllBorderWidth(StyleBoxFlat sb, int width)
    {
        sb.BorderWidthTop = sb.BorderWidthBottom =
            sb.BorderWidthLeft = sb.BorderWidthRight = width;
    }

    /// <summary>
    /// One-shot label styling: font + size + color in a single call.
    /// Replaces the repeated ApplyFont() + AddThemeFontSizeOverride() + AddThemeColorOverride() pattern.
    /// </summary>
    public static void StyleLabel(Label label, Font font, int fontSize, Color color)
    {
        if (font != null) label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
    }
}
