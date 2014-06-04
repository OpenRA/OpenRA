#region Copyright & License Information
 /*
  * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
  * This file is part of OpenRA, which is free software. It is made
  * available to you under the terms of the GNU General Public License
  * as published by the Free Software Foundation. For more information,
  * see COPYING.
  */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Graphics;

namespace OpenRA.Mods.RA
{
	public class PlaceModuleOrderGenerator : IOrderGenerator
	{
		readonly Actor producer;
		readonly string buildingName;
		readonly BuildingInfo bi;
		Sprite buildOk, buildBlocked;

		public PlaceModuleOrderGenerator(Actor producer, string name)
		{
			this.producer = producer;
			buildingName = name;
			var tileset = producer.World.TileSet.Id.ToLower();
			bi = producer.World.Map.Rules.Actors[buildingName].Traits.Get<BuildingInfo>();
			buildOk = producer.World.Map.SequenceProvider.GetSequence("overlay", "build-valid-{0}".F(tileset)).GetSprite(0);
			buildBlocked = producer.World.Map.SequenceProvider.GetSequence("overlay", "build-invalid").GetSprite(0);
		}

		public IEnumerable<Order> Order(World world, CPos xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
				world.CancelInputMode();

			if (mi.Button == MouseButton.Left)
			{
				if (world.CanPlaceModule(world.ActorMap.GetUnitsAt(xy).FirstOrDefault(), world.Map.Rules.Actors[buildingName], xy))
				{
					yield return new Order("PlaceModule", producer, false) { TargetLocation = xy, TargetString = buildingName };
					world.CancelInputMode();
				}
				else
				{
					Sound.PlayNotification(world.Map.Rules, producer.Owner, "Speech", "BuildingCannotPlaceAudio", producer.Owner.Country.Race);
					yield break;
				}
			}
		}

		public void RenderAfterWorld(WorldRenderer wr, World world)
		{
			var cells = new Dictionary<CPos, bool>();
			var position = wr.Position(wr.Viewport.ViewToWorldPx(Viewport.LastMousePos)).ToCPos();
			var topLeft = position - FootprintUtils.AdjustForBuildingSize(bi);
			var toPlaceInfo = world.Map.Rules.Actors[buildingName.ToLowerInvariant()];

			foreach (var t in FootprintUtils.Tiles(world.Map.Rules, buildingName, bi, topLeft))
				cells.Add(t, world.CanPlaceModule(world.ActorMap.GetUnitsAt(topLeft).FirstOrDefault(), toPlaceInfo, topLeft));

			var pal = wr.Palette("terrain");
			foreach (var c in cells)
			{
				var tile = c.Value ? buildOk : buildBlocked;
				new SpriteRenderable(tile, c.Key.CenterPosition,
					WVec.Zero, -511, pal, 1f, true).Render(wr);
			}
		}

		public void Tick(World world) {	}
		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) {	yield break; }
		public string GetCursor(World world, CPos xy, MouseInput mi) { return "default"; }
	}
}
