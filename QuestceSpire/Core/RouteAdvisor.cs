using System;
using System.Collections.Generic;
using System.Linq;

namespace QuestceSpire.Core;

/// <summary>
/// Recommends map paths (safe vs aggressive) by scoring each node
/// based on deck strength, HP, gold, relics, and floor context.
/// Inspired by STS2 Plus / Route Suggest community mods.
/// </summary>
public class RouteAdvisor
{
	// Node types recognized from the game map
	public const string NodeMonster = "MONSTER";
	public const string NodeElite = "ELITE";
	public const string NodeRest = "REST";
	public const string NodeShop = "SHOP";
	public const string NodeEvent = "EVENT";
	public const string NodeTreasure = "TREASURE";
	public const string NodeBoss = "BOSS";

	/// <summary>
	/// A single node on the map with its type and connections.
	/// </summary>
	public class MapNode
	{
		public int Floor { get; set; }
		public int Column { get; set; }
		public string NodeType { get; set; }
		public List<int> ConnectedColumns { get; set; } = new();
	}

	/// <summary>
	/// Scored route recommendation for a single path choice.
	/// </summary>
	public class RouteScore
	{
		public int Column { get; set; }
		public double Score { get; set; }
		public string Strategy { get; set; } // "safe", "aggressive", "balanced"
		public string Reason { get; set; }
		public List<string> UpcomingNodes { get; set; } = new();
	}

	/// <summary>
	/// Full route analysis result.
	/// </summary>
	public class RouteAnalysis
	{
		public List<RouteScore> Options { get; set; } = new();
		public int RecommendedColumn { get; set; }
		public string OverallAdvice { get; set; }
	}

	/// <summary>
	/// Analyze available paths from the current floor and recommend the best route.
	/// </summary>
	/// <param name="currentFloor">Current floor number (0-based)</param>
	/// <param name="currentColumn">Current column position</param>
	/// <param name="mapNodes">All known map nodes</param>
	/// <param name="hpPercent">Current HP as 0.0-1.0 ratio</param>
	/// <param name="gold">Current gold</param>
	/// <param name="deckSize">Current deck size</param>
	/// <param name="hasKeyCards">Whether deck has key engine pieces assembled</param>
	/// <param name="act">Current act number (1-3)</param>
	public RouteAnalysis Analyze(
		int currentFloor,
		int currentColumn,
		List<MapNode> mapNodes,
		double hpPercent,
		int gold,
		int deckSize,
		bool hasKeyCards,
		int act)
	{
		var result = new RouteAnalysis();

		// Find nodes on the next floor that connect from current position
		var currentNode = mapNodes.FirstOrDefault(n => n.Floor == currentFloor && n.Column == currentColumn);
		if (currentNode == null || currentNode.ConnectedColumns.Count == 0)
		{
			result.OverallAdvice = "맵 데이터 없음";
			return result;
		}

		// Determine player state profile
		var profile = ClassifyPlayerState(hpPercent, gold, deckSize, hasKeyCards, act);

		foreach (int nextCol in currentNode.ConnectedColumns)
		{
			var score = ScorePath(currentFloor + 1, nextCol, mapNodes, profile, lookAhead: 3);
			result.Options.Add(score);
		}

		// Sort by score descending
		result.Options.Sort((a, b) => b.Score.CompareTo(a.Score));

		if (result.Options.Count > 0)
		{
			result.RecommendedColumn = result.Options[0].Column;
			result.OverallAdvice = GenerateAdvice(result.Options, profile);
		}

		return result;
	}

	private RouteScore ScorePath(int startFloor, int startCol, List<MapNode> mapNodes, PlayerProfile profile, int lookAhead)
	{
		var score = new RouteScore { Column = startCol };
		double totalScore = 0;
		var upcomingNodes = new List<string>();

		int col = startCol;
		for (int i = 0; i < lookAhead; i++)
		{
			int floor = startFloor + i;
			var node = mapNodes.FirstOrDefault(n => n.Floor == floor && n.Column == col);
			if (node == null) break;

			upcomingNodes.Add(node.NodeType);
			double nodeScore = ScoreNode(node.NodeType, profile, floor);
			// Discount future nodes (closer = more weight)
			totalScore += nodeScore * (1.0 - i * 0.2);

			// Follow best-connected path for look-ahead
			if (node.ConnectedColumns.Count > 0)
				col = node.ConnectedColumns[0]; // Simple: follow first connection
		}

		score.Score = Math.Round(totalScore, 1);
		score.UpcomingNodes = upcomingNodes;
		score.Strategy = ClassifyStrategy(upcomingNodes);
		score.Reason = GenerateNodeReason(upcomingNodes, profile);

		return score;
	}

	private double ScoreNode(string nodeType, PlayerProfile profile, int floor)
	{
		return nodeType switch
		{
			NodeMonster => profile.WantsCards ? 3.0 : 1.5,
			NodeElite => profile.CanFightElite ? 5.0 : -2.0,
			NodeRest => profile.NeedsHeal ? 4.0 : (profile.WantsUpgrade ? 3.5 : 1.0),
			NodeShop => profile.WantsShop ? 4.0 : 1.5,
			NodeEvent => 2.5, // Events are generally neutral-positive
			NodeTreasure => 3.5,
			NodeBoss => 0, // Boss is mandatory, no scoring
			_ => 1.0,
		};
	}

	private class PlayerProfile
	{
		public bool NeedsHeal { get; set; }
		public bool WantsUpgrade { get; set; }
		public bool WantsCards { get; set; }
		public bool WantsShop { get; set; }
		public bool CanFightElite { get; set; }
		public string State { get; set; } // "desperate", "cautious", "stable", "strong"
	}

	private PlayerProfile ClassifyPlayerState(double hpPercent, int gold, int deckSize, bool hasKeyCards, int act)
	{
		var profile = new PlayerProfile();

		if (hpPercent < 0.3)
		{
			profile.State = "desperate";
			profile.NeedsHeal = true;
			profile.CanFightElite = false;
		}
		else if (hpPercent < 0.5)
		{
			profile.State = "cautious";
			profile.NeedsHeal = true;
			profile.CanFightElite = hasKeyCards;
		}
		else if (hpPercent < 0.75 || !hasKeyCards)
		{
			profile.State = "stable";
			profile.NeedsHeal = false;
			profile.CanFightElite = hasKeyCards;
		}
		else
		{
			profile.State = "strong";
			profile.NeedsHeal = false;
			profile.CanFightElite = true;
		}

		profile.WantsUpgrade = hasKeyCards && act <= 2;
		profile.WantsCards = deckSize < 20 && !hasKeyCards;
		profile.WantsShop = gold >= 100 || (gold >= 50 && deckSize > 22); // Shop for removal

		return profile;
	}

	private string ClassifyStrategy(List<string> nodes)
	{
		int elites = nodes.Count(n => n == NodeElite);
		int rests = nodes.Count(n => n == NodeRest);
		int fights = nodes.Count(n => n == NodeMonster || n == NodeElite);

		if (elites >= 2) return "aggressive";
		if (rests >= 2 || fights == 0) return "safe";
		return "balanced";
	}

	private string GenerateNodeReason(List<string> nodes, PlayerProfile profile)
	{
		var reasons = new List<string>();
		foreach (var n in nodes)
		{
			reasons.Add(n switch
			{
				NodeElite when profile.CanFightElite => "엘리트 (유물 획득 기회)",
				NodeElite => "엘리트 (위험!)",
				NodeRest when profile.NeedsHeal => "휴식 (회복 필요)",
				NodeRest => "휴식 (업그레이드)",
				NodeShop when profile.WantsShop => "상점 (카드 제거/구매)",
				NodeShop => "상점",
				NodeEvent => "이벤트",
				NodeMonster => "일반 전투",
				NodeTreasure => "보물",
				_ => n,
			});
		}
		return string.Join(" → ", reasons);
	}

	private string GenerateAdvice(List<RouteScore> options, PlayerProfile profile)
	{
		if (options.Count == 0) return "";

		var best = options[0];
		return profile.State switch
		{
			"desperate" => $"HP 위험! {best.Strategy} 경로 추천. {best.Reason}",
			"cautious" => $"조심스럽게 진행. {best.Reason}",
			"stable" => $"안정적. {best.Strategy} 경로로 {best.Reason}",
			"strong" => $"강한 상태! {best.Strategy} 경로. {best.Reason}",
			_ => best.Reason,
		};
	}
}
