#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
		public readonly ActorReference Actor;

		public string Tooltip
		{
			get
			{
				return (tooltip == null ? " < " + Info.Name + " >" : tooltip.Name) + "\n" + Owner.Name + " (" + Owner.Faction + ")"
					+ "\nID: " + ID + "\nType: " + Info.Name;
			}
		}

		public string ID { get; set; }
		public PlayerReference Owner { get; set; }
		public SubCell SubCell { get; private set; }
		public bool Selected { get; set; }

		readonly WorldRenderer worldRenderer;
		readonly TooltipInfoBase tooltip;
		IActorPreview[] previews;

		public EditorActorPreview(WorldRenderer worldRenderer, string id, ActorReference actor, PlayerReference owner)
		{
			ID = id;
			Actor = actor;
			Owner = owner;
			this.worldRenderer = worldRenderer;

			if (!actor.InitDict.Contains<FactionInit>())
				actor.InitDict.Add(new FactionInit(owner.Faction));

			if (!actor.InitDict.Contains<OwnerInit>())
				actor.InitDict.Add(new OwnerInit(owner.Name));

			var world = worldRenderer.World;
			if (!world.Map.Rules.Actors.TryGetValue(actor.Type.ToLowerInvariant(), out Info))
				throw new InvalidDataException("Actor {0} of unknown type {1}".F(id, actor.Type.ToLowerInvariant()));

			CenterPosition = PreviewPosition(world, actor.InitDict);

			var location = actor.InitDict.Get<LocationInit>().Value;
			var ios = Info.TraitInfoOrDefault<IOccupySpaceInfo>();

			var subCellInit = actor.InitDict.GetOrDefault<SubCellInit>();
			var subCell = subCellInit != null ? subCellInit.Value : SubCell.Any;

			if (ios != null)
				Footprint = ios.OccupiedCells(Info, location, subCell);
			else
			{
				var footprint = new Dictionary<CPos, SubCell>() { { location, SubCell.FullCell } };
				Footprint = new ReadOnlyDictionary<CPos, SubCell>(footprint);
			}

			tooltip = Info.TraitInfos<EditorOnlyTooltipInfo>().FirstOrDefault(info => info.EnabledByDefault) as TooltipInfoBase
				?? Info.TraitInfos<TooltipInfo>().FirstOrDefault(info => info.EnabledByDefault);

			DescriptiveName = tooltip != null ? tooltip.Name : Info.Name;

			GeneratePreviews();

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
				var highlight = worldRenderer.Palette("highlight");
				var overlay = items.Where(r => !r.IsDecoration)
					.Select(r => r.WithPalette(highlight));
				return items.Concat(overlay);
			}

			return items;
		}

		public IEnumerable<IRenderable> RenderAnnotations()
		{
			if (Selected)
				yield return SelectionBox;
		}

		public void ReplaceInit<T>(T init)
		{
			var original = Actor.InitDict.GetOrDefault<T>();
			if (original != null)
				Actor.InitDict.Remove(original);

			Actor.InitDict.Add(init);
			GeneratePreviews();
		}

		public void RemoveInit<T>()
		{
			var original = Actor.InitDict.GetOrDefault<T>();
			if (original != null)
				Actor.InitDict.Remove(original);
			GeneratePreviews();
		}

		public T Init<T>()
		{
			return Actor.InitDict.GetOrDefault<T>();
		}

		public MiniYaml Save()
		{
			Func<object, bool> saveInit = init =>
			{
				var factionInit = init as FactionInit;
				if (factionInit != null && factionInit.Value == Owner.Faction)
					return false;

				// TODO: Other default values will need to be filtered
				// here after we have built a properties panel
				return true;
			};

			return Actor.Save(saveInit);
		}

		WPos PreviewPosition(World world, TypeDictionary init)
		{
			if (init.Contains<CenterPositionInit>())
				return init.Get<CenterPositionInit>().Value;

			if (init.Contains<LocationInit>())
			{
				var cell = init.Get<LocationInit>().Value;
				var offset = WVec.Zero;

				var subCellInit = Actor.InitDict.GetOrDefault<SubCellInit>();
				var subCell = subCellInit != null ? subCellInit.Value : SubCell.Any;

				var buildingInfo = Info.TraitInfoOrDefault<BuildingInfo>();
				if (buildingInfo != null)
					offset = buildingInfo.CenterOffset(world);

				return world.Map.CenterOfSubCell(cell, subCell) + offset;
			}
			else
				throw new InvalidDataException("Actor {0} must define Location or CenterPosition".F(ID));
		}

		void GeneratePreviews()
		{
			var init = new ActorPreviewInitializer(Info, worldRenderer, Actor.InitDict);
			previews = Info.TraitInfos<IRenderActorPreviewInfo>()
				.SelectMany(rpi => rpi.RenderPreview(init))
				.ToArray();
		}

		public ActorReference Export()
		{
			return new ActorReference(Actor.Type, Actor.Save().ToDictionary());
		}

		public override string ToString()
		{
			return "{0} {1}".F(Info.Name, ID);
		}

		public bool Equals(EditorActorPreview other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;

			return string.Equals(ID, other.ID, StringComparison.OrdinalIgnoreCase);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
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
