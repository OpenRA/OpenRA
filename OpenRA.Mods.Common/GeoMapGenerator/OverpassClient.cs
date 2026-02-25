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
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRA.Mods.Common.GeoMapGenerator
{
	/// <summary>
	/// Fetches OSM data from the Overpass API with disk cache.
	/// </summary>
	public sealed class OverpassClient : IDisposable
	{
		const string CacheVersion = "v3";
		readonly HttpClient client;
		readonly string cacheDir;

		public OverpassClient(string cacheDirectory = null)
		{
			client = new HttpClient();
			cacheDir = cacheDirectory ?? Path.Combine(Platform.SupportDir, "geomaps-cache");
		}

		/// <summary>
		/// Build the Overpass QL query for the given bounding box.
		/// </summary>
		public static string BuildQuery(double south, double west, double north, double east, int timeout)
		{
			var s = south.ToString("F6", CultureInfo.InvariantCulture);
			var w = west.ToString("F6", CultureInfo.InvariantCulture);
			var n = north.ToString("F6", CultureInfo.InvariantCulture);
			var e = east.ToString("F6", CultureInfo.InvariantCulture);
			var bbox = $"{s},{w},{n},{e}";

			return $"[out:json][timeout:{timeout}];"
				+ $"("
				+ $"way['highway']({bbox});"
				+ $"way['waterway']({bbox});"
				+ $"way['natural'='water']({bbox});"
				+ $"way['landuse'='reservoir']({bbox});"
				+ $"way['natural'='coastline']({bbox});"
				+ $"way['building']({bbox});"
				+ $"relation['building']({bbox});"
				+ $"way['natural'='wood']({bbox});"
				+ $"way['landuse'='forest']({bbox});"
				+ $"way['landcover'='trees']({bbox});"
				+ $"way['landuse'='residential']({bbox});"
				+ $"way['landuse'='industrial']({bbox});"
				+ $"way['landuse'='commercial']({bbox});"
				+ $"relation['natural'='water']({bbox});"
				+ $");"
				+ $"(._;>;>;);"
				+ $"out body qt;";
		}

		/// <summary>
		/// Fetch OSM data for the given bounding box. Uses disk cache if available.
		/// </summary>
		public async Task<string> FetchAsync(double south, double west, double north, double east,
			string overpassUrl, int timeout,
			Action<string, int> onProgress = null, CancellationToken ct = default)
		{
			var query = BuildQuery(south, west, north, east, timeout);

			// Try cache
			var cachePath = GetCachePath(south, west, north, east, query);
			if (cachePath != null && File.Exists(cachePath))
			{
				onProgress?.Invoke("Loading cached OSM data...", 30);
				return await File.ReadAllTextAsync(cachePath, ct);
			}

			// HTTP fetch
			onProgress?.Invoke("Fetching OSM data from Overpass API...", 10);
			client.Timeout = TimeSpan.FromSeconds(timeout + 20);

			var content = new FormUrlEncodedContent(new[]
			{
				new System.Collections.Generic.KeyValuePair<string, string>("data", query)
			});

			var response = await client.PostAsync(overpassUrl, content, ct);
			response.EnsureSuccessStatusCode();

			onProgress?.Invoke("Reading response...", 40);
			var json = await response.Content.ReadAsStringAsync(ct);

			// Cache
			if (cachePath != null)
			{
				try
				{
					Directory.CreateDirectory(cacheDir);
					await File.WriteAllTextAsync(cachePath, json, ct);
				}
				catch
				{
					// Cache write failure is non-fatal
				}
			}

			onProgress?.Invoke("OSM data received.", 50);
			return json;
		}

		string GetCachePath(double south, double west, double north, double east, string query)
		{
			try
			{
				var key = $"{CacheVersion}|{south:F6},{west:F6},{north:F6},{east:F6}|{query}";
				var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
				var hex = Convert.ToHexString(hash).ToLowerInvariant();
				return Path.Combine(cacheDir, $"{hex}.json");
			}
			catch
			{
				return null;
			}
		}

		public void Dispose()
		{
			client?.Dispose();
		}
	}
}
