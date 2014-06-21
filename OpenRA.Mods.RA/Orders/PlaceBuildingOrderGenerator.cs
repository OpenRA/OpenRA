#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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

		public PlaceBuildingOrderGenerator(ProductionQueue queue, string name)
		{
			Producer = queue.Actor;
			Building = name;

			var map = Producer.World.Map;
			var tileset = Producer.World.TileSet.Id.ToLowerInvariant();
			BuildingInfo = map.Rules.Actors[Building].Traits.Get<BuildingInfo>();

			buildOk = map.SequenceProvider.GetSequence("overlay", "build-valid-{0}".F(tileset)).GetSprite(0);
			buildBlocked = map.SequenceProvider.GetSequence("overlay", "build-invalid").GetSprite(0);
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
					Sound.PlayNotification(world.Map.Rules, Producer.Owner, "Speech", "BuildingCannotPlaceAudio", Producer.Owner.Country.Race);
					yield break;
				}

				var isLineBuild = world.Map.Rules.Actors[Building].Traits.Contains<LineBuildInfo>();
				yield return new Order(isLineBuild ? "LineBuild" : "PlaceBuilding", Producer.Owner.PlayerActor, false)
				{
					TargetLocation = topLeft,
					TargetActor = Producer,
					TargetString = Building,
					SuppressVisualFeedback = true
				};
			}
		}

		public void Tick(World world) {}
		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		public void RenderAfterWorld(WorldRenderer wr, World world)
		{
			var position = wr.Position(wr.Viewport.ViewToWorldPx(Viewport.LastMousePos)).ToCPos();
			var topLeft = position - FootprintUtils.AdjustForBuildingSize(BuildingInfo);

			var rules = world.Map.Rules;

			var actorInfo = rules.Actors[Building];
			foreach (var dec in actorInfo.Traits.WithInterface<IPlaceBuildingDecoration>())
				dec.Render(wr, world, actorInfo, position.CenterPosition);	/* hack hack */

			var cells = new Dictionary<CPos, bool>();
			// Linebuild for walls.
			// Assumes a 1x1 footprint; weird things will happen for other footprints
			if (rules.Actors[Building].Traits.Contains<LineBuildInfo>())
			{
				foreach (var t in BuildingUtils.GetLineBuildCells(world, topLeft, Building, BuildingInfo))
					cells.Add(t, BuildingInfo.IsCloseEnoughToBase(world, world.LocalPlayer, Building, t));
			}
			else
			{
				if (!initialized)
				{
					var rbi = rules.Actors[Building].Traits.GetOrDefault<RenderBuildingInfo>();
					if (rbi == null)
						preview = new IRenderable[0];
					else
					{
						var palette = rbi.Palette ?? (Producer.Owner != null ?
							rbi.PlayerPalette + Producer.Owner.InternalName : null);

						preview = rbi.RenderPreview(world, rules.Actors[Building], wr.Palette(palette));
					}

					initialized = true;
				}

				var offset = topLeft.CenterPosition + FootprintUtils.CenterOffset(BuildingInfo) - WPos.Zero;
				foreach (var r in preview)
					r.OffsetBy(offset).Render(wr);

				var res = world.WorldActor.Trait<ResourceLayer>();
				var isCloseEnough = BuildingInfo.IsCloseEnoughToBase(world, world.LocalPlayer, Building, topLeft);
				foreach (var t in FootprintUtils.Tiles(rules, Building, BuildingInfo, topLeft))
					cells.Add(t, isCloseEnough && world.IsCellBuildable(t, BuildingInfo) && res.GetResource(t) == null);
			}

			var pal = wr.Palette("terrain");
			foreach (var c in cells)
			{
				var tile = c.Value ? buildOk : buildBlocked;
				new SpriteRenderable(tile, c.Key.CenterPosition,
					WVec.Zero, -511, pal, 1f, true).Render(wr);
			}
		}

		public string GetCursor(World world, CPos xy, MouseInput mi) { return "default"; }
	}
}
