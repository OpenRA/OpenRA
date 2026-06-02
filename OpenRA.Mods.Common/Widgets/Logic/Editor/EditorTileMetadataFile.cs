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
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.Terrain;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public enum EditorMetadataTrainingKind { None, OppositeIsland, OppositeRing, Similar, OrientationIsland, OrientationRing }

	public enum EditorMetadataTrainingPhase { PickPrimary, PickSecondary, PickOrientationSlot }

	public sealed class EditorTileMetadataFile
	{
		public const string MetadataFilename = "editor-tile-metadata.yaml";
		const string TrainingStatusKey = "TrainingStatus";

		readonly string metadataPath;
		MiniYamlNode[] rootNodes = [];

		public event Action Changed;

		public EditorTileMetadataFile(ModData modData)
		{
			metadataPath = ResolveMetadataPath(modData);
			Reload();
		}

		public bool IsAvailable => metadataPath != null;

		public string[] TemplateColumns { get; private set; } = [];
		public string[] ActorColumns { get; private set; } = [];

		public void Reload()
		{
			if (metadataPath == null || !File.Exists(metadataPath))
			{
				rootNodes = [];
				TemplateColumns = [];
				ActorColumns = [];
				return;
			}

			rootNodes = MiniYaml.FromFile(metadataPath, false).ToArray();
			var general = rootNodes.FirstOrDefault(n => n.Key == "General")?.Value.ToDictionary();
			TemplateColumns = ReadColumns(general, "TemplateColumns");
			ActorColumns = ReadColumns(general, "ActorColumns");
			Changed?.Invoke();
		}

		public IEnumerable<MetadataTemplateRow> TemplateRows(string tilesetId)
		{
			var templates = rootNodes.FirstOrDefault(n => n.Key == "Templates")?.Value;
			if (templates == null)
				yield break;

			foreach (var node in templates.Nodes)
			{
				var data = node.Value.ToDictionary();
				var tileset = ReadValue(data, "Tileset");
				if (!string.Equals(tileset, tilesetId, StringComparison.OrdinalIgnoreCase))
					continue;

				if (!TryGetTemplateId(node.Key, data, out var id))
					continue;

				yield return new MetadataTemplateRow(node.Key, id, data);
			}
		}

		public IEnumerable<MetadataActorRow> ActorRows()
		{
			var actors = rootNodes.FirstOrDefault(n => n.Key == "Actors")?.Value;
			if (actors == null)
				yield break;

			foreach (var node in actors.Nodes)
			{
				var data = node.Value.ToDictionary();
				var actorName = ReadValue(data, "OriginalActorName") ?? ActorNameFromKey(node.Key);
				if (string.IsNullOrWhiteSpace(actorName))
					continue;

				yield return new MetadataActorRow(node.Key, actorName, data);
			}
		}

		public bool IsColumnTrained(IReadOnlyDictionary<string, MiniYaml> data, string column)
		{
			var status = ReadTrainingStatus(data);
			return column switch
			{
				"Opposites_island" => status.Contains("island") || HasList(data, "Opposites_island", "Opposites"),
				"Opposites_ring" => status.Contains("ring") || HasList(data, "Opposites_ring"),
				"Similar" => status.Contains("similar") || HasList(data, "Similar"),
				"Orientation_island" => status.Contains("orientation_island") ||
					HasTrainedOrientationSlot(data, "Orientation_island", "Orientation"),
				"Orientation_ring" => status.Contains("orientation_ring") ||
					HasTrainedOrientationSlot(data, "Orientation_ring"),
				"OppositesSlot" => status.Contains("orientation_island") || status.Contains("orientation") ||
					HasTrainedOrientationSlot(data, "OppositesSlot", "Orientation_island", "Orientation"),
				_ => false
			};
		}

		public string ReadField(IReadOnlyDictionary<string, MiniYaml> data, string key)
		{
			if (key == TrainingStatusKey)
				return FormatTrainingStatus(ReadTrainingStatus(data));

			if (key == "Orientation_island")
				return ReadValue(data, "Orientation_island") ?? ReadValue(data, "Orientation") ?? "";

			if (key == "Orientation_ring")
				return ReadValue(data, "Orientation_ring") ?? "";

			return ReadValue(data, key) ?? "";
		}

		public string ReadOrientationForTraining(IReadOnlyDictionary<string, MiniYaml> data, EditorMetadataTrainingKind mode)
		{
			if (mode == EditorMetadataTrainingKind.OrientationRing)
				return ReadValue(data, "Orientation_ring") ?? "";

			return ReadOrientationIslandDisplay(data);
		}

		public void SaveOppositeIsland(string templateKey, IEnumerable<string> oppositeRefs)
		{
			SaveOppositeIslandMany([templateKey], oppositeRefs);
		}

		public void SaveOppositeRing(string templateKey, IEnumerable<string> oppositeRefs)
		{
			SaveOppositeRingMany([templateKey], oppositeRefs);
		}

		public void SaveOppositeIslandMany(IEnumerable<string> templateKeys, IEnumerable<string> oppositeRefs)
		{
			SaveBidirectionalOppositesMany(templateKeys, oppositeRefs, "Opposites_island", "island");
		}

		public void SaveOppositeRingMany(IEnumerable<string> templateKeys, IEnumerable<string> oppositeRefs)
		{
			SaveBidirectionalOppositesMany(templateKeys, oppositeRefs, "Opposites_ring", "ring");
		}

		void SaveBidirectionalOppositesMany(IEnumerable<string> templateKeys, IEnumerable<string> oppositeRefs, string field, string trainingFlag)
		{
			var changed = false;
			foreach (var templateKey in templateKeys)
				changed |= SaveBidirectionalOppositesCore(templateKey, oppositeRefs, field, trainingFlag);

			if (changed)
				WriteToDisk();
		}

		bool SaveBidirectionalOppositesCore(string templateKey, IEnumerable<string> oppositeRefs, string field, string trainingFlag)
		{
			var oppositeList = oppositeRefs.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			if (!TryUpdateEntry("Templates", templateKey, field, JoinRefs(oppositeList)))
				return false;

			AddTrainingFlag("Templates", templateKey, trainingFlag);

			var primaryRef = GetTemplateReference(templateKey);
			if (string.IsNullOrWhiteSpace(primaryRef))
				return true;

			foreach (var oppositeRef in oppositeList)
			{
				if (!TryFindTemplateKeyByReference(oppositeRef, out var oppositeKey) ||
					string.Equals(oppositeKey, templateKey, StringComparison.Ordinal))
					continue;

				var merged = ParseRefList(ReadTemplateField(oppositeKey, field));
				if (merged.Any(r => string.Equals(r, primaryRef, StringComparison.OrdinalIgnoreCase)))
					continue;

				merged.Add(primaryRef);
				TryUpdateEntry("Templates", oppositeKey, field, JoinRefs(merged));
				AddTrainingFlag("Templates", oppositeKey, trainingFlag);
			}

			return true;
		}

		string GetTemplateReference(string templateKey)
		{
			var data = GetTemplateData(templateKey);
			return data == null ? null : ReadValue(data, "OriginalFilename");
		}

		string ReadTemplateField(string templateKey, string field)
		{
			var data = GetTemplateData(templateKey);
			return data == null ? null : ReadValue(data, field);
		}

		IReadOnlyDictionary<string, MiniYaml> GetTemplateData(string templateKey)
		{
			var templates = rootNodes.FirstOrDefault(n => n.Key == "Templates")?.Value;
			var entry = templates?.Nodes.FirstOrDefault(n => n.Key == templateKey);
			return entry?.Value.ToDictionary();
		}

		bool TryFindTemplateKeyByReference(string reference, out string templateKey)
		{
			templateKey = null;
			var templates = rootNodes.FirstOrDefault(n => n.Key == "Templates")?.Value;
			if (templates == null)
				return false;

			foreach (var node in templates.Nodes)
			{
				var data = node.Value.ToDictionary();
				var filename = ReadValue(data, "OriginalFilename");
				if (!string.IsNullOrEmpty(filename) &&
					string.Equals(filename, reference, StringComparison.OrdinalIgnoreCase))
				{
					templateKey = node.Key;
					return true;
				}

				if (TryGetTemplateId(node.Key, data, out var id) &&
					string.Equals(id.ToString(CultureInfo.InvariantCulture), reference, StringComparison.OrdinalIgnoreCase))
				{
					templateKey = node.Key;
					return true;
				}
			}

			return false;
		}

		static List<string> ParseRefList(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return [];

			return value.Split(',')
				.Select(s => s.Trim())
				.Where(s => s.Length > 0)
				.ToList();
		}

		public void SaveSimilarTemplate(string templateKey, IEnumerable<string> similarRefs)
		{
			SaveSimilarTemplateMany([templateKey], similarRefs);
		}

		public void SaveSimilarActor(string actorKey, IEnumerable<string> similarRefs)
		{
			SaveSimilarActorMany([actorKey], similarRefs);
		}

		public void SaveSimilarTemplateMany(IEnumerable<string> templateKeys, IEnumerable<string> similarRefs)
		{
			var value = JoinRefs(similarRefs);
			var changed = false;
			foreach (var templateKey in templateKeys)
			{
				if (!TryUpdateEntry("Templates", templateKey, "Similar", value))
					continue;

				AddTrainingFlag("Templates", templateKey, "similar");
				changed = true;
			}

			if (changed)
				WriteToDisk();
		}

		public void SaveSimilarActorMany(IEnumerable<string> actorKeys, IEnumerable<string> similarRefs)
		{
			var value = JoinRefs(similarRefs);
			var changed = false;
			foreach (var actorKey in actorKeys)
			{
				if (!TryUpdateEntry("Actors", actorKey, "Similar", value))
					continue;

				AddTrainingFlag("Actors", actorKey, "similar");
				changed = true;
			}

			if (changed)
				WriteToDisk();
		}

		public void SaveOrientationIsland(string templateKey, int slot)
		{
			SaveOrientationIslandMany([templateKey], slot);
		}

		public void SaveOrientationRing(string templateKey, int slot)
		{
			SaveOrientationRingMany([templateKey], slot);
		}

		public void SaveOrientationIslandMany(IEnumerable<string> templateKeys, int slot)
		{
			var slotName = EditorTileMetadata.SlotIndexToName(slot);
			var changed = false;
			foreach (var templateKey in templateKeys)
			{
				if (TryUpdateEntry("Templates", templateKey, "Orientation_island", slotName))
					changed = true;
				AddTrainingFlag("Templates", templateKey, "orientation_island");
				if (TryUpdateEntry("Templates", templateKey, "OppositesSlot", slotName))
					changed = true;
			}

			if (changed)
				WriteToDisk();
		}

		public void SaveOrientationRingMany(IEnumerable<string> templateKeys, int slot)
		{
			var slotName = EditorTileMetadata.SlotIndexToName(slot);
			var changed = false;
			foreach (var templateKey in templateKeys)
			{
				if (TryUpdateEntry("Templates", templateKey, "Orientation_ring", slotName))
					changed = true;
				AddTrainingFlag("Templates", templateKey, "orientation_ring");
			}

			if (changed)
				WriteToDisk();
		}

		public static string TemplateReference(ITemplatedTerrainInfo terrainInfo, TerrainTemplateInfo template, MetadataTemplateRow row)
		{
			if (!string.IsNullOrEmpty(row?.OriginalFilename))
				return row.OriginalFilename;

			if (template is DefaultTerrainTemplateInfo defaultTemplate && defaultTemplate.Images.Length > 0)
				return defaultTemplate.Images[0];

			return template.Id.ToString(CultureInfo.InvariantCulture);
		}

		public static string TemplateKey(string tilesetId, ushort templateId) =>
			$"Template@{tilesetId.ToUpperInvariant()}-{templateId}";

		void UpdateTemplate(string templateKey, string field, string value, string trainingFlag)
		{
			if (!TryUpdateEntry("Templates", templateKey, field, value))
				return;

			AddTrainingFlag("Templates", templateKey, trainingFlag);
			WriteToDisk();
		}

		void UpdateTemplateFieldOnly(string templateKey, string field, string value)
		{
			if (TryUpdateEntry("Templates", templateKey, field, value))
				WriteToDisk();
		}

		void UpdateActor(string actorKey, string field, string value, string trainingFlag)
		{
			if (!TryUpdateEntry("Actors", actorKey, field, value))
				return;

			AddTrainingFlag("Actors", actorKey, trainingFlag);
			WriteToDisk();
		}

		void AddTrainingFlag(string section, string entryKey, string flag)
		{
			var sectionNode = rootNodes.FirstOrDefault(n => n.Key == section);
			if (sectionNode == null)
				return;

			var entry = sectionNode.Value.Nodes.FirstOrDefault(n => n.Key == entryKey);
			if (entry == null)
				return;

			var data = entry.Value.ToDictionary();
			var status = ReadTrainingStatus(data);
			status.Add(flag);
			TryUpdateEntry(section, entryKey, TrainingStatusKey, FormatTrainingStatus(status));
		}

		bool TryUpdateEntry(string section, string entryKey, string field, string value)
		{
			var sectionIndex = -1;
			for (var i = 0; i < rootNodes.Length; i++)
			{
				if (rootNodes[i].Key == section)
				{
					sectionIndex = i;
					break;
				}
			}

			if (sectionIndex < 0)
				return false;

			var sectionNode = rootNodes[sectionIndex];
			var entryIndex = -1;
			for (var i = 0; i < sectionNode.Value.Nodes.Length; i++)
			{
				if (sectionNode.Value.Nodes[i].Key == entryKey)
				{
					entryIndex = i;
					break;
				}
			}
			if (entryIndex < 0)
				return false;

			var entry = sectionNode.Value.Nodes[entryIndex];
			var nodes = entry.Value.Nodes.ToList();
			var fieldIndex = nodes.FindIndex(n => n.Key == field);
			if (fieldIndex >= 0)
				nodes[fieldIndex] = new MiniYamlNode(field, value ?? "");
			else
				nodes.Add(new MiniYamlNode(field, value ?? ""));

			var updatedEntry = entry.WithValue(entry.Value.WithNodes(nodes));
			var updatedEntries = sectionNode.Value.Nodes.ToArray();
			updatedEntries[entryIndex] = updatedEntry;
			rootNodes[sectionIndex] = sectionNode.WithValue(sectionNode.Value.WithNodes(updatedEntries));
			return true;
		}

		void WriteToDisk()
		{
			if (metadataPath == null)
				return;

			rootNodes.WriteToFile(metadataPath);
			Reload();
		}

		static string ResolveMetadataPath(ModData modData)
		{
			if (modData.Manifest.Package is Folder folder)
			{
				var path = Path.Combine(folder.Name, MetadataFilename);
				if (File.Exists(path))
					return path;
			}

			if (modData.Manifest.Package is Folder packageFolder)
				return Path.Combine(packageFolder.Name, MetadataFilename);

			return null;
		}

		static string[] ReadColumns(Dictionary<string, MiniYaml> general, string key)
		{
			if (general == null || !general.TryGetValue(key, out var yaml) || string.IsNullOrWhiteSpace(yaml.Value))
				return [];

			return yaml.Value.Split(',')
				.Select(s => s.Trim())
				.Where(s => s.Length > 0)
				.ToArray();
		}

		static string ReadValue(IReadOnlyDictionary<string, MiniYaml> data, string key)
		{
			return data.TryGetValue(key, out var yaml) ? yaml.Value : null;
		}

		static bool HasList(IReadOnlyDictionary<string, MiniYaml> data, params string[] keys)
		{
			foreach (var key in keys)
			{
				if (data.TryGetValue(key, out var yaml) && !string.IsNullOrWhiteSpace(yaml.Value))
					return true;
			}

			return false;
		}

		static string ReadOrientationIslandDisplay(IReadOnlyDictionary<string, MiniYaml> data)
		{
			var island = ReadValue(data, "Orientation_island");
			if (!string.IsNullOrWhiteSpace(island))
				return island;

			return ReadValue(data, "Orientation") ?? ReadValue(data, "OppositesSlot") ?? "";
		}

		static bool HasTrainedOrientationSlot(IReadOnlyDictionary<string, MiniYaml> data, params string[] keys)
		{
			foreach (var key in keys)
			{
				if (EditorTileMetadata.TryParseOrientationSlot(ReadValue(data, key)).HasValue)
					return true;
			}

			return false;
		}

		static HashSet<string> ReadTrainingStatus(IReadOnlyDictionary<string, MiniYaml> data)
		{
			if (!data.TryGetValue(TrainingStatusKey, out var yaml) || string.IsNullOrWhiteSpace(yaml.Value))
				return [];

			return yaml.Value.Split(',')
				.Select(s => s.Trim().ToLowerInvariant())
				.Where(s => s.Length > 0)
				.ToHashSet(StringComparer.Ordinal);
		}

		static string FormatTrainingStatus(HashSet<string> status) =>
			string.Join(", ", status.OrderBy(s => s, StringComparer.Ordinal));

		static string JoinRefs(IEnumerable<string> refs) =>
			string.Join(", ", refs.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct(StringComparer.OrdinalIgnoreCase));

		static bool TryGetTemplateId(string key, Dictionary<string, MiniYaml> data, out ushort id)
		{
			if (data.TryGetValue("TemplateId", out var templateIdYaml) &&
				ushort.TryParse(templateIdYaml.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
				return true;

			var dash = key.LastIndexOf('-');
			if (dash >= 0 && ushort.TryParse(key[(dash + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
				return true;

			id = 0;
			return false;
		}

		static string ActorNameFromKey(string key)
		{
			var at = key.LastIndexOf('@');
			return at >= 0 ? key[(at + 1)..] : key;
		}
	}

	public sealed class MetadataTemplateRow
	{
		public readonly string Key;
		public readonly ushort TemplateId;
		public readonly IReadOnlyDictionary<string, MiniYaml> Data;

		public string OriginalFilename => ReadValue(Data, "OriginalFilename");

		public MetadataTemplateRow(string key, ushort templateId, IReadOnlyDictionary<string, MiniYaml> data)
		{
			Key = key;
			TemplateId = templateId;
			Data = data;
		}

		static string ReadValue(IReadOnlyDictionary<string, MiniYaml> data, string key)
		{
			return data.TryGetValue(key, out var yaml) ? yaml.Value : null;
		}
	}

	public sealed class MetadataActorRow
	{
		public readonly string Key;
		public readonly string ActorName;
		public readonly IReadOnlyDictionary<string, MiniYaml> Data;

		public MetadataActorRow(string key, string actorName, IReadOnlyDictionary<string, MiniYaml> data)
		{
			Key = key;
			ActorName = actorName;
			Data = data;
		}
	}
}
