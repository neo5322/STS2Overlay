using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuestceSpire.Tracking;

/// <summary>
/// Aggregates community win-rate data from multiple external APIs:
/// - STS2.fun (pick rates, win rates)
/// - sts2-advisor API (community scoring)
/// - Knowledge Demon (run analytics)
/// Merges results into a unified format for AdaptiveScorer consumption.
/// </summary>
public class CommunityApiClient
{
	private static readonly TimeSpan RateLimit = TimeSpan.FromSeconds(2);
	private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(6);

	private readonly string _cacheFolder;
	private readonly RunDatabase _db;

	public CommunityApiClient(RunDatabase db, string dataFolder)
	{
		_db = db;
		_cacheFolder = Path.Combine(dataFolder, "community_cache");
		if (!Directory.Exists(_cacheFolder))
			Directory.CreateDirectory(_cacheFolder);
	}

	/// <summary>
	/// Fetch and merge community data from all available sources.
	/// Results are cached to disk and merged into the local database.
	/// </summary>
	public async Task<CommunityAggregateResult> FetchAll(string character = null)
	{
		var result = new CommunityAggregateResult();

		// Check disk cache first
		var cached = LoadFromCache(character);
		if (cached != null)
		{
			Plugin.Log("CommunityApiClient: using cached data.");
			return cached;
		}

		var tasks = new List<Task>();

		// Source 1: STS2.fun
		tasks.Add(Task.Run(async () =>
		{
			try
			{
				var data = await FetchSts2Fun(character);
				if (data != null)
				{
					lock (result) { result.Sources.Add("sts2.fun"); MergeCardStats(result, data); }
				}
			}
			catch (Exception ex) { Plugin.Log($"CommunityApiClient: sts2.fun failed — {ex.Message}"); }
		}));

		// Source 2: sts2-advisor community API
		tasks.Add(Task.Run(async () =>
		{
			try
			{
				var data = await FetchSts2Advisor(character);
				if (data != null)
				{
					lock (result) { result.Sources.Add("sts2-advisor"); MergeCardStats(result, data); }
				}
			}
			catch (Exception ex) { Plugin.Log($"CommunityApiClient: sts2-advisor failed — {ex.Message}"); }
		}));

		await Task.WhenAll(tasks);

		result.FetchedAt = DateTime.UtcNow;
		result.TotalSources = result.Sources.Count;

		if (result.Sources.Count > 0)
		{
			SaveToCache(character, result);
			Plugin.Log($"CommunityApiClient: aggregated from {result.Sources.Count} source(s), {result.CardStats.Count} cards, {result.RelicStats.Count} relics.");
		}

		return result;
	}

	/// <summary>
	/// Apply aggregated community data to the local database for AdaptiveScorer.
	/// </summary>
	public void ApplyToDatabase(CommunityAggregateResult data)
	{
		if (data == null || data.CardStats.Count == 0) return;

		try
		{
			var cardStats = data.CardStats.Values.Select(s => new CommunityCardStats
			{
				CardId = s.CardId,
				Character = s.Character,
				PickRate = s.PickRate,
				WinRateWhenPicked = s.WinRateWhenPicked,
				WinRateWhenSkipped = s.WinRateWhenSkipped,
				SampleSize = s.SampleSize,
			}).ToList();

			_db.MergeCommunityCardStats(cardStats);

			var relicStats = data.RelicStats.Values.Select(s => new CommunityRelicStats
			{
				RelicId = s.RelicId,
				Character = s.Character,
				PickRate = s.PickRate,
				WinRateWhenPicked = s.WinRateWhenPicked,
				SampleSize = s.SampleSize,
			}).ToList();

			if (relicStats.Count > 0)
				_db.MergeCommunityRelicStats(relicStats);

			Plugin.Log($"CommunityApiClient: applied {cardStats.Count} card stats, {relicStats.Count} relic stats to database.");
		}
		catch (Exception ex)
		{
			Plugin.Log($"CommunityApiClient: apply error — {ex.Message}");
		}
	}

	#region STS2.fun

	private async Task<List<AggCardStat>> FetchSts2Fun(string character)
	{
		string url = "https://sts2.fun/api/stats";
		if (!string.IsNullOrEmpty(character))
			url += $"?character={Uri.EscapeDataString(character)}";

		var json = await PipelineHttp.RetryAsync(
			() => PipelineHttp.GetAsync(url, RateLimit), maxRetries: 2);

		var obj = JObject.Parse(json);
		var stats = new List<AggCardStat>();

		var cards = obj["cards"] as JObject;
		if (cards != null)
		{
			foreach (var prop in cards.Properties())
			{
				var card = prop.Value;
				stats.Add(new AggCardStat
				{
					CardId = prop.Name,
					Character = character ?? "all",
					PickRate = card["pick_rate"]?.Value<double>() ?? 0,
					WinRateWhenPicked = card["win_rate"]?.Value<double>() ?? 0,
					WinRateWhenSkipped = card["skip_win_rate"]?.Value<double>() ?? 0,
					SampleSize = card["sample_size"]?.Value<int>() ?? 0,
					Source = "sts2.fun",
				});
			}
		}

		return stats.Count > 0 ? stats : null;
	}

	#endregion

	#region sts2-advisor

	private async Task<List<AggCardStat>> FetchSts2Advisor(string character)
	{
		string url = "https://sts2-advisor-api.workers.dev/api/stats?min_samples=5";
		if (!string.IsNullOrEmpty(character))
			url += $"&character={Uri.EscapeDataString(character)}";

		var json = await PipelineHttp.RetryAsync(
			() => PipelineHttp.GetAsync(url, RateLimit), maxRetries: 2);

		var obj = JObject.Parse(json);
		var stats = new List<AggCardStat>();

		var cardStats = obj["card_stats"] as JArray;
		if (cardStats != null)
		{
			foreach (var card in cardStats)
			{
				stats.Add(new AggCardStat
				{
					CardId = card["card_id"]?.ToString() ?? "",
					Character = card["character"]?.ToString() ?? character ?? "all",
					PickRate = card["pick_rate"]?.Value<double>() ?? 0,
					WinRateWhenPicked = card["win_rate_picked"]?.Value<double>() ?? 0,
					WinRateWhenSkipped = card["win_rate_skipped"]?.Value<double>() ?? 0,
					SampleSize = card["sample_size"]?.Value<int>() ?? 0,
					Source = "sts2-advisor",
				});
			}
		}

		return stats.Count > 0 ? stats : null;
	}

	#endregion

	#region Merge Logic

	private void MergeCardStats(CommunityAggregateResult result, List<AggCardStat> newStats)
	{
		foreach (var stat in newStats)
		{
			string key = $"{stat.CardId}|{stat.Character}";
			if (result.CardStats.TryGetValue(key, out var existing))
			{
				// Weighted average based on sample size
				int totalSamples = existing.SampleSize + stat.SampleSize;
				if (totalSamples > 0)
				{
					double w1 = (double)existing.SampleSize / totalSamples;
					double w2 = (double)stat.SampleSize / totalSamples;
					existing.PickRate = existing.PickRate * w1 + stat.PickRate * w2;
					existing.WinRateWhenPicked = existing.WinRateWhenPicked * w1 + stat.WinRateWhenPicked * w2;
					existing.WinRateWhenSkipped = existing.WinRateWhenSkipped * w1 + stat.WinRateWhenSkipped * w2;
					existing.SampleSize = totalSamples;
				}
			}
			else
			{
				result.CardStats[key] = stat;
			}
		}
	}

	#endregion

	#region Cache

	private CommunityAggregateResult LoadFromCache(string character)
	{
		try
		{
			string path = GetCachePath(character);
			if (!File.Exists(path)) return null;

			var info = new FileInfo(path);
			if (DateTime.UtcNow - info.LastWriteTimeUtc > CacheExpiry) return null;

			var json = File.ReadAllText(path);
			return JsonConvert.DeserializeObject<CommunityAggregateResult>(json);
		}
		catch { return null; }
	}

	private void SaveToCache(string character, CommunityAggregateResult data)
	{
		try
		{
			string path = GetCachePath(character);
			File.WriteAllText(path, JsonConvert.SerializeObject(data));
		}
		catch (Exception ex)
		{
			Plugin.Log($"CommunityApiClient: cache save failed — {ex.Message}");
		}
	}

	private string GetCachePath(string character)
	{
		string name = string.IsNullOrEmpty(character) ? "all" : character.ToLowerInvariant();
		return Path.Combine(_cacheFolder, $"community_{name}.json");
	}

	#endregion
}

#region Data Models

public class CommunityAggregateResult
{
	public Dictionary<string, AggCardStat> CardStats { get; set; } = new();
	public Dictionary<string, AggRelicStat> RelicStats { get; set; } = new();
	public List<string> Sources { get; set; } = new();
	public int TotalSources { get; set; }
	public DateTime FetchedAt { get; set; }
}

public class AggCardStat
{
	public string CardId { get; set; }
	public string Character { get; set; }
	public double PickRate { get; set; }
	public double WinRateWhenPicked { get; set; }
	public double WinRateWhenSkipped { get; set; }
	public int SampleSize { get; set; }
	public string Source { get; set; }
}

public class AggRelicStat
{
	public string RelicId { get; set; }
	public string Character { get; set; }
	public double PickRate { get; set; }
	public double WinRateWhenPicked { get; set; }
	public int SampleSize { get; set; }
	public string Source { get; set; }
}

#endregion
