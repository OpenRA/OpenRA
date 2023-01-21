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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class ExplicitSequenceFilenames : UpdateRule
	{
		public override string Name => "Sequence filenames must be specified explicitly.";

		public override string Description =>
			"Sequence sprite filenames are no longer automatically inferred, and the AddExtension,\n" +
			"UseTilesetExtension, UseTilesetNodes and TilesetOverrides fields have been removed.\n\n" +
			"The sprite filename for each sequence must now be defined using the Filename field.\n" +
			"Tileset specific overrides can be defined as children of the TilesetFilenames field.";

		string defaultSpriteExtension = ".shp";
		List<MiniYamlNode> resolvedImagesNodes;
		readonly Dictionary<string, string> tilesetExtensions = new Dictionary<string, string>();
		readonly Dictionary<string, string> tilesetCodes = new Dictionary<string, string>();
		bool parseModYaml = true;
		bool reportModYamlChanges;
		bool disabled;

		public override IEnumerable<string> BeforeUpdateSequences(ModData modData, List<MiniYamlNode> resolvedImagesNodes)
		{
			// Keep a resolved copy of the sequences so we can account for values imported through inheritance or Defaults.
			// This will be modified during processing, so take a deep copy to avoid side-effects on other update rules.
			this.resolvedImagesNodes = MiniYaml.FromString(resolvedImagesNodes.WriteToString());

			var requiredMetadata = new HashSet<string>();
			foreach (var imageNode in resolvedImagesNodes)
			{
				foreach (var sequenceNode in imageNode.Value.Nodes)
				{
					var useTilesetExtensionNode = sequenceNode.LastChildMatching("UseTilesetExtension");
					if (useTilesetExtensionNode != null && !tilesetExtensions.Any())
						requiredMetadata.Add("TilesetExtensions");

					var useTilesetCodeNode = sequenceNode.LastChildMatching("UseTilesetCode");
					if (useTilesetCodeNode != null && !tilesetCodes.Any())
						requiredMetadata.Add("TilesetCodes");
				}
			}

			if (requiredMetadata.Any())
			{
				yield return $"The ExplicitSequenceFilenames rule requires {requiredMetadata.JoinWith(", ")}\n" +
				             "to be defined under the SpriteSequenceFormat definition in mod.yaml.\n" +
				             "Add these definitions back and run the update rule again.";
				disabled = true;
			}
		}

		public override IEnumerable<string> BeforeUpdate(ModData modData)
		{
			// Don't reload data when processing maps
			if (!parseModYaml)
				yield break;

			parseModYaml = false;

			// HACK: We need to read the obsolete yaml definitions to be able to update the sequences
			// TilesetSpecificSpriteSequence no longer defines fields for these, so we must take them directly from mod.yaml
			var yamlField = modData.Manifest.GetType().GetField("yaml", BindingFlags.Instance | BindingFlags.NonPublic);
			var yaml = (Dictionary<string, MiniYaml>)yamlField?.GetValue(modData.Manifest);

			if (yaml != null && yaml.TryGetValue("SpriteSequenceFormat", out var spriteSequenceFormatYaml))
			{
				if (spriteSequenceFormatYaml.Value == "DefaultSpriteSequence")
				{
					defaultSpriteExtension = "";
					yield break;
				}

				var spriteSequenceFormatNode = new MiniYamlNode("", spriteSequenceFormatYaml);
				var defaultSpriteExtensionNode = spriteSequenceFormatNode.LastChildMatching("DefaultSpriteExtension");
				if (defaultSpriteExtensionNode != null)
				{
					reportModYamlChanges = true;
					defaultSpriteExtension = defaultSpriteExtensionNode.Value.Value;
				}

				var tilesetExtensionsNode = spriteSequenceFormatNode.LastChildMatching("TilesetExtensions");
				if (tilesetExtensionsNode != null)
				{
					reportModYamlChanges = true;
					foreach (var n in tilesetExtensionsNode.Value.Nodes)
						tilesetExtensions[n.Key] = n.Value.Value;
				}

				var tilesetCodesNode = spriteSequenceFormatNode.LastChildMatching("TilesetCodes");
				if (tilesetCodesNode != null)
				{
					reportModYamlChanges = true;
					foreach (var n in tilesetCodesNode.Value.Nodes)
						tilesetCodes[n.Key] = n.Value.Value;
				}
			}
		}

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (!reportModYamlChanges)
				yield break;

			yield return "The DefaultSpriteExtension, TilesetExtensions, and TilesetCodes fields defined\n" +
				"under SpriteSequenceFormat in your mod.yaml are no longer used, and can be removed.";

			reportModYamlChanges = false;
		}

		public override IEnumerable<string> UpdateSequenceNode(ModData modData, MiniYamlNode imageNode)
		{
			if (disabled)
				yield break;

			var resolvedImageNode = resolvedImagesNodes.Single(n => n.Key == imageNode.Key);

			// Add a placeholder for inherited sequences that were previously implicitly named
			var implicitInheritedSequences = new List<string>();
			foreach (var resolvedSequenceNode in resolvedImageNode.Value.Nodes)
			{
				if (resolvedSequenceNode.Key != "Defaults" && string.IsNullOrEmpty(resolvedSequenceNode.Value.Value) &&
				    imageNode.LastChildMatching(resolvedSequenceNode.Key) == null)
				{
					imageNode.AddNode(resolvedSequenceNode.Key, "");
					implicitInheritedSequences.Add(resolvedSequenceNode.Key);
				}
			}

			var resolvedDefaultsNode = resolvedImageNode.LastChildMatching("Defaults");
			if (resolvedDefaultsNode != null)
			{
				foreach (var resolvedSequenceNode in resolvedImageNode.Value.Nodes)
				{
					if (resolvedSequenceNode == resolvedDefaultsNode)
						continue;

					resolvedSequenceNode.Value.Nodes = MiniYaml.Merge(new[] { resolvedDefaultsNode.Value.Nodes, resolvedSequenceNode.Value.Nodes });
					resolvedSequenceNode.Value.Value = resolvedSequenceNode.Value.Value ?? resolvedDefaultsNode.Value.Value;
				}
			}

			// Sequences that explicitly defined a filename may be inherited by others that depend on the explicit name
			// Keep track of these sequences so we don't remove the filenames later!
			var explicitlyNamedSequences = new List<string>();

			// Add Filename/TilesetFilenames nodes to every sequence
			foreach (var sequenceNode in imageNode.Value.Nodes)
			{
				if (string.IsNullOrEmpty(sequenceNode.Key) || sequenceNode.KeyMatches("Inherits"))
					continue;

				if (!string.IsNullOrEmpty(sequenceNode.Value.Value))
					explicitlyNamedSequences.Add(sequenceNode.Key);

				var resolvedSequenceNode = resolvedImageNode.Value.Nodes.SingleOrDefault(n => n.Key == sequenceNode.Key);
				if (resolvedSequenceNode == null)
					continue;

				ProcessNode(modData, sequenceNode, resolvedSequenceNode, imageNode.Key);
			}

			// Identify a suitable default for deduplication
			MiniYamlNode defaultFilenameNode = null;
			MiniYamlNode defaultTilesetFilenamesNode = null;
			foreach (var defaultsNode in imageNode.ChildrenMatching("Defaults"))
			{
				defaultFilenameNode = defaultsNode.LastChildMatching("Filename") ?? defaultFilenameNode;
				defaultTilesetFilenamesNode = defaultsNode.LastChildMatching("TilesetFilenames") ?? defaultTilesetFilenamesNode;
			}

			if ((defaultFilenameNode == null || defaultTilesetFilenamesNode == null) && !imageNode.Key.StartsWith("^"))
			{
				var duplicateCount = new Dictionary<string, int>();
				var duplicateTilesetCount = new Dictionary<string, int>();

				foreach (var sequenceNode in imageNode.Value.Nodes)
				{
					if (string.IsNullOrEmpty(sequenceNode.Key) || explicitlyNamedSequences.Contains(sequenceNode.Key))
						continue;

					var tilesetFilenamesNode = sequenceNode.LastChildMatching("TilesetFilenames");
					if (defaultTilesetFilenamesNode == null && tilesetFilenamesNode != null)
					{
						var key = tilesetFilenamesNode.Value.Nodes.WriteToString();
						duplicateTilesetCount[key] = duplicateTilesetCount.GetValueOrDefault(key, 0) + 1;
					}

					var filenameNode = sequenceNode.LastChildMatching("Filename");
					if (defaultFilenameNode == null && filenameNode != null)
					{
						var key = filenameNode.Value.Value;
						duplicateCount[key] = duplicateCount.GetValueOrDefault(key, 0) + 1;
					}
				}

				var maxDuplicateTilesetCount = duplicateTilesetCount.MaxByOrDefault(kv => kv.Value).Value;
				if (maxDuplicateTilesetCount > 1)
				{
					if (imageNode.LastChildMatching("Defaults") == null)
						imageNode.Value.Nodes.Insert(0, new MiniYamlNode("Defaults", ""));

					var nodes = MiniYaml.FromString(duplicateTilesetCount.First(kv => kv.Value == maxDuplicateTilesetCount).Key);
					defaultTilesetFilenamesNode = new MiniYamlNode("TilesetFilenames", "", nodes);
					imageNode.LastChildMatching("Defaults").Value.Nodes.Insert(0, defaultTilesetFilenamesNode);
				}

				var maxDuplicateCount = duplicateCount.MaxByOrDefault(kv => kv.Value).Value;
				if (maxDuplicateCount > 1)
				{
					if (imageNode.LastChildMatching("Defaults") == null)
						imageNode.Value.Nodes.Insert(0, new MiniYamlNode("Defaults", ""));

					defaultFilenameNode = new MiniYamlNode("Filename", duplicateCount.First(kv => kv.Value == maxDuplicateCount).Key);
					imageNode.LastChildMatching("Defaults").Value.Nodes.Insert(0, defaultFilenameNode);
				}
			}

			// Remove redundant definitions
			foreach (var sequenceNode in imageNode.Value.Nodes.ToList())
			{
				if (sequenceNode.Key == "Defaults" || sequenceNode.Key == "Inherits" || string.IsNullOrEmpty(sequenceNode.Key))
					continue;

				var combineNode = sequenceNode.LastChildMatching("Combine");
				var filenameNode = sequenceNode.LastChildMatching("Filename");
				var tilesetFilenamesNode = sequenceNode.LastChildMatching("TilesetFilenames");

				if (defaultTilesetFilenamesNode != null && combineNode != null)
					sequenceNode.Value.Nodes.Insert(0, new MiniYamlNode("TilesetFilenames", ""));

				if (defaultFilenameNode != null && combineNode != null)
					sequenceNode.Value.Nodes.Insert(0, new MiniYamlNode("Filename", ""));

				if (defaultTilesetFilenamesNode != null && tilesetFilenamesNode == null && filenameNode != null)
				{
					var index = sequenceNode.Value.Nodes.IndexOf(filenameNode) + 1;
					sequenceNode.Value.Nodes.Insert(index, new MiniYamlNode("TilesetFilenames", ""));
				}

				// Remove redundant overrides
				if (!explicitlyNamedSequences.Contains(sequenceNode.Key))
				{
					if (defaultTilesetFilenamesNode != null && tilesetFilenamesNode != null)
					{
						var allTilesetsMatch = true;
						foreach (var overrideNode in tilesetFilenamesNode.Value.Nodes)
							if (!defaultTilesetFilenamesNode.Value.Nodes.Any(n => n.Key == overrideNode.Key && n.Value.Value == overrideNode.Value.Value))
								allTilesetsMatch = false;

						if (allTilesetsMatch)
							sequenceNode.RemoveNode(tilesetFilenamesNode);
					}

					if (filenameNode?.Value.Value != null && filenameNode?.Value.Value == defaultFilenameNode?.Value.Value)
						sequenceNode.RemoveNode(filenameNode);
				}
			}

			var allSequencesHaveFilename = true;
			var allSequencesHaveTilesetFilenames = true;
			foreach (var sequenceNode in imageNode.Value.Nodes.ToList())
			{
				if (sequenceNode.Key == "Defaults" || sequenceNode.Key == "Inherits" || string.IsNullOrEmpty(sequenceNode.Key))
					continue;

				if (sequenceNode.LastChildMatching("Filename") == null)
					allSequencesHaveFilename = false;

				if (sequenceNode.LastChildMatching("TilesetFilenames") == null)
					allSequencesHaveTilesetFilenames = false;
			}

			if (allSequencesHaveFilename || allSequencesHaveTilesetFilenames)
			{
				foreach (var sequenceNode in imageNode.Value.Nodes.ToList())
				{
					if (sequenceNode.Key == "Defaults")
					{
						if (allSequencesHaveFilename)
							sequenceNode.RemoveNodes("Filename");

						if (allSequencesHaveTilesetFilenames)
							sequenceNode.RemoveNodes("TilesetFilenames");

						if (!sequenceNode.Value.Nodes.Any())
							imageNode.RemoveNode(sequenceNode);
					}

					if (allSequencesHaveFilename && sequenceNode.LastChildMatching("Combine") != null)
						sequenceNode.RemoveNodes("Filename");

					if (allSequencesHaveTilesetFilenames && sequenceNode.LastChildMatching("Combine") != null)
						sequenceNode.RemoveNodes("TilesetFilenames");

					var tilesetFilenamesNode = sequenceNode.LastChildMatching("TilesetFilenames");
					if (allSequencesHaveTilesetFilenames && tilesetFilenamesNode != null && !tilesetFilenamesNode.Value.Nodes.Any())
						sequenceNode.RemoveNode(tilesetFilenamesNode);
				}
			}

			foreach (var sequenceNode in imageNode.Value.Nodes.ToList())
				if (implicitInheritedSequences.Contains(sequenceNode.Key) && !sequenceNode.Value.Nodes.Any())
					imageNode.RemoveNode(sequenceNode);

			yield break;
		}

		void ProcessNode(ModData modData, MiniYamlNode sequenceNode, MiniYamlNode resolvedSequenceNode, string imageName)
		{
			var addExtension = true;
			var addExtensionNode = resolvedSequenceNode.LastChildMatching("AddExtension");
			if (addExtensionNode != null)
				addExtension = FieldLoader.GetValue<bool>("AddExtension", addExtensionNode.Value.Value);

			var useTilesetExtension = false;
			var useTilesetExtensionNode = resolvedSequenceNode.LastChildMatching("UseTilesetExtension");
			if (useTilesetExtensionNode != null)
				useTilesetExtension = FieldLoader.GetValue<bool>("UseTilesetExtension", useTilesetExtensionNode.Value.Value);

			var useTilesetCode = false;
			var useTilesetCodeNode = resolvedSequenceNode.LastChildMatching("UseTilesetCode");
			if (useTilesetCodeNode != null)
				useTilesetCode = FieldLoader.GetValue<bool>("UseTilesetCode", useTilesetCodeNode.Value.Value);

			var tilesetOverrides = new Dictionary<string, string>();
			var tilesetOverridesNode = resolvedSequenceNode.LastChildMatching("TilesetOverrides");
			if (tilesetOverridesNode != null)
				foreach (var tilesetNode in tilesetOverridesNode.Value.Nodes)
					tilesetOverrides[tilesetNode.Key] = tilesetNode.Value.Value;

			sequenceNode.RemoveNodes("AddExtension");
			sequenceNode.RemoveNodes("UseTilesetExtension");
			sequenceNode.RemoveNodes("UseTilesetCode");
			sequenceNode.RemoveNodes("TilesetOverrides");

			// Replace removals with masking
			foreach (var node in sequenceNode.Value.Nodes)
				if (node.Key?.StartsWith("-") ?? false)
					node.Key = node.Key.Substring(1);

			var combineNode = sequenceNode.LastChildMatching("Combine");
			if (combineNode != null)
			{
				var i = 0;
				foreach (var node in combineNode.Value.Nodes)
				{
					ProcessNode(modData, node, node, node.Key);
					node.Key = (i++).ToString();
				}

				return;
			}

			var filename = string.IsNullOrEmpty(resolvedSequenceNode.Value.Value) ? imageName : resolvedSequenceNode.Value.Value;
			if (filename.StartsWith("^"))
				return;

			if (useTilesetExtension || useTilesetCode)
			{
				var tilesetFilenamesNode = new MiniYamlNode("TilesetFilenames", "");
				var duplicateCount = new Dictionary<string, int>();
				foreach (var tileset in modData.DefaultTerrainInfo.Keys)
				{
					if (!tilesetOverrides.TryGetValue(tileset, out var sequenceTileset))
						sequenceTileset = tileset;

					var overrideFilename = filename;
					if (useTilesetCode)
						overrideFilename = filename.Substring(0, 1) + tilesetCodes[sequenceTileset] +
						                   filename.Substring(2, filename.Length - 2);

					if (addExtension)
						overrideFilename += useTilesetExtension ? tilesetExtensions[sequenceTileset] : defaultSpriteExtension;

					tilesetFilenamesNode.AddNode(tileset, overrideFilename);
					duplicateCount[overrideFilename] = duplicateCount.GetValueOrDefault(overrideFilename, 0) + 1;
				}

				sequenceNode.Value.Nodes.Insert(0, tilesetFilenamesNode);

				// Deduplicate tileset overrides
				var maxDuplicateCount = duplicateCount.MaxByOrDefault(kv => kv.Value).Value;
				if (maxDuplicateCount > 1)
				{
					var filenameNode = new MiniYamlNode("Filename", duplicateCount.First(kv => kv.Value == maxDuplicateCount).Key);
					foreach (var overrideNode in tilesetFilenamesNode.Value.Nodes.ToList())
						if (overrideNode.Value.Value == filenameNode.Value.Value)
							tilesetFilenamesNode.Value.Nodes.Remove(overrideNode);

					if (!tilesetFilenamesNode.Value.Nodes.Any())
						sequenceNode.RemoveNode(tilesetFilenamesNode);

					sequenceNode.Value.Nodes.Insert(0, filenameNode);
				}
			}
			else
			{
				if (addExtension)
					filename += defaultSpriteExtension;

				sequenceNode.Value.Nodes.Insert(0, new MiniYamlNode("Filename", filename));
			}

			sequenceNode.ReplaceValue("");
		}
	}
}
