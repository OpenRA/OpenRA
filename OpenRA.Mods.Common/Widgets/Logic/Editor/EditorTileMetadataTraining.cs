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
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public sealed class EditorTileMetadataTraining
	{
		public static EditorTileMetadataTraining Instance { get; set; }

		readonly EditorTileMetadataFile metadataFile;
		readonly ITemplatedTerrainInfo terrainInfo;
		readonly Ruleset rules;

		public EditorMetadataTrainingKind Mode { get; private set; }
		public EditorMetadataTrainingPhase Phase { get; private set; }

		readonly HashSet<string> primaryTemplateKeys = new(StringComparer.Ordinal);
		readonly HashSet<ushort> primaryTemplateIds = [];
		readonly HashSet<string> primaryActorKeys = new(StringComparer.OrdinalIgnoreCase);
		readonly HashSet<string> primaryActorNames = new(StringComparer.OrdinalIgnoreCase);

		public string PrimaryTemplateKey { get; private set; }
		public ushort? PrimaryTemplateId { get; private set; }
		public string PrimaryActorKey { get; private set; }
		public string PrimaryActorName { get; private set; }

		readonly HashSet<string> secondaryRefs = new(StringComparer.OrdinalIgnoreCase);
		readonly Dictionary<string, MetadataTemplateRow> templateRowsByKey = new(StringComparer.Ordinal);
		readonly Dictionary<ushort, MetadataTemplateRow> templateRowsById = [];
		readonly Dictionary<string, MetadataActorRow> actorRowsByKey = new(StringComparer.OrdinalIgnoreCase);
		readonly Dictionary<string, MetadataActorRow> actorRowsByName = new(StringComparer.OrdinalIgnoreCase);

		public event Action Changed;

		public EditorTileMetadataTraining(ModData modData, ITemplatedTerrainInfo terrainInfo, Ruleset rules)
		{
			this.terrainInfo = terrainInfo;
			this.rules = rules;
			metadataFile = new EditorTileMetadataFile(modData);
			RebuildRowCache();
			metadataFile.Changed += RebuildRowCache;
			Instance = this;
		}

		public EditorTileMetadataFile MetadataFile => metadataFile;

		public bool IsActive => Mode != EditorMetadataTrainingKind.None;

		public int PrimaryTemplateCount => primaryTemplateIds.Count;

		public int PrimaryActorCount => primaryActorNames.Count;

		public string ModeDisplayName => Mode switch
		{
			EditorMetadataTrainingKind.OppositeIsland => "Opposite Island",
			EditorMetadataTrainingKind.OppositeRing => "Opposite Ring",
			EditorMetadataTrainingKind.Similar => "Similar",
			EditorMetadataTrainingKind.OrientationIsland => "Orientation Island",
			EditorMetadataTrainingKind.OrientationRing => "Orientation Ring",
			EditorMetadataTrainingKind.RelatedCornersIsland => "Corner Island",
			EditorMetadataTrainingKind.RelatedCornersRing => "Corner Ring",
			_ => ""
		};

		public bool ShowTrainingCheckboxes => IsActive && Phase == EditorMetadataTrainingPhase.PickPrimary;

		public bool ShowSecondarySelection =>
			IsActive &&
			!ShowOrientationTraining &&
			(Mode == EditorMetadataTrainingKind.OppositeIsland ||
				Mode == EditorMetadataTrainingKind.OppositeRing ||
				Mode == EditorMetadataTrainingKind.RelatedCornersIsland ||
				Mode == EditorMetadataTrainingKind.RelatedCornersRing ||
				Mode == EditorMetadataTrainingKind.Similar) &&
			(PrimaryTemplateCount > 0 || PrimaryActorCount > 0);

		public bool ShowOrientationTraining =>
			IsActive &&
			(Mode == EditorMetadataTrainingKind.OrientationIsland || Mode == EditorMetadataTrainingKind.OrientationRing);

		public bool ShowOrientationSave => ShowOrientationTraining && PrimaryTemplateCount > 0;

		public bool ShowRelatedCornersTraining =>
			IsActive &&
			(Mode == EditorMetadataTrainingKind.RelatedCornersIsland || Mode == EditorMetadataTrainingKind.RelatedCornersRing);

		public int? PendingOrientationSlot { get; private set; }

		public IReadOnlyCollection<string> SecondaryRefs => secondaryRefs;

		public void Start(EditorMetadataTrainingKind mode)
		{
			Mode = mode;
			Phase = EditorMetadataTrainingPhase.PickPrimary;
			ClearSelection();
			NotifyChanged();
		}

		public void ApplyFocusedTemplateAsPrimary(ushort templateId)
		{
			if (!IsActive || !templateRowsById.TryGetValue(templateId, out var row))
				return;

			primaryTemplateIds.Clear();
			primaryTemplateKeys.Clear();
			primaryTemplateIds.Add(templateId);
			primaryTemplateKeys.Add(row.Key);
			SyncLegacyPrimaryTemplate();
			UpdatePhaseAfterPrimaryChange();
			LoadPendingTrainingValuesFromPrimary();
			NotifyChanged();
		}

		public void Cancel()
		{
			Mode = EditorMetadataTrainingKind.None;
			Phase = EditorMetadataTrainingPhase.PickPrimary;
			ClearSelection();
			NotifyChanged();
		}

		public bool IsPrimaryTemplateSelected(ushort templateId) => primaryTemplateIds.Contains(templateId);

		public bool IsPrimaryActorSelected(string actorName) =>
			primaryActorNames.Contains(actorName);

		public bool TogglePrimaryTemplate(ushort templateId)
		{
			if (!IsActive)
				return false;

			if (Mode != EditorMetadataTrainingKind.Similar &&
				Mode != EditorMetadataTrainingKind.OrientationIsland &&
				Mode != EditorMetadataTrainingKind.OrientationRing &&
				Mode != EditorMetadataTrainingKind.RelatedCornersIsland &&
				Mode != EditorMetadataTrainingKind.RelatedCornersRing &&
				Mode != EditorMetadataTrainingKind.OppositeIsland &&
				Mode != EditorMetadataTrainingKind.OppositeRing)
				return false;

			if (!templateRowsById.TryGetValue(templateId, out var row))
				return false;

			if (!primaryTemplateIds.Add(templateId))
			{
				primaryTemplateIds.Remove(templateId);
				primaryTemplateKeys.Remove(row.Key);
			}
			else
				primaryTemplateKeys.Add(row.Key);

			SyncLegacyPrimaryTemplate();
			UpdatePhaseAfterPrimaryChange();
			LoadPendingTrainingValuesFromPrimary();
			NotifyChanged();
			return true;
		}

		void LoadPendingTrainingValuesFromPrimary()
		{
			if (ShowOrientationTraining)
				LoadPendingOrientationFromPrimary();
			else if (ShowRelatedCornersTraining)
				LoadPendingRelatedCornersFromPrimary();
		}

		void LoadPendingOrientationFromPrimary()
		{
			PendingOrientationSlot = null;
			if (PrimaryTemplateId == null || !templateRowsById.TryGetValue(PrimaryTemplateId.Value, out var row))
				return;

			var field = Mode == EditorMetadataTrainingKind.OrientationRing ? "Orientation_ring" : "Orientation_island";
			var raw = metadataFile.ReadField(row.Data, field);
			PendingOrientationSlot = EditorTileMetadata.TryParseOrientationSlot(raw);
		}

		void LoadPendingRelatedCornersFromPrimary()
		{
			secondaryRefs.Clear();
			if (PrimaryTemplateId == null || !templateRowsById.TryGetValue(PrimaryTemplateId.Value, out var row))
				return;

			var field = Mode == EditorMetadataTrainingKind.RelatedCornersRing
				? "Related_corners_ring"
				: "Related_corners_island";
			var raw = metadataFile.ReadField(row.Data, field);
			if (string.IsNullOrWhiteSpace(raw))
				return;

			foreach (var part in raw.Split(','))
			{
				var reference = part.Trim();
				if (reference.Length > 0)
					secondaryRefs.Add(reference);
			}
		}

		public bool TogglePrimaryActor(string actorName)
		{
			if (!IsActive || Mode != EditorMetadataTrainingKind.Similar)
				return false;

			if (!actorRowsByName.TryGetValue(actorName, out var row))
				return false;

			if (!primaryActorNames.Add(actorName))
			{
				primaryActorNames.Remove(actorName);
				primaryActorKeys.Remove(row.Key);
			}
			else
				primaryActorKeys.Add(row.Key);

			SyncLegacyPrimaryActor();
			UpdatePhaseAfterPrimaryChange();
			NotifyChanged();
			return true;
		}

		void UpdatePhaseAfterPrimaryChange()
		{
			Phase = EditorMetadataTrainingPhase.PickPrimary;
			if (ShowOrientationTraining && PrimaryTemplateCount == 0)
				PendingOrientationSlot = null;

			if (ShowRelatedCornersTraining && PrimaryTemplateCount == 0)
				secondaryRefs.Clear();
		}

		public bool ToggleSecondaryTemplate(ushort templateId)
		{
			if (!ShowSecondarySelection || primaryTemplateIds.Contains(templateId))
				return false;

			if (!templateRowsById.TryGetValue(templateId, out var row))
				return false;

			var reference = EditorTileMetadataFile.TemplateReference(terrainInfo, terrainInfo.Templates[templateId], row);
			if (!secondaryRefs.Add(reference))
				secondaryRefs.Remove(reference);

			NotifyChanged();
			return true;
		}

		public bool ToggleSecondaryActor(string actorName)
		{
			if (!ShowSecondarySelection || Mode != EditorMetadataTrainingKind.Similar)
				return false;

			if (primaryActorNames.Contains(actorName))
				return false;

			if (!secondaryRefs.Add(actorName))
				secondaryRefs.Remove(actorName);

			NotifyChanged();
			return true;
		}

		public bool IsSecondaryTemplateSelected(ushort templateId)
		{
			if (!templateRowsById.TryGetValue(templateId, out var row))
				return false;

			var reference = EditorTileMetadataFile.TemplateReference(terrainInfo, terrainInfo.Templates[templateId], row);
			return secondaryRefs.Contains(reference);
		}

		public bool IsSecondaryActorSelected(string actorName) => secondaryRefs.Contains(actorName);

		public void Save()
		{
			if (!IsActive)
				return;

			switch (Mode)
			{
				case EditorMetadataTrainingKind.OppositeIsland when PrimaryTemplateCount > 0:
					metadataFile.SaveOppositeIslandMany(primaryTemplateKeys, secondaryRefs);
					break;
				case EditorMetadataTrainingKind.OppositeRing when PrimaryTemplateCount > 0:
					metadataFile.SaveOppositeRingMany(primaryTemplateKeys, secondaryRefs);
					break;
				case EditorMetadataTrainingKind.Similar when PrimaryTemplateCount > 0:
					metadataFile.SaveSimilarTemplateMany(primaryTemplateKeys, secondaryRefs);
					break;
				case EditorMetadataTrainingKind.Similar when PrimaryActorCount > 0:
					metadataFile.SaveSimilarActorMany(primaryActorKeys, secondaryRefs);
					break;
				case EditorMetadataTrainingKind.RelatedCornersIsland when PrimaryTemplateCount > 0:
					metadataFile.SaveRelatedCornersIslandMany(primaryTemplateKeys, secondaryRefs, terrainInfo);
					break;
				case EditorMetadataTrainingKind.RelatedCornersRing when PrimaryTemplateCount > 0:
					metadataFile.SaveRelatedCornersRingMany(primaryTemplateKeys, secondaryRefs, terrainInfo);
					break;
			}

			Cancel();
		}

		public void SelectOrientationSlot(int slot)
		{
			if (!ShowOrientationTraining || PrimaryTemplateCount == 0)
				return;

			PendingOrientationSlot = slot.Clamp(0, EditorTileMetadata.VerticalSlot);
			NotifyChanged();
		}

		public void SaveOrientation()
		{
			if (!ShowOrientationSave || !PendingOrientationSlot.HasValue || PrimaryTemplateCount == 0)
				return;

			switch (Mode)
			{
				case EditorMetadataTrainingKind.OrientationIsland:
					metadataFile.SaveOrientationIslandMany(primaryTemplateKeys, PendingOrientationSlot.Value);
					break;
				case EditorMetadataTrainingKind.OrientationRing:
					metadataFile.SaveOrientationRingMany(primaryTemplateKeys, PendingOrientationSlot.Value);
					break;
			}

			Cancel();
		}

		void ClearSelection()
		{
			primaryTemplateKeys.Clear();
			primaryTemplateIds.Clear();
			primaryActorKeys.Clear();
			primaryActorNames.Clear();
			PrimaryTemplateKey = null;
			PrimaryTemplateId = null;
			PrimaryActorKey = null;
			PrimaryActorName = null;
			PendingOrientationSlot = null;
			secondaryRefs.Clear();
		}

		void RebuildRowCache()
		{
			templateRowsByKey.Clear();
			templateRowsById.Clear();
			actorRowsByKey.Clear();
			actorRowsByName.Clear();

			if (terrainInfo == null)
				return;

			foreach (var row in metadataFile.TemplateRows(terrainInfo.Id))
			{
				templateRowsByKey[row.Key] = row;
				templateRowsById[row.TemplateId] = row;
			}

			foreach (var row in metadataFile.ActorRows())
			{
				actorRowsByKey[row.Key] = row;
				actorRowsByName[row.ActorName] = row;
			}

			NotifyChanged();
		}

		void SyncLegacyPrimaryTemplate()
		{
			if (primaryTemplateIds.Count == 0)
			{
				PrimaryTemplateKey = null;
				PrimaryTemplateId = null;
				return;
			}

			PrimaryTemplateId = primaryTemplateIds.First();
			if (templateRowsById.TryGetValue(PrimaryTemplateId.Value, out var row))
				PrimaryTemplateKey = row.Key;
		}

		void SyncLegacyPrimaryActor()
		{
			if (primaryActorNames.Count == 0)
			{
				PrimaryActorKey = null;
				PrimaryActorName = null;
				return;
			}

			PrimaryActorName = primaryActorNames.First();
			if (actorRowsByName.TryGetValue(PrimaryActorName, out var row))
				PrimaryActorKey = row.Key;
		}

		public string GetPrimaryDisplayName()
		{
			if (PrimaryTemplateCount == 1 && PrimaryTemplateId != null &&
				templateRowsById.TryGetValue(PrimaryTemplateId.Value, out var row))
				return row.OriginalFilename ?? $"Template {PrimaryTemplateId}";

			if (PrimaryTemplateCount > 1)
				return $"{PrimaryTemplateCount} tiles selected";

			if (PrimaryActorCount == 1)
				return PrimaryActorName;

			if (PrimaryActorCount > 1)
				return $"{PrimaryActorCount} actors selected";

			return null;
		}

		void NotifyChanged() => Changed?.Invoke();
	}
}
