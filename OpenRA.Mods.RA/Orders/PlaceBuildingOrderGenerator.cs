#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Orders
{
	public class PlaceBuildingOrderGenerator : IOrderGenerator
	{
		readonly Actor Producer;
		readonly string Building;
		readonly BuildingInfo BuildingInfo;
		IEnumerable<IRenderable> preview;
		Sprite buildOk, buildBlocked;
		bool initialized = false;

		public PlaceBuildingOrderGenerator(Actor producer, string name)
		{
			Producer = producer;
			Building = name;
			var tileset = producer.World.TileSet.Id.ToLower();
			BuildingInfo = Rules.Info[Building].Traits.Get<BuildingInfo>();

			buildOk = SequenceProvider.GetSequence("overlay", "build-valid-{0}".F(tileset)).GetSprite(0);
			buildBlocked = SequenceProvider.GetSequence("overlay", "build-invalid").GetSprite(0);
		}

		public IEnumerable<Order> Order(World world, CPos xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
				world.CancelInputMode();

			var ret = InnerOrder(world, xy, mi).ToList();
			if (ret.Count > 0)
				world.CancelInputMode();

			return ret;
		}

		IEnumerable<Order> InnerOrder(World world, CPos xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				var topLeft = xy - FootprintUtils.AdjustForBuildingSize(BuildingInfo);
				if (!world.CanPlaceBuilding(Building, BuildingInfo, topLeft, null)
					|| !BuildingInfo.IsCloseEnoughToBase(world, Producer.Owner, Building, topLeft))
				{
					Sound.PlayNotification(Producer.Owner, "Speech", "BuildingCannotPlaceAudio", Producer.Owner.Country.Race);
					yield break;
				}

				var isLineBuild = Rules.Info[Building].Traits.Contains<LineBuildInfo>();
				yield return new Order(isLineBuild ? "LineBuild" : "PlaceBuilding",
					Producer.Owner.PlayerActor, false) { TargetLocation = topLeft, TargetString = Building };
			}
		}

		public void Tick(World world) {}
		public void RenderAfterWorld(WorldRenderer wr, World world) {}
		public void RenderBeforeWorld(WorldRenderer wr, World world)
		{
			var position = Game.viewport.ViewToWorld(Viewport.LastMousePos);
			var topLeft = position - FootprintUtils.AdjustForBuildingSize(BuildingInfo);

			var actorInfo = Rules.Info[Building];
			foreach (var dec in actorInfo.Traits.WithInterface<IPlaceBuildingDecoration>())
				dec.Render(wr, world, actorInfo, Traits.Util.CenterOfCell(position));	/* hack hack */

			var cells = new Dictionary<CPos, bool>();
			// Linebuild for walls.
			// Assumes a 1x1 footprint; weird things will happen for other footprints
			if (Rules.Info[Building].Traits.Contains<LineBuildInfo>())
			{
				foreach (var t in BuildingUtils.GetLineBuildCells(world, topLeft, Building, BuildingInfo))
					cells.Add(t, BuildingInfo.IsCloseEnoughToBase(world, world.LocalPlayer, Building, t));
			}
			else
			{
				if (!initialized)
				{
					var rbi = Rules.Info[Building].Traits.Get<RenderBuildingInfo>();
					var palette = rbi.Palette ?? (Producer.Owner != null ?
					                              rbi.PlayerPalette + Producer.Owner.InternalName : null);

					preview = rbi.RenderPreview(Rules.Info[Building], wr.Palette(palette));
					initialized = true;
				}

				var offset = topLeft.CenterPosition + FootprintUtils.CenterOffset(BuildingInfo) - WPos.Zero;
				foreach (var r in preview)
					r.WithPos(r.Pos + offset).Render(wr);

				var res = world.WorldActor.Trait<ResourceLayer>();
				var isCloseEnough = BuildingInfo.IsCloseEnoughToBase(world, world.LocalPlayer, Building, topLeft);
				foreach (var t in FootprintUtils.Tiles(Building, BuildingInfo, topLeft))
					cells.Add(t, isCloseEnough && world.IsCellBuildable(t, BuildingInfo) && res.GetResource(t) == null);
			}

			var pal = wr.Palette("terrain");
			foreach (var c in cells)
				(c.Value ? buildOk : buildBlocked).DrawAt(c.Key.ToPPos().ToFloat2(), pal);
		}

		public string GetCursor(World world, CPos xy, MouseInput mi) { return "default"; }
	}
}
