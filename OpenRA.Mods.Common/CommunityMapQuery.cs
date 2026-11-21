#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenRA.Support;

namespace OpenRA.Mods.Common
{
	public sealed class CommunityMapQuery
	{
		const int PageSize = 20;
		const int MaxPage = 10000;

		readonly string apiUrl;
		readonly string mapRepositoryUrl;
		readonly ModData modData;
		readonly MapCache mapCache;

		CancellationTokenSource searchCts;
		CancellationTokenSource countCts;

		/// <summary>Hashes of maps on the current page.</summary>
		readonly HashSet<string> currentResultHashes = [];

		public string SortBy { get; set; } = "latest";
		public string Tileset { get; set; }
		public int? Players { get; set; }
		public bool OnlyAdvanced { get; set; }
		public bool OnlyLua { get; set; }
		public bool IsLoading { get; private set; }

		/// <summary>Current page number (1-based).</summary>
		public int CurrentPage { get; private set; } = 1;

		/// <summary>Total number of pages, or null while still being determined.</summary>
		public int? TotalPages { get; private set; }

		/// <summary>Total number of maps matching the current filters on the server.
		/// Null while the count is being determined.</summary>
		public int? TotalAvailable { get; private set; }

		/// <summary>Returns whether the given map hash is part of the current page results.</summary>
		public bool ContainsHash(string hash) => currentResultHashes.Contains(hash);

		public CommunityMapQuery(ModData modData, string apiUrl, string mapRepositoryUrl)
		{
			this.modData = modData;
			this.apiUrl = apiUrl;
			this.mapRepositoryUrl = mapRepositoryUrl;
			mapCache = modData.MapCache;
		}

		public void Search(Action onComplete, Action<string> onError)
		{
			// Cancel any in-flight request before starting a new search.
			CancelPending();

			CurrentPage = 1;
			TotalPages = null;
			TotalAvailable = null;
			IsLoading = false;
			currentResultHashes.Clear();
			LoadPage(1, onComplete, onError);
			CountTotalAsync();
		}

		public void GoToPage(int page, Action onComplete, Action<string> onError)
		{
			if (IsLoading || page < 1)
				return;

			// If we know the total pages, clamp.
			if (TotalPages.HasValue && page > TotalPages.Value)
				return;

			CancelSearchPending();
			currentResultHashes.Clear();
			LoadPage(page, onComplete, onError);
		}

		HttpQueryBuilder BuildQuery(int page, string sortBy, string tileset,
			int? players = null, bool onlyAdvanced = false, bool onlyLua = false)
		{
			var query = new HttpQueryBuilder(apiUrl)
			{
				{ "mod", modData.Manifest.Id },
				{ "page", page },
				{ "sort_by", sortBy },
				{ "format", Map.CurrentMapFormat },
				{ "with_problems", "hide_lint_failed" },
			};

			if (!string.IsNullOrEmpty(tileset))
				query.Add("tileset", tileset);

			if (players.HasValue)
				query.Add("players", players.Value);

			if (onlyAdvanced)
				query.Add("only_advanced", "on");

			if (onlyLua)
				query.Add("only_lua", "on");

			return query;
		}

		void LoadPage(int page, Action onComplete, Action<string> onError)
		{
			if (IsLoading)
				return;

			IsLoading = true;

			searchCts ??= new CancellationTokenSource();
			var token = searchCts.Token;

			var query = BuildQuery(page, SortBy, Tileset, Players, OnlyAdvanced, OnlyLua);

			Task.Run(async () =>
			{
				try
				{
					var client = HttpClientFactory.Create();
					var json = await client.GetStringAsync(query.ToString(), token);

					if (token.IsCancellationRequested)
						return;

					var hashes = ParseMapHashes(json);

					foreach (var hash in hashes)
						currentResultHashes.Add(hash);

					// Filter out maps already known locally as System or User.
					var newHashes = hashes
						.Where(h =>
						{
							var p = mapCache[h];
							return p.Status == MapStatus.Unavailable
								&& p.Class != MapClassification.System
								&& p.Class != MapClassification.User;
						})
						.ToList();

					if (newHashes.Count > 0)
					{
						mapCache.QueryRemoteMapDetails(
							mapRepositoryUrl,
							newHashes,
							targetClass: MapClassification.Community);
					}

					Game.RunAfterTick(() =>
					{
						CurrentPage = page;
						IsLoading = false;
						onComplete?.Invoke();
					});
				}
				catch (OperationCanceledException)
				{
					// Request was cancelled by a new Search call. Do nothing.
				}
				catch (Exception e)
				{
					Log.Write("debug", $"Community map query failed: {e}");
					Game.RunAfterTick(() =>
					{
						IsLoading = false;
						onError?.Invoke(e.Message);
					});
				}
			});
		}

		/// <summary>Uses binary search to find the total number of maps matching
		/// the current filters without downloading all pages.</summary>
		void CountTotalAsync()
		{
			CancelCountPending();
			countCts = new CancellationTokenSource();
			var token = countCts.Token;

			// Capture current filter state so the result is discarded if filters change.
			var queryTileset = Tileset;
			var queryPlayers = Players;
			var queryAdvanced = OnlyAdvanced;
			var queryLua = OnlyLua;

			Task.Run(async () =>
			{
				try
				{
					var client = HttpClientFactory.Create();

					// Binary search for the last page that contains results.
					var low = 1;
					var high = 1;

					// First, find an upper bound by doubling.
					while (high <= MaxPage)
					{
						if (token.IsCancellationRequested)
							return;

						var count = await GetPageCount(client, high, queryTileset, queryPlayers, queryAdvanced, queryLua, token);
						if (count < PageSize)
							break;

						low = high;
						high = Math.Min(high * 2, MaxPage + 1);
					}

					// Binary search between low and high.
					while (low < high)
					{
						if (token.IsCancellationRequested)
							return;

						var mid = low + (high - low) / 2;
						var count = await GetPageCount(client, mid, queryTileset, queryPlayers, queryAdvanced, queryLua, token);
						if (count < PageSize)
							high = mid;
						else
							low = mid + 1;
					}

					// low == high == the first page with fewer than PageSize results.
					// Count that page to get the exact remainder.
					if (token.IsCancellationRequested)
						return;

					var lastPageCount = await GetPageCount(client, low, queryTileset, queryPlayers, queryAdvanced, queryLua, token);
					var total = (low - 1) * PageSize + lastPageCount;

					Game.RunAfterTick(() =>
					{
						if (!token.IsCancellationRequested)
						{
							TotalAvailable = total;
							TotalPages = total == 0 ? 1 : (total + PageSize - 1) / PageSize;
						}
					});
				}
				catch (OperationCanceledException)
				{
					// Cancelled by a new search — ignore.
				}
				catch (Exception e)
				{
					Log.Write("debug", $"Community map count failed: {e}");
				}
			});
		}

		async Task<int> GetPageCount(System.Net.Http.HttpClient client, int page,
			string tileset, int? players, bool onlyAdvanced, bool onlyLua, CancellationToken token)
		{
			var query = BuildQuery(page, "latest", tileset, players, onlyAdvanced, onlyLua);
			var json = await client.GetStringAsync(query.ToString(), token);
			using var doc = JsonDocument.Parse(json);
			return doc.RootElement.EnumerateObject().Count();
		}

		void CancelCountPending()
		{
			if (countCts != null)
			{
				countCts.Cancel();
				countCts.Dispose();
				countCts = null;
			}
		}

		void CancelSearchPending()
		{
			if (searchCts != null)
			{
				searchCts.Cancel();
				searchCts.Dispose();
				searchCts = null;
			}
		}

		public void CancelPending()
		{
			CancelSearchPending();
			CancelCountPending();
		}

		static List<string> ParseMapHashes(string json)
		{
			var hashes = new List<string>();

			using var doc = JsonDocument.Parse(json);
			foreach (var property in doc.RootElement.EnumerateObject())
			{
				if (property.Value.ValueKind != JsonValueKind.Object)
					continue;

				if (!property.Value.TryGetProperty("map_hash", out var hashElement))
					continue;

				var hash = hashElement.GetString();
				if (!string.IsNullOrEmpty(hash))
					hashes.Add(hash);
			}

			return hashes;
		}
	}
}
