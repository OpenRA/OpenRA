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
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits.Radar;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class EditorActorPreview : IEquatable<EditorActorPreview>
	{
		public readonly string DescriptiveName;
		public readonly ActorInfo Info;
		public readonly WPos CenterPosition;
		public readonly IReadOnlyDictionary<CPos, SubCell> Footprint;
		public readonly Rectangle Bounds;
		public readonly SelectionBoxAnnotationRenderable SelectionBox;

		public string Tooltip =>
			(tooltip == null ? " < " + Info.Name + " >" : tooltip.Name) + "\n" + Owner.Name + " (" + Owner.Faction + ")"
			+ "\nID: " + ID + "\nType: " + Info.Name;

		public string Type => reference.Type;

		public string ID { get; set; }
		public PlayerReference Owner { get; set; }
		public SubCell SubCell { get; }
		public bool Selected { get; set; }
		public Color RadarColor { get; private set; }
		readonly RadarColorFromTerrainInfo terrainRadarColorInfo;

		readonly WorldRenderer worldRenderer;
		readonly TooltipInfoBase tooltip;
		IActorPreview[] previews;
		readonly ActorReference reference;
		readonly Dictionary<INotifyEditorPlacementInfo, object> editorData = new Dictionary<INotifyEditorPlacementInfo, object>();

		public EditorActorPreview(WorldRenderer worldRenderer, string id, ActorReference reference, PlayerReference owner)
		{
			ID = id;
			this.reference = reference;
			Owner = owner;
			this.worldRenderer = worldRenderer;

			if (!reference.Contains<FactionInit>())
				reference.Add(new FactionInit(owner.Faction));

			if (!reference.Contains<OwnerInit>())
				reference.Add(new OwnerInit(owner.Name));

			var world = worldRenderer.World;
			if (!world.Map.Rules.Actors.TryGetValue(reference.Type.ToLowerInvariant(), out Info))
				throw new InvalidDataException($"Actor {id} of unknown type {reference.Type.ToLowerInvariant()}");

			CenterPosition = PreviewPosition(world, reference);

			var location = reference.Get<LocationInit>().Value;
			var ios = Info.TraitInfoOrDefault<IOccupySpaceInfo>();

			var subCellInit = reference.GetOrDefault<SubCellInit>();
			var subCell = subCellInit != null ? subCellInit.Value : SubCell.Any;

			var occupiedCells = ios?.OccupiedCells(Info, location, subCell);
			if (occupiedCells == null || occupiedCells.Count == 0)
				Footprint = new Dictionary<CPos, SubCell>() { { location, SubCell.FullCell } };
			else
				Footprint = occupiedCells;

			tooltip = Info.TraitInfos<EditorOnlyTooltipInfo>().FirstOrDefault(info => info.EnabledByDefault) as TooltipInfoBase
				?? Info.TraitInfos<TooltipInfo>().FirstOrDefault(info => info.EnabledByDefault);

			DescriptiveName = tooltip != null ? tooltip.Name : Info.Name;

			GeneratePreviews();

			terrainRadarColorInfo = Info.TraitInfoOrDefault<RadarColorFromTerrainInfo>();
			UpdateRadarColor();

			// Bounds are fixed from the initial render.
			// If this is a problem, then we may need to fetch the area from somewhere else
			var r = previews.SelectMany(p => p.ScreenBounds(worldRenderer, CenterPosition));

			Bounds = r.Union();

			SelectionBox = new SelectionBoxAnnotationRenderable(new WPos(CenterPosition.X, CenterPosition.Y, 8192),
				new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height), Color.White);
		}

		public void Tick()
		{
			foreach (var p in previews)
				p.Tick();
		}

		public IEnumerable<IRenderable> Render()
		{
			var items = previews.SelectMany(p => p.Render(worldRenderer, CenterPosition));
			if (Selected)
			{
				var overlay = items.Where(r => !r.IsDecoration && r is IModifyableRenderable)
					.Select(r =>
					{
						var mr = (IModifyableRenderable)r;
						return mr.WithTint(float3.Ones, mr.TintModifiers | TintModifiers.ReplaceColor).WithAlpha(0.5f);
					});

				return items.Concat(overlay);
			}

			return items;
		}

		public IEnumerable<IRenderable> RenderAnnotations()
		{
			if (Selected)
				yield return SelectionBox;
		}

		public void AddedToEditor()
		{
			foreach (var notify in Info.TraitInfos<INotifyEditorPlacementInfo>())
				editorData[notify] = notify.AddedToEditor(this, worldRenderer.World);
		}

		public void RemovedFromEditor()
		{
			foreach (var kv in editorData)
				kv.Key.RemovedFromEditor(this, worldRenderer.World, kv.Value);
		}

		public void AddInit<T>(T init) where T : ActorInit
		{
			reference.Add(init);
			GeneratePreviews();
		}

		public void ReplaceInit<T>(T init, TraitInfo info) where T : ActorInit
		{
			var original = GetInitOrDefault<T>(info);
			if (original != null)
				reference.Remove(original);

			reference.Add(init);
			GeneratePreviews();
		}

		public void RemoveInit<T>(TraitInfo info) where T : ActorInit
		{
			var original = GetInitOrDefault<T>(info);
			if (original != null)
				reference.Remove(original);
			GeneratePreviews();
		}

		public int RemoveInits<T>() where T : ActorInit
		{
			var removed = reference.RemoveAll<T>();
			GeneratePreviews();
			return removed;
		}

		public T GetInitOrDefault<T>(TraitInfo info) where T : ActorInit
		{
			return reference.GetOrDefault<T>(info);
		}

		public IEnumerable<T> GetInits<T>() where T : ActorInit
		{
			return reference.GetAll<T>();
		}

		public T GetInitOrDefault<T>() where T : ActorInit, ISingleInstanceInit
		{
			return reference.GetOrDefault<T>();
		}

		public void ReplaceInit<T>(T init) where T : ActorInit, ISingleInstanceInit
		{
			var original = reference.GetOrDefault<T>();
			if (original != null)
				reference.Remove(original);

			reference.Add(init);
			GeneratePreviews();
			UpdateRadarColor();
		}

		public void RemoveInit<T>() where T : ActorInit, ISingleInstanceInit
		{
			reference.RemoveAll<T>();
			GeneratePreviews();
		}

		public MiniYaml Save()
		{
			bool SaveInit(ActorInit init)
			{
				if (init is FactionInit factionInit && factionInit.Value == Owner.Faction)
					return false;

				if (init is HealthInit healthInit && healthInit.Value == 100)
					return false;

				// TODO: Other default values will need to be filtered
				// here after we have built a properties panel
				return true;
			}

			return reference.Save(SaveInit);
		}

		WPos PreviewPosition(World world, ActorReference actor)
		{
			var centerPositionInit = actor.GetOrDefault<CenterPositionInit>();
			if (centerPositionInit != null)
				return centerPositionInit.Value;

			var locationInit = actor.GetOrDefault<LocationInit>();

			if (locationInit != null)
			{
				var cell = locationInit.Value;
				var offset = WVec.Zero;

				var subCellInit = reference.GetOrDefault<SubCellInit>();
				var subCell = subCellInit != null ? subCellInit.Value : SubCell.Any;

				var buildingInfo = Info.TraitInfoOrDefault<BuildingInfo>();
				if (buildingInfo != null)
					offset = buildingInfo.CenterOffset(world);

				return world.Map.CenterOfSubCell(cell, subCell) + offset;
			}
			else
				throw new InvalidDataException($"Actor {ID} must define Location or CenterPosition");
		}

		void GeneratePreviews()
		{
			var init = new ActorPreviewInitializer(reference, worldRenderer);
			previews = Info.TraitInfos<IRenderActorPreviewInfo>()
				.SelectMany(rpi => rpi.RenderPreview(init))
				.ToArray();
		}

		void UpdateRadarColor()
		{
			RadarColor = terrainRadarColorInfo == null ? Owner.Color : terrainRadarColorInfo.GetColorFromTerrain(worldRenderer.World);
		}

		public ActorReference Export()
		{
			return reference.Clone();
		}

		public override string ToString()
		{
			return $"{Info.Name} {ID}";
		}

		public bool Equals(EditorActorPreview other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;

			return string.Equals(ID, other.ID, StringComparison.OrdinalIgnoreCase);
		}

		public override bool Equals(object obj)
		{
			if (obj is null)
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			if (obj.GetType() != GetType())
				return false;

			return Equals((EditorActorPreview)obj);
		}

		public override int GetHashCode()
		{
			return ID != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(ID) : 0;
		}
	}
}
