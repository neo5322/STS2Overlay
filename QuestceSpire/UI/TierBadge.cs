using Godot;
using QuestceSpire.Core;

namespace QuestceSpire.UI;

/// <summary>
/// Tier grade color lookups. Delegates to OverlayTheme for canonical color definitions.
/// </summary>
public static class TierBadge
{
	public static Color GetGodotColor(TierGrade grade) => OverlayTheme.GetTierColor(grade);

	public static Color GetTextColor(TierGrade grade) => OverlayTheme.GetTierTextColor(grade);
}
