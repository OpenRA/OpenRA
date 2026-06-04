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
	public enum EditorOppositesMode { Island, Ring }

	sealed class EditorTileMetadata
	{
		const int Slots = 9;
		public const int RingHiddenSlot = 4;
		public const int HorizontalSlot = 9;
		public const int VerticalSlot = 10;
		const int MaxOppositesTemplateSpan = 5;

		readonly Dictionary<TemplateKey, Entry> entries;
		readonly Dictionary<string, ActorEntry> actorEntries;
		readonly Dictionary<OppositesCacheKey, TerrainTemplateInfo[]> oppositesCache = [];

		readonly record struct OppositesCacheKey(TemplateKey Template, EditorOppositesMode Mode);

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
					var slotIsland = ParseSlot(ReadValue(data, "Orientation_island"))
						?? ParseSlot(ReadValue(data, "Orientation"))
						?? ParseSlot(ReadValue(data, "OppositesSlot"));
					var slotRing = ParseSlot(ReadValue(data, "Orientation_ring"));
					var oppositesIsland = ReadList(data, "Opposites_island");
					if (oppositesIsland.Length == 0)
						oppositesIsland = ReadList(data, "Opposites");
					var oppositesRing = ReadList(data, "Opposites_ring");
					var similar = ReadList(data, "Similar");
					var relatedCornersIsland = ReadList(data, "Related_corners_island");
					if (relatedCornersIsland.Length == 0)
						relatedCornersIsland = ReadList(data, "Related_corners");
					var relatedCornersRing = ReadList(data, "Related_corners_ring");

					entries[new TemplateKey(tileset, id)] = new Entry(
						groups, slotIsland, slotRing, oppositesIsland, oppositesRing, similar, relatedCornersIsland, relatedCornersRing);
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

		public TerrainTemplateInfo[] FindOpposites(
			ITemplatedTerrainInfo terrainInfo,
			TerrainTemplateInfo selectedTemplate,
			EditorOppositesMode mode = EditorOppositesMode.Island)
		{
			var cacheKey = new OppositesCacheKey(new TemplateKey(terrainInfo.Id, selectedTemplate.Id), mode);
			if (oppositesCache.TryGetValue(cacheKey, out var cached))
				return cached;

			var result = new TerrainTemplateInfo[Slots];
			var selectedSlot = SlotFor(terrainInfo, selectedTemplate, mode);
			if (selectedSlot == null)
				return oppositesCache[cacheKey] = result;

			if (mode == EditorOppositesMode.Ring && IsCenterOrientationSlot(selectedSlot.Value))
				return oppositesCache[cacheKey] = result;

			result[OppositesGridIndex(selectedSlot.Value)] = selectedTemplate;
			var selectedEntry = EntryFor(terrainInfo, selectedTemplate);
			var unassignedRelatedCorners = new List<TerrainTemplateInfo>();
			var unassignedExplicitOpposites = new List<TerrainTemplateInfo>();

			// Related corners are placed by 3x3 topology, not per-file orientation training.
			// Island: land at center, water outward — a corner cell is the unique junction of one
			// horizontal and one vertical edge. Ring: water at center, land outward — same grid, inverted art.
			var cornerSlots = TopologicalCornerSlotsForRelated(mode, selectedSlot.Value);
			var usedCornerSlots = new HashSet<int>();
			var cornerTemplates = MatchingTemplates(terrainInfo, selectedEntry.RelatedCornerRefs(mode))
				.OrderBy(t => TemplateImageSortKey(t), StringComparer.OrdinalIgnoreCase)
				.ToArray();
			foreach (var template in cornerTemplates)
			{
				if (template.Id == selectedTemplate.Id)
					continue;

				var slot = InferRelatedCornerSlot(terrainInfo, template, selectedSlot.Value, cornerSlots, usedCornerSlots, mode);
				if (slot != null && TryAssignOpposite(result, mode, slot.Value, template))
				{
					usedCornerSlots.Add(slot.Value);
					continue;
				}

				unassignedRelatedCorners.Add(template);
			}

			var cornerSlotIndex = 0;
			foreach (var template in unassignedRelatedCorners.ToArray())
			{
				while (cornerSlotIndex < cornerSlots.Length && usedCornerSlots.Contains(cornerSlots[cornerSlotIndex]))
					cornerSlotIndex++;

				if (cornerSlotIndex >= cornerSlots.Length)
					break;

				if (TryAssignOpposite(result, mode, cornerSlots[cornerSlotIndex], template))
				{
					usedCornerSlots.Add(cornerSlots[cornerSlotIndex]);
					unassignedRelatedCorners.Remove(template);
					cornerSlotIndex++;
				}
			}

			foreach (var template in MatchingTemplates(terrainInfo, selectedEntry.OppositeRefs(mode)))
			{
				if (template.Id == selectedTemplate.Id)
					continue;

				var slot = SlotFor(terrainInfo, template, mode);
				if (slot != null && TryAssignOpposite(result, mode, slot.Value, template))
					continue;

				unassignedExplicitOpposites.Add(template);
			}

			foreach (var fallbackSlot in FallbackOppositeSlots(mode, selectedSlot.Value))
			{
				if (unassignedExplicitOpposites.Count == 0)
					break;

				var template = unassignedExplicitOpposites[0];
				if (TryAssignOpposite(result, mode, fallbackSlot, template))
					unassignedExplicitOpposites.RemoveAt(0);
			}

			foreach (var fallbackSlot in FallbackOppositeSlots(mode, selectedSlot.Value))
			{
				if (unassignedRelatedCorners.Count == 0)
					break;

				var template = unassignedRelatedCorners[0];
				if (TryAssignOpposite(result, mode, fallbackSlot, template))
					unassignedRelatedCorners.RemoveAt(0);
			}

			foreach (var template in terrainInfo.TemplatesInDefinitionOrder)
			{
				if (template.Id == selectedTemplate.Id || !CanShowInOpposites(template))
					continue;

				if (!IsRelated(selectedTemplate, selectedEntry, template, EntryFor(terrainInfo, template)))
					continue;

				var slot = SlotFor(terrainInfo, template, mode);
				if (slot != null)
					TryAssignOpposite(result, mode, slot.Value, template);
			}

			return oppositesCache[cacheKey] = result;
		}

		public static bool IsCenterOrientationSlot(int slot) =>
			slot is RingHiddenSlot or HorizontalSlot or VerticalSlot;

		public static int OppositesGridIndex(int slot) =>
			slot is HorizontalSlot or VerticalSlot ? RingHiddenSlot : slot;

		/// <summary>
		/// Unique 3x3 slots for trained related-corner tiles given the primary tile's slot.
		/// A corner piece links exactly one vertical and one horizontal neighbor; island vs ring
		/// only changes which art belongs in each cell, not which cell index is the junction.
		/// </summary>
		public static int[] TopologicalCornerSlotsForRelated(EditorOppositesMode mode, int primarySlot)
		{
			return primarySlot switch
			{
				// Edge primaries: the two outer corner cells this edge touches.
				1 => [0, 2],
				7 => [6, 8],
				3 => [0, 6],
				5 => [2, 8],
				// Corner primaries: the two edge cells this corner links.
				0 => [1, 3],
				2 => [1, 5],
				6 => [3, 7],
				8 => [5, 7],
				// Ring center split; island center (4) uses the four outer corners.
				HorizontalSlot => [0, 2],
				VerticalSlot => [6, 8],
				RingHiddenSlot or 4 => [0, 2, 6, 8],
				_ => []
			};
		}

		int? InferRelatedCornerSlot(
			ITemplatedTerrainInfo terrainInfo,
			TerrainTemplateInfo template,
			int primarySlot,
			int[] candidateSlots,
			HashSet<int> usedSlots,
			EditorOppositesMode mode)
		{
			if (candidateSlots.Length == 0)
				return null;

			var entry = EntryFor(terrainInfo, template);
			var trainedSlot = entry.Slot(mode);
			if (trainedSlot != null && !usedSlots.Contains(trainedSlot.Value) &&
				candidateSlots.Contains(trainedSlot.Value))
				return trainedSlot;

			if (candidateSlots.Length == 1)
				return usedSlots.Contains(candidateSlots[0]) ? null : candidateSlots[0];

			var inferred = InferCornerSlotFromWater(terrainInfo, template, candidateSlots, primarySlot);
			return inferred != null && !usedSlots.Contains(inferred.Value) ? inferred : null;
		}

		/// <summary>
		/// Picks top-left vs top-right (etc.) from tile art: water/river on the half facing the 3x3 center.
		/// </summary>
		public static int? InferCornerSlotFromWater(
			ITemplatedTerrainInfo terrainInfo,
			TerrainTemplateInfo template,
			int[] candidateSlots,
			int primarySlot)
		{
			if (candidateSlots.Length != 2)
				return null;

			bool? pickFirst = primarySlot switch
			{
				1 or HorizontalSlot => CompareWaterFacingCenter(terrainInfo, template, towardEast: true, towardSouth: true) >=
					CompareWaterFacingCenter(terrainInfo, template, towardEast: false, towardSouth: true),
				7 => CompareWaterFacingCenter(terrainInfo, template, towardEast: true, towardSouth: false) >=
					CompareWaterFacingCenter(terrainInfo, template, towardEast: false, towardSouth: false),
				3 => CompareWaterFacingCenter(terrainInfo, template, towardEast: true, towardSouth: true) >=
					CompareWaterFacingCenter(terrainInfo, template, towardEast: true, towardSouth: false),
				5 => CompareWaterFacingCenter(terrainInfo, template, towardEast: false, towardSouth: true) >
					CompareWaterFacingCenter(terrainInfo, template, towardEast: false, towardSouth: false),
				_ => null
			};

			if (!pickFirst.HasValue)
				return null;

			return pickFirst.Value ? candidateSlots[0] : candidateSlots[1];
		}

		static int CompareWaterFacingCenter(
			ITemplatedTerrainInfo terrainInfo,
			TerrainTemplateInfo template,
			bool towardEast,
			bool towardSouth)
		{
			var count = 0;
			for (var y = 0; y < template.Size.Y; y++)
			{
				for (var x = 0; x < template.Size.X; x++)
				{
					if (towardEast && x < (template.Size.X + 1) / 2)
						continue;
					if (!towardEast && x >= template.Size.X / 2)
						continue;
					if (towardSouth && y < (template.Size.Y + 1) / 2)
						continue;
					if (!towardSouth && y >= template.Size.Y / 2)
						continue;

					if (TileKind(terrainInfo, template, x, y) == TerrainKind.Water)
						count++;
				}
			}

			return count;
		}

		static IEnumerable<int> FallbackOppositeSlots(EditorOppositesMode mode, int selectedSlot)
		{
			if (mode != EditorOppositesMode.Island)
				yield break;

			foreach (var slot in selectedSlot switch
			{
				7 => new[] { 1, 0, 2, 3, 5, 4, 6, 8 },
				1 => new[] { 7, 0, 2, 3, 5, 6, 8, 4 },
				0 => new[] { 8, 1, 2, 7, 5, 3, 4, 6 },
				2 => new[] { 6, 1, 0, 7, 3, 5, 4, 8 },
				6 => new[] { 2, 0, 1, 7, 5, 3, 4, 8 },
				8 => new[] { 0, 1, 2, 7, 3, 5, 4, 6 },
				3 => new[] { 5, 1, 7, 0, 2, 6, 8, 4 },
				5 => new[] { 3, 1, 7, 0, 2, 6, 8, 4 },
				4 => new[] { 1, 3, 5, 7, 0, 2, 6, 8 },
				_ => new[] { 1, 3, 5, 7, 0, 2, 6, 8, 4 }
			})
				yield return slot;
		}

		static bool TryAssignOpposite(TerrainTemplateInfo[] result, EditorOppositesMode mode, int slot, TerrainTemplateInfo template)
		{
			if (!IsOppositesSlotUsed(mode, slot))
				return false;

			var index = OppositesGridIndex(slot);
			if (index < 0 || index >= result.Length || result[index] != null)
				return false;

			result[index] = template;
			return true;
		}

		public static bool IsOppositesSlotUsed(EditorOppositesMode mode, int slot) =>
			mode == EditorOppositesMode.Island || !IsCenterOrientationSlot(slot);

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

			var selectedSlot = SlotFor(terrainInfo, selectedTemplate, EditorOppositesMode.Island);
			if (selectedSlot == null)
				return [selectedTemplate];

			return terrainInfo.TemplatesInDefinitionOrder
				.Where(template => template.Id == selectedTemplate.Id ||
					(CanShowInOpposites(template) &&
					IsRelated(selectedTemplate, selectedEntry, template, EntryFor(terrainInfo, template)) &&
					SlotFor(terrainInfo, template, EditorOppositesMode.Island) == selectedSlot))
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

		static string TemplateImageSortKey(TerrainTemplateInfo template)
		{
			if (template is DefaultTerrainTemplateInfo defaultTemplate && defaultTemplate.Images.Length > 0)
				return defaultTemplate.Images[0];

			return template.Id.ToString(CultureInfo.InvariantCulture);
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

		public static string SlotIndexToName(int slot) => slot switch
		{
			0 => "TopLeft",
			1 => "Top",
			2 => "TopRight",
			3 => "Left",
			4 => "Center",
			5 => "Right",
			6 => "BottomLeft",
			7 => "Bottom",
			8 => "BottomRight",
			HorizontalSlot => "Horizontal",
			VerticalSlot => "Vertical",
			_ => "Center"
		};

		static int? ParseSlot(string value)
		{
			return value?.Trim().ToLowerInvariant() switch
			{
				"top-left" or "topleft" => 0,
				"top" or "top-center" or "topcenter" or "up" => 1,
				"top-right" or "topright" => 2,
				"left" or "middle-left" or "middleleft" => 3,
				"center" => 4,
				"horizontal" => HorizontalSlot,
				"vertical" => VerticalSlot,
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

		public static int? TryParseOrientationSlot(string value) => ParseSlot(value);

		int? SlotFor(ITemplatedTerrainInfo terrainInfo, TerrainTemplateInfo template, EditorOppositesMode mode)
		{
			var entry = EntryFor(terrainInfo, template);
			if (entry.Slot(mode) != null)
				return entry.Slot(mode);

			var topWater = EdgeKind(terrainInfo, template, Edge.Top) == TerrainKind.Water;
			var rightWater = EdgeKind(terrainInfo, template, Edge.Right) == TerrainKind.Water;
			var bottomWater = EdgeKind(terrainInfo, template, Edge.Bottom) == TerrainKind.Water;
			var leftWater = EdgeKind(terrainInfo, template, Edge.Left) == TerrainKind.Water;

			// Corner chirality from outer water edges (toward open water, away from 3x3 center).
			if (topWater && rightWater)
				return 0;
			if (topWater && leftWater)
				return 2;
			if (bottomWater && rightWater)
				return 6;
			if (bottomWater && leftWater)
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
			public static readonly Entry Empty = new([], null, null);

			public readonly HashSet<string> Groups;
			public readonly int? SlotIsland;
			public readonly int? SlotRing;
			public readonly string[] OppositeRefsIsland;
			public readonly string[] OppositeRefsRing;
			public readonly string[] SimilarRefs;
			public readonly string[] RelatedCornerRefsIsland;
			public readonly string[] RelatedCornerRefsRing;

			public Entry(
				HashSet<string> groups,
				int? slotIsland,
				int? slotRing,
				string[] oppositeRefsIsland = null,
				string[] oppositeRefsRing = null,
				string[] similarRefs = null,
				string[] relatedCornerRefsIsland = null,
				string[] relatedCornerRefsRing = null)
			{
				Groups = groups;
				SlotIsland = slotIsland;
				SlotRing = slotRing;
				OppositeRefsIsland = oppositeRefsIsland ?? [];
				OppositeRefsRing = oppositeRefsRing ?? [];
				SimilarRefs = similarRefs ?? [];
				RelatedCornerRefsIsland = relatedCornerRefsIsland ?? [];
				RelatedCornerRefsRing = relatedCornerRefsRing ?? [];
			}

			public int? Slot(EditorOppositesMode mode) => mode == EditorOppositesMode.Ring ? SlotRing : SlotIsland;

			public string[] OppositeRefs(EditorOppositesMode mode) => mode switch
			{
				EditorOppositesMode.Ring => OppositeRefsRing,
				_ => OppositeRefsIsland,
			};

			public string[] RelatedCornerRefs(EditorOppositesMode mode) => mode switch
			{
				EditorOppositesMode.Ring => RelatedCornerRefsRing,
				_ => RelatedCornerRefsIsland,
			};
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
