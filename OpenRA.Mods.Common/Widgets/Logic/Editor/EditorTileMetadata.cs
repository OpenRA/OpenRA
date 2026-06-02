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
using System.Globalization;
using System.Linq;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	sealed class EditorTileMetadata
	{
		const int Slots = 9;
		const int MaxOppositesTemplateSpan = 5;

		readonly Dictionary<TemplateKey, Entry> entries;
		readonly Dictionary<string, ActorEntry> actorEntries;
		readonly Dictionary<TemplateKey, TerrainTemplateInfo[]> oppositesCache = [];

		EditorTileMetadata(Dictionary<TemplateKey, Entry> entries, Dictionary<string, ActorEntry> actorEntries)
		{
			this.entries = entries;
			this.actorEntries = actorEntries;
		}

		public static EditorTileMetadata Load(ModData modData, ITemplatedTerrainInfo terrainInfo)
		{
			if (modData == null || terrainInfo == null)
				return new EditorTileMetadata([], []);

			var filename = $"{modData.Manifest.Id}|editor-tile-metadata.yaml";
			if (!modData.DefaultFileSystem.Exists(filename))
				return new EditorTileMetadata([], []);

			var yaml = new MiniYaml(null, MiniYaml.FromStream(modData.DefaultFileSystem.Open(filename), filename))
				.ToDictionary();
			var entries = new Dictionary<TemplateKey, Entry>();
			if (yaml.TryGetValue("Templates", out var templateYaml))
			{
				foreach (var node in templateYaml.Nodes)
				{
					var data = node.Value.ToDictionary();
					if (!TryGetTemplateId(node.Key, data, out var id))
						continue;

					var tileset = ReadValue(data, "Tileset");
					var groups = ReadCsv(data, "OppositesGroup");
					var slot = ParseSlot(ReadValue(data, "Orientation")) ?? ParseSlot(ReadValue(data, "OppositesSlot"));
					var opposites = ReadList(data, "Opposites");
					var similar = ReadList(data, "Similar");

					entries[new TemplateKey(tileset, id)] = new Entry(groups, slot, opposites, similar);
				}
			}

			var actorEntries = new Dictionary<string, ActorEntry>(StringComparer.OrdinalIgnoreCase);
			if (yaml.TryGetValue("Actors", out var actorYaml))
			{
				foreach (var node in actorYaml.Nodes)
				{
					var data = node.Value.ToDictionary();
					var actorName = ReadValue(data, "OriginalActorName") ?? ActorNameFromKey(node.Key);
					if (string.IsNullOrWhiteSpace(actorName))
						continue;

					var category = ReadValue(data, "SimilarGroup") ?? ReadValue(data, "Category");
					actorEntries[actorName] = new ActorEntry(category, ReadList(data, "Similar"));
				}
			}

			return new EditorTileMetadata(entries, actorEntries);
		}

		public TerrainTemplateInfo[] FindOpposites(ITemplatedTerrainInfo terrainInfo, TerrainTemplateInfo selectedTemplate)
		{
			var cacheKey = new TemplateKey(terrainInfo.Id, selectedTemplate.Id);
			if (oppositesCache.TryGetValue(cacheKey, out var cached))
				return cached;

			var result = new TerrainTemplateInfo[Slots];
			var selectedSlot = SlotFor(terrainInfo, selectedTemplate);
			if (selectedSlot == null)
				return oppositesCache[cacheKey] = result;

			result[selectedSlot.Value] = selectedTemplate;
			var selectedEntry = EntryFor(terrainInfo, selectedTemplate);

			foreach (var template in MatchingTemplates(terrainInfo, selectedEntry.OppositeRefs))
			{
				var slot = SlotFor(terrainInfo, template);
				if (slot != null && result[slot.Value] == null)
					result[slot.Value] = template;
			}

			foreach (var template in terrainInfo.TemplatesInDefinitionOrder)
			{
				if (template.Id == selectedTemplate.Id || !CanShowInOpposites(template))
					continue;

				if (!IsRelated(selectedTemplate, selectedEntry, template, EntryFor(terrainInfo, template)))
					continue;

				var slot = SlotFor(terrainInfo, template);
				if (slot != null && result[slot.Value] == null)
					result[slot.Value] = template;
			}

			return oppositesCache[cacheKey] = result;
		}

		public TerrainTemplateInfo[] FindSimilar(ITemplatedTerrainInfo terrainInfo, TerrainTemplateInfo selectedTemplate)
		{
			return FindSimilarGroup(terrainInfo, selectedTemplate)
				.Where(template => template.Id != selectedTemplate.Id)
				.ToArray();
		}

		public TerrainTemplateInfo[] FindSimilarGroup(ITemplatedTerrainInfo terrainInfo, TerrainTemplateInfo selectedTemplate)
		{
			var selectedEntry = EntryFor(terrainInfo, selectedTemplate);
			var explicitSimilar = MatchingTemplates(terrainInfo, selectedEntry.SimilarRefs).ToArray();
			if (explicitSimilar.Length > 0)
				return [selectedTemplate, .. explicitSimilar];

			var selectedSlot = SlotFor(terrainInfo, selectedTemplate);
			if (selectedSlot == null)
				return [selectedTemplate];

			return terrainInfo.TemplatesInDefinitionOrder
				.Where(template => template.Id == selectedTemplate.Id ||
					(CanShowInOpposites(template) &&
					IsRelated(selectedTemplate, selectedEntry, template, EntryFor(terrainInfo, template)) &&
					SlotFor(terrainInfo, template) == selectedSlot))
				.ToArray();
		}

		public ActorInfo[] FindSimilarActors(Ruleset rules, ActorInfo selectedActor)
		{
			return FindSimilarActorGroup(rules, selectedActor)
				.Where(actor => actor != selectedActor)
				.ToArray();
		}

		public ActorInfo[] FindSimilarActorGroup(Ruleset rules, ActorInfo selectedActor)
		{
			var selectedEntry = ActorEntryFor(selectedActor);
			var explicitSimilar = MatchingActors(rules, selectedEntry.SimilarRefs).ToArray();
			if (explicitSimilar.Length > 0)
				return [selectedActor, .. explicitSimilar];

			if (string.IsNullOrWhiteSpace(selectedEntry.Category))
				return [selectedActor];

			return rules.Actors.Values
				.Where(actor => actor == selectedActor || ActorEntryFor(actor).Matches(selectedEntry))
				.ToArray();
		}

		Entry EntryFor(ITemplatedTerrainInfo terrainInfo, TerrainTemplateInfo template)
		{
			if (entries.TryGetValue(new TemplateKey(terrainInfo.Id, template.Id), out var entry))
				return entry;

			return entries.TryGetValue(new TemplateKey(null, template.Id), out entry) ? entry : Entry.Empty;
		}

		static string ReadValue(Dictionary<string, MiniYaml> data, string key)
		{
			return data.TryGetValue(key, out var yaml) ? yaml.Value : null;
		}

		static string ActorNameFromKey(string key)
		{
			var at = key.LastIndexOf('@');
			return at >= 0 ? key[(at + 1)..] : key;
		}

		static bool TryGetTemplateId(string key, Dictionary<string, MiniYaml> data, out ushort id)
		{
			if (data.TryGetValue("TemplateId", out var templateIdYaml) &&
				ushort.TryParse(templateIdYaml.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
				return true;

			var at = key.LastIndexOf('@');
			if (at >= 0 && ushort.TryParse(key[(at + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
				return true;

			id = 0;
			return false;
		}

		static HashSet<string> ReadCsv(Dictionary<string, MiniYaml> data, string key)
		{
			if (!data.TryGetValue(key, out var yaml) || string.IsNullOrWhiteSpace(yaml.Value))
				return [];

			return yaml.Value.Split(',')
				.Select(s => s.Trim())
				.Where(s => s.Length > 0)
				.ToHashSet(StringComparer.OrdinalIgnoreCase);
		}

		static string[] ReadList(Dictionary<string, MiniYaml> data, string key)
		{
			if (!data.TryGetValue(key, out var yaml) || string.IsNullOrWhiteSpace(yaml.Value))
				return [];

			return yaml.Value.Split(',')
				.Select(s => s.Trim())
				.Where(s => s.Length > 0)
				.ToArray();
		}

		IEnumerable<TerrainTemplateInfo> MatchingTemplates(ITemplatedTerrainInfo terrainInfo, IReadOnlyList<string> references)
		{
			if (references.Count == 0)
				yield break;

			foreach (var reference in references)
			{
				foreach (var template in terrainInfo.TemplatesInDefinitionOrder)
				{
					if (TemplateMatchesReference(template, reference))
					{
						yield return template;
						break;
					}
				}
			}
		}

		IEnumerable<ActorInfo> MatchingActors(Ruleset rules, IReadOnlyList<string> references)
		{
			if (references.Count == 0)
				yield break;

			foreach (var reference in references)
			{
				var key = reference.ToLowerInvariant();
				if (rules.Actors.TryGetValue(key, out var actor))
					yield return actor;
			}
		}

		ActorEntry ActorEntryFor(ActorInfo actor)
		{
			if (actorEntries.TryGetValue(actor.Name, out var entry))
				return entry;

			var editorData = actor.TraitInfoOrDefault<MapEditorDataInfo>();
			return new ActorEntry(editorData?.Categories.FirstOrDefault(), []);
		}

		static bool TemplateMatchesReference(TerrainTemplateInfo template, string reference)
		{
			if (ushort.TryParse(reference, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && template.Id == id)
				return true;

			if (reference.StartsWith("Template@", StringComparison.OrdinalIgnoreCase))
			{
				var suffix = reference[(reference.LastIndexOf('-') + 1)..];
				if (ushort.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out id) && template.Id == id)
					return true;
			}

			return template is DefaultTerrainTemplateInfo defaultTemplate &&
				defaultTemplate.Images.Any(image => string.Equals(image, reference, StringComparison.OrdinalIgnoreCase));
		}

		static int? ParseSlot(string value)
		{
			return value?.Trim().ToLowerInvariant() switch
			{
				"top-left" or "topleft" => 0,
				"top" or "top-center" or "topcenter" or "up" => 1,
				"top-right" or "topright" => 2,
				"left" or "middle-left" or "middleleft" => 3,
				"center" or "horizontal" or "vertical" => 4,
				"right" or "middle-right" or "middleright" => 5,
				"bottom-left" or "bottomleft" => 6,
				"bottom" or "bottom-center" or "bottomcenter" or "down" => 7,
				"bottom-right" or "bottomright" => 8,
				"up-left" or "upleft" => 0,
				"up-right" or "upright" => 2,
				"down-left" or "downleft" => 6,
				"down-right" or "downright" => 8,
				"end-left" or "endleft" => 3,
				"end-right" or "endright" => 5,
				"end-up" or "endup" => 1,
				"end-down" or "enddown" => 7,
				_ => null
			};
		}

		static bool CanShowInOpposites(TerrainTemplateInfo template)
		{
			return !template.PickAny
				&& template.Size.X <= MaxOppositesTemplateSpan
				&& template.Size.Y <= MaxOppositesTemplateSpan;
		}

		static bool IsRelated(TerrainTemplateInfo selectedTemplate, Entry selectedEntry, TerrainTemplateInfo template, Entry entry)
		{
			if (selectedEntry.Groups.Count > 0 || entry.Groups.Count > 0)
				return selectedEntry.Groups.Overlaps(entry.Groups);

			return selectedTemplate.Categories.Any(c => template.Categories.Contains(c));
		}

		int? SlotFor(ITemplatedTerrainInfo terrainInfo, TerrainTemplateInfo template)
		{
			var entry = EntryFor(terrainInfo, template);
			if (entry.Slot != null)
				return entry.Slot;

			var topWater = EdgeKind(terrainInfo, template, Edge.Top) == TerrainKind.Water;
			var rightWater = EdgeKind(terrainInfo, template, Edge.Right) == TerrainKind.Water;
			var bottomWater = EdgeKind(terrainInfo, template, Edge.Bottom) == TerrainKind.Water;
			var leftWater = EdgeKind(terrainInfo, template, Edge.Left) == TerrainKind.Water;

			if (topWater && leftWater)
				return 0;
			if (topWater && rightWater)
				return 2;
			if (bottomWater && leftWater)
				return 6;
			if (bottomWater && rightWater)
				return 8;
			if (topWater)
				return 1;
			if (leftWater)
				return 3;
			if (rightWater)
				return 5;
			if (bottomWater)
				return 7;

			return null;
		}

		static TerrainKind EdgeKind(ITemplatedTerrainInfo terrainInfo, TerrainTemplateInfo template, Edge edge)
		{
			var primaryLength = edge is Edge.Top or Edge.Bottom ? template.Size.Y : template.Size.X;
			var secondaryLength = edge is Edge.Top or Edge.Bottom ? template.Size.X : template.Size.Y;

			for (var primary = 0; primary < primaryLength; primary++)
			{
				var land = 0;
				var water = 0;
				for (var secondary = 0; secondary < secondaryLength; secondary++)
				{
					var x = edge switch
					{
						Edge.Left => primary,
						Edge.Right => template.Size.X - 1 - primary,
						_ => secondary
					};
					var y = edge switch
					{
						Edge.Top => primary,
						Edge.Bottom => template.Size.Y - 1 - primary,
						_ => secondary
					};

					var kind = TileKind(terrainInfo, template, x, y);
					if (kind == TerrainKind.Land)
						land++;
					else if (kind == TerrainKind.Water)
						water++;
				}

				if (land > water)
					return TerrainKind.Land;
				if (water > land)
					return TerrainKind.Water;
			}

			return TerrainKind.Unknown;
		}

		static TerrainKind TileKind(ITemplatedTerrainInfo terrainInfo, TerrainTemplateInfo template, int x, int y)
		{
			var index = y * template.Size.X + x;
			if (!template.Contains(index) || template[index] == null)
				return TerrainKind.Unknown;

			var type = terrainInfo.TerrainTypes[template[index].TerrainType].Type;
			return type is "Water" or "River" ? TerrainKind.Water : TerrainKind.Land;
		}

		readonly struct Entry
		{
			public static readonly Entry Empty = new([], null);

			public readonly HashSet<string> Groups;
			public readonly int? Slot;
			public readonly string[] OppositeRefs;
			public readonly string[] SimilarRefs;

			public Entry(HashSet<string> groups, int? slot, string[] oppositeRefs = null, string[] similarRefs = null)
			{
				Groups = groups;
				Slot = slot;
				OppositeRefs = oppositeRefs ?? [];
				SimilarRefs = similarRefs ?? [];
			}
		}

		readonly struct ActorEntry
		{
			public readonly string Category;
			public readonly string[] SimilarRefs;

			public ActorEntry(string category, string[] similarRefs)
			{
				Category = category;
				SimilarRefs = similarRefs ?? [];
			}

			public bool Matches(ActorEntry other)
			{
				return !string.IsNullOrWhiteSpace(Category) &&
					string.Equals(Category, other.Category, StringComparison.OrdinalIgnoreCase);
			}
		}

		readonly struct TemplateKey : IEquatable<TemplateKey>
		{
			readonly string tileset;
			readonly ushort id;

			public TemplateKey(string tileset, ushort id)
			{
				this.tileset = tileset?.ToUpperInvariant();
				this.id = id;
			}

			public bool Equals(TemplateKey other)
			{
				return string.Equals(tileset, other.tileset, StringComparison.Ordinal) && id == other.id;
			}

			public override bool Equals(object obj)
			{
				return obj is TemplateKey other && Equals(other);
			}

			public static bool operator ==(TemplateKey left, TemplateKey right)
			{
				return left.Equals(right);
			}

			public static bool operator !=(TemplateKey left, TemplateKey right)
			{
				return !left.Equals(right);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(tileset, id);
			}
		}

		enum Edge { Top, Right, Bottom, Left }
		enum TerrainKind { Unknown, Land, Water }
	}
}
