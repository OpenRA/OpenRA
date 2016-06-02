#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class EditorActorPreview
	{
		public readonly string Tooltip;
		public readonly string ID;
		public readonly ActorInfo Info;
		public readonly PlayerReference Owner;
		public readonly WPos CenterPosition;
		public readonly IReadOnlyDictionary<CPos, SubCell> Footprint;
		public readonly Rectangle Bounds;

		public SubCell SubCell { get; private set; }

		readonly ActorReference actor;
		readonly WorldRenderer worldRenderer;
		IActorPreview[] previews;

		public EditorActorPreview(WorldRenderer worldRenderer, string id, ActorReference actor, PlayerReference owner)
		{
			ID = id;
			this.actor = actor;
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

			var location = actor.InitDict.Get<LocationInit>().Value(worldRenderer.World);
			var ios = Info.TraitInfoOrDefault<IOccupySpaceInfo>();

			var subCellInit = actor.InitDict.GetOrDefault<SubCellInit>();
			var subCell = subCellInit != null ? subCellInit.Value(worldRenderer.World) : SubCell.Any;

			if (ios != null)
				Footprint = ios.OccupiedCells(Info, location, subCell);
			else
			{
				var footprint = new Dictionary<CPos, SubCell>() { { location, SubCell.FullCell } };
				Footprint = new ReadOnlyDictionary<CPos, SubCell>(footprint);
			}

			var tooltip = Info.TraitInfoOrDefault<EditorOnlyTooltipInfo>() as TooltipInfoBase ?? Info.TraitInfoOrDefault<TooltipInfo>();
			Tooltip = (tooltip == null ? " < " + Info.Name + " >" : tooltip.Name) + "\n" + owner.Name + " (" + owner.Faction + ")"
				+ "\nID: " + ID + "\nType: " + Info.Name;

			GeneratePreviews();

			// Bounds are fixed from the initial render.
			// If this is a problem, then we may need to fetch the area from somewhere else
			var r = previews
				.SelectMany(p => p.Render(worldRenderer, CenterPosition))
				.Select(rr => rr.PrepareRender(worldRenderer));

			if (r.Any())
			{
				Bounds = r.First().ScreenBounds(worldRenderer);
				foreach (var rr in r.Skip(1))
					Bounds = Rectangle.Union(Bounds, rr.ScreenBounds(worldRenderer));
			}
		}

		public void Tick()
		{
			foreach (var p in previews)
				p.Tick();
		}

		public IEnumerable<IRenderable> Render()
		{
			return previews.SelectMany(p => p.Render(worldRenderer, CenterPosition));
		}

		public void ReplaceInit<T>(T init)
		{
			var original = actor.InitDict.GetOrDefault<T>();
			if (original != null)
				actor.InitDict.Remove(original);

			actor.InitDict.Add(init);
			GeneratePreviews();
		}

		public T Init<T>()
		{
			return actor.InitDict.GetOrDefault<T>();
		}

		public MiniYaml Save()
		{
			Func<object, bool> saveInit = init =>
			{
				var factionInit = init as FactionInit;
				if (factionInit != null && factionInit.Faction == Owner.Faction)
					return false;

				// TODO: Other default values will need to be filtered
				// here after we have built a properties panel
				return true;
			};

			return actor.Save(saveInit);
		}

		WPos PreviewPosition(World world, TypeDictionary init)
		{
			if (init.Contains<CenterPositionInit>())
				return init.Get<CenterPositionInit>().Value(world);

			if (init.Contains<LocationInit>())
			{
				var cell = init.Get<LocationInit>().Value(world);
				var offset = WVec.Zero;

				var subCellInit = actor.InitDict.GetOrDefault<SubCellInit>();
				var subCell = subCellInit != null ? subCellInit.Value(worldRenderer.World) : SubCell.Any;

				var buildingInfo = Info.TraitInfoOrDefault<BuildingInfo>();
				if (buildingInfo != null)
					offset = FootprintUtils.CenterOffset(world, buildingInfo);

				return world.Map.CenterOfSubCell(cell, subCell) + offset;
			}
			else
				throw new InvalidDataException("Actor {0} must define Location or CenterPosition".F(ID));
		}

		void GeneratePreviews()
		{
			var init = new ActorPreviewInitializer(Info, worldRenderer, actor.InitDict);
			previews = Info.TraitInfos<IRenderActorPreviewInfo>()
				.SelectMany(rpi => rpi.RenderPreview(init))
				.ToArray();
		}

		public ActorReference Export()
		{
			return new ActorReference(actor.Type, actor.Save().ToDictionary());
		}

		public override string ToString()
		{
			return "{0} {1}".F(Info.Name, ID);
		}
	}
}
