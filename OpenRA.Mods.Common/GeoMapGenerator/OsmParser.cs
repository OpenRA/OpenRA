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
using System.Text.Json;

namespace OpenRA.Mods.Common.GeoMapGenerator
{
	public readonly struct OsmNode
	{
		public readonly long Id;
		public readonly double Lat;
		public readonly double Lon;
		public readonly IReadOnlyDictionary<string, string> Tags;

		public OsmNode(long id, double lat, double lon, IReadOnlyDictionary<string, string> tags)
		{
			Id = id;
			Lat = lat;
			Lon = lon;
			Tags = tags;
		}
	}

	public readonly struct OsmWay
	{
		public readonly long Id;
		public readonly long[] NodeIds;
		public readonly IReadOnlyDictionary<string, string> Tags;

		public OsmWay(long id, long[] nodeIds, IReadOnlyDictionary<string, string> tags)
		{
			Id = id;
			NodeIds = nodeIds;
			Tags = tags;
		}
	}

	public readonly struct OsmRelation
	{
		public readonly long Id;
		public readonly OsmRelationMember[] Members;
		public readonly IReadOnlyDictionary<string, string> Tags;

		public OsmRelation(long id, OsmRelationMember[] members, IReadOnlyDictionary<string, string> tags)
		{
			Id = id;
			Members = members;
			Tags = tags;
		}
	}

	public readonly struct OsmRelationMember
	{
		public readonly string Type;
		public readonly long Ref;
		public readonly string Role;

		public OsmRelationMember(string type, long refId, string role)
		{
			Type = type;
			Ref = refId;
			Role = role;
		}
	}

	/// <summary>
	/// Parsed Overpass API JSON response indexed for fast lookup.
	/// </summary>
	public sealed class OsmData
	{
		public IReadOnlyDictionary<long, OsmNode> NodesById { get; }
		public IReadOnlyDictionary<long, OsmWay> WaysById { get; }
		public IReadOnlyList<OsmRelation> Relations { get; }

		OsmData(Dictionary<long, OsmNode> nodes, Dictionary<long, OsmWay> ways, List<OsmRelation> relations)
		{
			NodesById = nodes;
			WaysById = ways;
			Relations = relations;
		}

		/// <summary>
		/// Parse Overpass API JSON response into indexed structures.
		/// </summary>
		public static OsmData Parse(string json)
		{
			var nodes = new Dictionary<long, OsmNode>();
			var ways = new Dictionary<long, OsmWay>();
			var relations = new List<OsmRelation>();

			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;

			if (!root.TryGetProperty("elements", out var elements))
				return new OsmData(nodes, ways, relations);

			foreach (var el in elements.EnumerateArray())
			{
				if (!el.TryGetProperty("type", out var typeProp))
					continue;

				var type = typeProp.GetString();
				var id = el.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : 0;
				var tags = ParseTags(el);

				switch (type)
				{
					case "node":
					{
						if (el.TryGetProperty("lat", out var latProp) && el.TryGetProperty("lon", out var lonProp))
						{
							var node = new OsmNode(id, latProp.GetDouble(), lonProp.GetDouble(), tags);
							nodes[id] = node;
						}

						break;
					}

					case "way":
					{
						var nodeIds = ParseNodeIds(el);
						var way = new OsmWay(id, nodeIds, tags);
						ways[id] = way;
						break;
					}

					case "relation":
					{
						var members = ParseMembers(el);
						var relation = new OsmRelation(id, members, tags);
						relations.Add(relation);
						break;
					}
				}
			}

			return new OsmData(nodes, ways, relations);
		}

		static IReadOnlyDictionary<string, string> ParseTags(JsonElement el)
		{
			var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (el.TryGetProperty("tags", out var tagsProp) && tagsProp.ValueKind == JsonValueKind.Object)
			{
				foreach (var kv in tagsProp.EnumerateObject())
					tags[kv.Name] = kv.Value.GetString() ?? "";
			}

			return tags;
		}

		static long[] ParseNodeIds(JsonElement el)
		{
			if (!el.TryGetProperty("nodes", out var nodesProp) || nodesProp.ValueKind != JsonValueKind.Array)
				return Array.Empty<long>();

			var ids = new long[nodesProp.GetArrayLength()];
			var i = 0;
			foreach (var n in nodesProp.EnumerateArray())
				ids[i++] = n.GetInt64();
			return ids;
		}

		static OsmRelationMember[] ParseMembers(JsonElement el)
		{
			if (!el.TryGetProperty("members", out var membersProp) || membersProp.ValueKind != JsonValueKind.Array)
				return Array.Empty<OsmRelationMember>();

			var members = new OsmRelationMember[membersProp.GetArrayLength()];
			var i = 0;
			foreach (var m in membersProp.EnumerateArray())
			{
				var type = m.TryGetProperty("type", out var tp) ? tp.GetString() ?? "" : "";
				var refId = m.TryGetProperty("ref", out var rp) ? rp.GetInt64() : 0;
				var role = m.TryGetProperty("role", out var rl) ? rl.GetString() ?? "outer" : "outer";
				members[i++] = new OsmRelationMember(type, refId, role);
			}

			return members;
		}
	}
}
