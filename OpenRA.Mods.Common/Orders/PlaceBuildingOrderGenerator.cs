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

namespace OpenRA.Mods.Common.Orders
{
	public class PlaceBuildingOrderGenerator : IOrderGenerator
	{
		readonly Actor producer;
		readonly string building;
		readonly BuildingInfo buildingInfo;
		readonly PlaceBuildingInfo placeBuildingInfo;
		readonly BuildingInfluence buildingInfluence;
		readonly string race;
		readonly Sprite buildOk;
		readonly Sprite buildBlocked;
		IActorPreview[] preview;

		bool initialized;

		public PlaceBuildingOrderGenerator(ProductionQueue queue, string name)
		{
			producer = queue.Actor;
			placeBuildingInfo = producer.Owner.PlayerActor.Info.Traits.Get<PlaceBuildingInfo>();
			building = name;

			// Clear selection if using Left-Click Orders
			if (Game.Settings.Game.UseClassicMouseStyle)
				producer.World.Selection.Clear();

			var map = producer.World.Map;
			var tileset = producer.World.TileSet.Id.ToLowerInvariant();

			var info = map.Rules.Actors[building];
			buildingInfo = info.Traits.Get<BuildingInfo>();

			var buildableInfo = info.Traits.Get<BuildableInfo>();
			var mostLikelyProducer = queue.MostLikelyProducer();
			race = buildableInfo.ForceRace ?? (mostLikelyProducer.Trait != null ? mostLikelyProducer.Trait.Race : producer.Owner.Faction.InternalName);

			buildOk = map.SequenceProvider.GetSequence("overlay", "build-valid-{0}".F(tileset)).GetSprite(0);
			buildBlocked = map.SequenceProvider.GetSequence("overlay", "build-invalid").GetSprite(0);

			buildingInfluence = producer.World.WorldActor.Trait<BuildingInfluence>();
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
				var orderType = "PlaceBuilding";
				var topLeft = xy - FootprintUtils.AdjustForBuildingSize(buildingInfo);

				var plugInfo = world.Map.Rules.Actors[building].Traits.GetOrDefault<PlugInfo>();
				if (plugInfo != null)
				{
					orderType = "PlacePlug";
					if (!AcceptsPlug(topLeft, plugInfo))
					{
						Sound.PlayNotification(world.Map.Rules, producer.Owner, "Speech", "BuildingCannotPlaceAudio", producer.Owner.Faction.InternalName);
						yield break;
					}
				}
				else
				{
					if (!world.CanPlaceBuilding(building, buildingInfo, topLeft, null)
						|| !buildingInfo.IsCloseEnoughToBase(world, producer.Owner, building, topLeft))
					{
						Sound.PlayNotification(world.Map.Rules, producer.Owner, "Speech", "BuildingCannotPlaceAudio", producer.Owner.Faction.InternalName);
						yield break;
					}

					if (world.Map.Rules.Actors[building].Traits.Contains<LineBuildInfo>())
						orderType = "LineBuild";
				}

				yield return new Order(orderType, producer.Owner.PlayerActor, false)
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

		bool AcceptsPlug(CPos cell, PlugInfo plug)
		{
			var host = buildingInfluence.GetBuildingAt(cell);
			if (host == null)
				return false;

			var location = host.Location;
			return host.TraitsImplementing<Pluggable>().Any(p => location + p.Info.Offset == cell && p.AcceptsPlug(host, plug.Type));
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

			var plugInfo = rules.Actors[building].Traits.GetOrDefault<PlugInfo>();
			if (plugInfo != null)
			{
				if (buildingInfo.Dimensions.X != 1 || buildingInfo.Dimensions.Y != 1)
					throw new InvalidOperationException("Plug requires a 1x1 sized Building");

				cells.Add(topLeft, AcceptsPlug(topLeft, plugInfo));
			}
			else if (rules.Actors[building].Traits.Contains<LineBuildInfo>())
			{
				// Linebuild for walls.
				if (buildingInfo.Dimensions.X != 1 || buildingInfo.Dimensions.Y != 1)
					throw new InvalidOperationException("LineBuild requires a 1x1 sized Building");

				foreach (var t in BuildingUtils.GetLineBuildCells(world, topLeft, building, buildingInfo))
					cells.Add(t, buildingInfo.IsCloseEnoughToBase(world, world.LocalPlayer, building, t));
			}
			else
			{
				if (!initialized)
				{
					var td = new TypeDictionary()
					{
						new FactionInit(race),
						new OwnerInit(producer.Owner),
						new HideBibPreviewInit()
					};

					var init = new ActorPreviewInitializer(rules.Actors[building], wr, td);
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

			var pal = wr.Palette(placeBuildingInfo.Palette);
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
