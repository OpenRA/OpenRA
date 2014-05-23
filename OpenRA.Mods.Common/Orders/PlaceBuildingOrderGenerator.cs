#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Orders
{
	public class PlaceBuildingOrderGenerator : IOrderGenerator
	{
		readonly Actor producer;
		readonly string building;
		readonly BuildingInfo buildingInfo;
		IActorPreview[] preview;

		Sprite buildOk, buildBlocked;
		bool initialized = false;

		public PlaceBuildingOrderGenerator(ProductionQueue queue, string name)
		{
			producer = queue.Actor;
			building = name;

			var map = producer.World.Map;
			var tileset = producer.World.TileSet.Id.ToLowerInvariant();
			buildingInfo = map.Rules.Actors[building].Traits.Get<BuildingInfo>();

			buildOk = map.SequenceProvider.GetSequence("overlay", "build-valid-{0}".F(tileset)).GetSprite(0);
			buildBlocked = map.SequenceProvider.GetSequence("overlay", "build-invalid").GetSprite(0);
		}

		public IEnumerable<Order> Order(World world, CPos xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
				world.CancelInputMode();

			var ret = InnerOrder(world, xy, mi).ToArray();
			if (ret.Length > 0)
				world.CancelInputMode();

			return ret;
		}

		IEnumerable<Order> InnerOrder(World world, CPos xy, MouseInput mi)
		{
			if (world.Paused)
				yield break;

			if (mi.Button == MouseButton.Left)
			{
				var topLeft = xy - FootprintUtils.AdjustForBuildingSize(buildingInfo);
				if (!world.CanPlaceBuilding(building, buildingInfo, topLeft, null)
					|| !buildingInfo.IsCloseEnoughToBase(world, producer.Owner, building, topLeft))
				{
					Sound.PlayNotification(world.Map.Rules, producer.Owner, "Speech", "BuildingCannotPlaceAudio", producer.Owner.Country.Race);
					yield break;
				}

				var isLineBuild = world.Map.Rules.Actors[building].Traits.Contains<LineBuildInfo>();
				yield return new Order(isLineBuild ? "LineBuild" : "PlaceBuilding", producer.Owner.PlayerActor, false)
				{
					TargetLocation = topLeft,
					TargetActor = producer,
					TargetString = building,
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
			var topLeft = xy - FootprintUtils.AdjustForBuildingSize(buildingInfo);

			var rules = world.Map.Rules;

			var actorInfo = rules.Actors[building];
			foreach (var dec in actorInfo.Traits.WithInterface<IPlaceBuildingDecoration>())
				foreach (var r in dec.Render(wr, world, actorInfo, world.Map.CenterOfCell(xy)))
					yield return r;

			var cells = new Dictionary<CPos, bool>();

			// Linebuild for walls.
			// Requires a 1x1 footprint
			if (rules.Actors[building].Traits.Contains<LineBuildInfo>())
			{
				if (buildingInfo.Dimensions.X != 1 || buildingInfo.Dimensions.Y != 1)
					throw new InvalidOperationException("LineBuild requires a 1x1 sized Building");

				foreach (var t in BuildingUtils.GetLineBuildCells(world, topLeft, building, buildingInfo))
					cells.Add(t, buildingInfo.IsCloseEnoughToBase(world, world.LocalPlayer, building, t));
			}
			else
			{
				if (!initialized)
				{
					var init = new ActorPreviewInitializer(rules.Actors[building], producer.Owner, wr, new TypeDictionary());
					preview = rules.Actors[building].Traits.WithInterface<IRenderActorPreviewInfo>()
						.SelectMany(rpi => rpi.RenderPreview(init))
						.ToArray();

					initialized = true;
				}

				var offset = world.Map.CenterOfCell(topLeft) + FootprintUtils.CenterOffset(world, buildingInfo);
				var previewRenderables = preview
					.SelectMany(p => p.Render(wr, offset))
					.OrderBy(WorldRenderer.RenderableScreenZPositionComparisonKey);

				foreach (var r in previewRenderables)
					yield return r;

				var res = world.WorldActor.Trait<ResourceLayer>();
				var isCloseEnough = buildingInfo.IsCloseEnoughToBase(world, world.LocalPlayer, building, topLeft);
				foreach (var t in FootprintUtils.Tiles(rules, building, buildingInfo, topLeft))
					cells.Add(t, isCloseEnough && world.IsCellBuildable(t, buildingInfo) && res.GetResource(t) == null);
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
