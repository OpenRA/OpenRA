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
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.RA.Orders
{
	public class PlaceBuildingOrderGenerator : IOrderGenerator
	{
		readonly Actor Producer;
		readonly string Building;
		readonly BuildingInfo BuildingInfo;
		IActorPreview[] preview;

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
			if (world.Paused)
				yield break;

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

		public void Tick(World world)
		{
			if (preview == null)
				return;

			foreach (var p in preview)
				p.Tick();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr, World world)
		{
			var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
			var topLeft = xy - FootprintUtils.AdjustForBuildingSize(BuildingInfo);

			var rules = world.Map.Rules;

			var actorInfo = rules.Actors[Building];
			foreach (var dec in actorInfo.Traits.WithInterface<IPlaceBuildingDecoration>())
				foreach (var r in dec.Render(wr, world, actorInfo, world.Map.CenterOfCell(xy)))
					yield return r;

			var cells = new Dictionary<CPos, bool>();
			// Linebuild for walls.
			// Requires a 1x1 footprint
			if (rules.Actors[Building].Traits.Contains<LineBuildInfo>())
			{
				if (BuildingInfo.Dimensions.X != 1 || BuildingInfo.Dimensions.Y != 1)
					throw new InvalidOperationException("LineBuild requires a 1x1 sized Building");

				foreach (var t in BuildingUtils.GetLineBuildCells(world, topLeft, Building, BuildingInfo))
					cells.Add(t, BuildingInfo.IsCloseEnoughToBase(world, world.LocalPlayer, Building, t));
			}
			else
			{
				if (!initialized)
				{
					var init = new ActorPreviewInitializer(rules.Actors[Building], Producer.Owner, wr, new TypeDictionary());
					preview = rules.Actors[Building].Traits.WithInterface<IRenderActorPreviewInfo>()
						.SelectMany(rpi => rpi.RenderPreview(init))
						.ToArray();

					initialized = true;
				}

				var comparer = new RenderableComparer(wr);
				var offset = world.Map.CenterOfCell(topLeft) + FootprintUtils.CenterOffset(world, BuildingInfo);
				var previewRenderables = preview
					.SelectMany(p => p.Render(wr, offset))
					.OrderBy(r => r, comparer);

				foreach (var r in previewRenderables)
					yield return r;

				var res = world.WorldActor.Trait<ResourceLayer>();
				var isCloseEnough = BuildingInfo.IsCloseEnoughToBase(world, world.LocalPlayer, Building, topLeft);
				foreach (var t in FootprintUtils.Tiles(rules, Building, BuildingInfo, topLeft))
					cells.Add(t, isCloseEnough && world.IsCellBuildable(t, BuildingInfo) && res.GetResource(t) == null);
			}

			var pal = wr.Palette("terrain");
			foreach (var c in cells)
			{
				var tile = c.Value ? buildOk : buildBlocked;
				yield return new SpriteRenderable(tile, world.Map.CenterOfCell(c.Key),
					WVec.Zero, -511, pal, 1f, true);
			}
		}

		public string GetCursor(World world, CPos xy, MouseInput mi) { return "default"; }
	}
}
