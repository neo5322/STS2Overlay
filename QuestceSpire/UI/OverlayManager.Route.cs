using System;
using System.Collections.Generic;
using Godot;
using QuestceSpire.Core;

namespace QuestceSpire.UI;

/// <summary>Route advisor UI — map path recommendations displayed during map screen.</summary>
public partial class OverlayManager
{
	private VBoxContainer _routeContainer;

	/// <summary>
	/// Show route recommendations when the player is on the map screen.
	/// Called from Rebuild() when screen is MAP.
	/// </summary>
	private void ShowRouteAdvice()
	{
		if (_content == null) return;

		var settings = OverlaySettings.Load();
		if (!settings.ShowRouteAdvice) return;

		var routeAdvisor = Plugin.RouteAdvisor;
		if (routeAdvisor == null) return;

		// Get current game state for route context
		var state = _currentGameState;
		if (state == null) return;

		double hpPercent = state.MaxHP > 0 ? (double)state.CurrentHP / state.MaxHP : 1.0;
		int gold = state.Gold;
		int deckSize = state.Deck?.Count ?? 0;
		int act = state.Act > 0 ? state.Act : 1;

		// Determine if player has key engine pieces
		bool hasKeyCards = deckSize > 0 && Plugin.DeckAnalyzer != null &&
			Plugin.DeckAnalyzer.Analyze(state.Deck, state.Character)?.TopArchetype != null;

		// Try to read map nodes from game state
		var mapNodes = ReadMapNodes();
		if (mapNodes == null || mapNodes.Count == 0)
		{
			AddRouteHeader("경로 추천");
			AddRouteNote("맵 데이터를 읽을 수 없습니다.");
			return;
		}

		var analysis = routeAdvisor.Analyze(
			_currentFloor, 0, mapNodes, hpPercent, gold, deckSize, hasKeyCards, act);

		AddRouteHeader("경로 추천");

		if (!string.IsNullOrEmpty(analysis.OverallAdvice))
		{
			AddRouteNote(analysis.OverallAdvice);
		}

		foreach (var option in analysis.Options)
		{
			AddRouteOption(option);
		}
	}

	private void AddRouteHeader(string text)
	{
		if (_content == null) return;

		var header = new Label
		{
			Text = $"━━ {text} ━━",
		};
		header.AddThemeColorOverride("font_color", ClrHeader);
		header.AddThemeFontSizeOverride("font_size", 15);
		_content.AddChild(header);
	}

	private void AddRouteNote(string text)
	{
		if (_content == null) return;

		var label = new Label
		{
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		};
		label.AddThemeColorOverride("font_color", ClrSub);
		label.AddThemeFontSizeOverride("font_size", 12);
		_content.AddChild(label);
	}

	private void AddRouteOption(RouteAdvisor.RouteScore option)
	{
		if (_content == null) return;

		var hbox = new HBoxContainer();
		hbox.AddThemeConstantOverride("separation", 8);

		// Strategy badge
		var badge = new Label
		{
			Text = option.Strategy switch
			{
				"safe" => "[안전]",
				"aggressive" => "[공격]",
				"balanced" => "[균형]",
				_ => "[?]",
			},
		};
		badge.AddThemeColorOverride("font_color", option.Strategy switch
		{
			"safe" => ClrPositive,
			"aggressive" => ClrNegative,
			"balanced" => ClrAqua,
			_ => ClrSub,
		});
		badge.AddThemeFontSizeOverride("font_size", 12);
		hbox.AddChild(badge);

		// Score
		var scoreLabel = new Label
		{
			Text = $"{option.Score:F1}점",
		};
		scoreLabel.AddThemeColorOverride("font_color", option.Score >= 8 ? ClrPositive : option.Score >= 4 ? ClrSub : ClrNegative);
		scoreLabel.AddThemeFontSizeOverride("font_size", 12);
		hbox.AddChild(scoreLabel);

		// Node path preview
		var pathLabel = new Label
		{
			Text = string.Join(" → ", option.UpcomingNodes.ConvertAll(NodeToKorean)),
		};
		pathLabel.AddThemeColorOverride("font_color", ClrNotes);
		pathLabel.AddThemeFontSizeOverride("font_size", 11);
		hbox.AddChild(pathLabel);

		_content.AddChild(hbox);

		// Reason
		if (!string.IsNullOrEmpty(option.Reason))
		{
			var reasonLabel = new Label
			{
				Text = $"  └ {option.Reason}",
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
			};
			reasonLabel.AddThemeColorOverride("font_color", ClrNotes);
			reasonLabel.AddThemeFontSizeOverride("font_size", 10);
			_content.AddChild(reasonLabel);
		}
	}

	private static string NodeToKorean(string nodeType)
	{
		return nodeType switch
		{
			RouteAdvisor.NodeMonster => "전투",
			RouteAdvisor.NodeElite => "엘리트",
			RouteAdvisor.NodeRest => "휴식",
			RouteAdvisor.NodeShop => "상점",
			RouteAdvisor.NodeEvent => "이벤트",
			RouteAdvisor.NodeTreasure => "보물",
			RouteAdvisor.NodeBoss => "보스",
			_ => nodeType,
		};
	}

	/// <summary>
	/// Attempt to read map nodes from the game via reflection.
	/// Returns null if map data is unavailable.
	/// </summary>
	private List<RouteAdvisor.MapNode> ReadMapNodes()
	{
		try
		{
			// Try to access the map from RunManager via reflection
			var runMgrType = typeof(MegaCrit.Sts2.Core.Runs.RunManager);
			var instanceProp = runMgrType.GetProperty("Instance",
				System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
			if (instanceProp == null) return null;

			var instance = instanceProp.GetValue(null);
			if (instance == null) return null;

			// Look for map-related properties
			var mapProp = instance.GetType().GetProperty("Map") ??
			              instance.GetType().GetProperty("CurrentMap");
			if (mapProp == null) return null;

			var map = mapProp.GetValue(instance);
			if (map == null) return null;

			// Try to iterate map rows/nodes
			var nodesProp = map.GetType().GetProperty("Nodes") ??
			                map.GetType().GetProperty("AllNodes");
			if (nodesProp == null) return null;

			var nodes = nodesProp.GetValue(map);
			if (nodes == null) return null;

			var result = new List<RouteAdvisor.MapNode>();
			if (nodes is System.Collections.IEnumerable enumerable)
			{
				foreach (var node in enumerable)
				{
					var mapNode = ExtractMapNode(node);
					if (mapNode != null)
						result.Add(mapNode);
				}
			}

			return result.Count > 0 ? result : null;
		}
		catch (Exception ex)
		{
			Plugin.Log($"RouteAdvisor: failed to read map — {ex.Message}");
			return null;
		}
	}

	private RouteAdvisor.MapNode ExtractMapNode(object node)
	{
		try
		{
			var type = node.GetType();

			var floorProp = type.GetProperty("Row") ?? type.GetProperty("Floor") ?? type.GetProperty("Y");
			var colProp = type.GetProperty("Column") ?? type.GetProperty("Col") ?? type.GetProperty("X");
			var typeProp = type.GetProperty("NodeType") ?? type.GetProperty("Type") ?? type.GetProperty("RoomType");

			int floor = floorProp != null ? Convert.ToInt32(floorProp.GetValue(node)) : -1;
			int col = colProp != null ? Convert.ToInt32(colProp.GetValue(node)) : -1;
			string nodeType = typeProp?.GetValue(node)?.ToString()?.ToUpperInvariant() ?? "UNKNOWN";

			if (floor < 0 || col < 0) return null;

			var mapNode = new RouteAdvisor.MapNode
			{
				Floor = floor,
				Column = col,
				NodeType = NormalizeNodeType(nodeType),
			};

			// Try to read connections
			var connProp = type.GetProperty("ConnectedNodes") ??
			               type.GetProperty("Connections") ??
			               type.GetProperty("Children");
			if (connProp != null)
			{
				var conns = connProp.GetValue(node);
				if (conns is System.Collections.IEnumerable connEnum)
				{
					foreach (var conn in connEnum)
					{
						var connColProp = conn.GetType().GetProperty("Column") ??
						                  conn.GetType().GetProperty("Col") ??
						                  conn.GetType().GetProperty("X");
						if (connColProp != null)
							mapNode.ConnectedColumns.Add(Convert.ToInt32(connColProp.GetValue(conn)));
					}
				}
			}

			return mapNode;
		}
		catch
		{
			return null;
		}
	}

	private static string NormalizeNodeType(string raw)
	{
		if (string.IsNullOrEmpty(raw)) return "UNKNOWN";
		raw = raw.ToUpperInvariant();

		if (raw.Contains("ELITE")) return RouteAdvisor.NodeElite;
		if (raw.Contains("BOSS")) return RouteAdvisor.NodeBoss;
		if (raw.Contains("REST") || raw.Contains("CAMP")) return RouteAdvisor.NodeRest;
		if (raw.Contains("SHOP") || raw.Contains("MERCHANT")) return RouteAdvisor.NodeShop;
		if (raw.Contains("EVENT") || raw.Contains("QUESTION") || raw.Contains("?")) return RouteAdvisor.NodeEvent;
		if (raw.Contains("TREASURE") || raw.Contains("CHEST")) return RouteAdvisor.NodeTreasure;
		if (raw.Contains("MONSTER") || raw.Contains("COMBAT") || raw.Contains("FIGHT")) return RouteAdvisor.NodeMonster;

		return raw;
	}
}
