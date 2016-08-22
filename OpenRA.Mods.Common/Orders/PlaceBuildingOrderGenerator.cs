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
		readonly ProductionQueue queue;
		readonly string building;
		readonly BuildingInfo buildingInfo;
		readonly PlaceBuildingInfo placeBuildingInfo;
		readonly BuildingInfluence buildingInfluence;
		readonly string faction;
		readonly Sprite buildOk;
		readonly Sprite buildBlocked;
		IActorPreview[] preview;

		bool initialized;

		public PlaceBuildingOrderGenerator(ProductionQueue queue, string name)
		{
			var world = queue.Actor.World;
			this.queue = queue;
			placeBuildingInfo = queue.Actor.Owner.PlayerActor.Info.TraitInfo<PlaceBuildingInfo>();
			building = name;

			// Clear selection if using Left-Click Orders
			if (Game.Settings.Game.UseClassicMouseStyle)
				world.Selection.Clear();

			var map = world.Map;
			var tileset = world.Map.Tileset.ToLowerInvariant();

			var info = map.Rules.Actors[building];
			buildingInfo = info.TraitInfo<BuildingInfo>();

			var buildableInfo = info.TraitInfo<BuildableInfo>();
			var mostLikelyProducer = queue.MostLikelyProducer();
			faction = buildableInfo.ForceFaction
				?? (mostLikelyProducer.Trait != null ? mostLikelyProducer.Trait.Faction : queue.Actor.Owner.Faction.InternalName);

			buildOk = map.Rules.Sequences.GetSequence("overlay", "build-valid-{0}".F(tileset)).GetSprite(0);
			buildBlocked = map.Rules.Sequences.GetSequence("overlay", "build-invalid").GetSprite(0);

			buildingInfluence = world.WorldActor.Trait<BuildingInfluence>();
		}

		public IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
				world.CancelInputMode();

			var ret = InnerOrder(world, cell, mi).ToArray();

			// If there was a successful placement order
			if (ret.Any(o => o.OrderString == "PlaceBuilding"
				|| o.OrderString == "LineBuild"
				|| o.OrderString == "PlacePlug"))
				world.CancelInputMode();

			return ret;
		}

		IEnumerable<Order> InnerOrder(World world, CPos cell, MouseInput mi)
		{
			if (world.Paused)
				yield break;

			var owner = queue.Actor.Owner;
			if (mi.Button == MouseButton.Left)
			{
				var orderType = "PlaceBuilding";
				var topLeft = cell - FootprintUtils.AdjustForBuildingSize(buildingInfo);

				var plugInfo = world.Map.Rules.Actors[building].TraitInfoOrDefault<PlugInfo>();
				if (plugInfo != null)
				{
					orderType = "PlacePlug";
					if (!AcceptsPlug(topLeft, plugInfo))
					{
						Game.Sound.PlayNotification(world.Map.Rules, owner, "Speech", "BuildingCannotPlaceAudio", owner.Faction.InternalName);
						yield break;
					}
				}
				else
				{
					if (!world.CanPlaceBuilding(building, buildingInfo, topLeft, null)
						|| !buildingInfo.IsCloseEnoughToBase(world, owner, building, topLeft))
					{
						foreach (var order in ClearBlockersOrders(world, topLeft))
							yield return order;

						Game.Sound.PlayNotification(world.Map.Rules, owner, "Speech", "BuildingCannotPlaceAudio", owner.Faction.InternalName);
						yield break;
					}

					if (world.Map.Rules.Actors[building].HasTraitInfo<LineBuildInfo>() && !mi.Modifiers.HasModifier(Modifiers.Shift))
						orderType = "LineBuild";
				}

				yield return new Order(orderType, owner.PlayerActor, false)
				{
					TargetLocation = topLeft,
					TargetActor = queue.Actor,
					TargetString = building,
					SuppressVisualFeedback = true
				};
			}
		}

		public void Tick(World world)
		{
			if (queue.CurrentItem() == null || queue.CurrentItem().Item != building)
				world.CancelInputMode();

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
		public IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world)
		{
			var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
			var topLeft = xy - FootprintUtils.AdjustForBuildingSize(buildingInfo);
			var offset = world.Map.CenterOfCell(topLeft) + FootprintUtils.CenterOffset(world, buildingInfo);
			var rules = world.Map.Rules;

			var actorInfo = rules.Actors[building];
			foreach (var dec in actorInfo.TraitInfos<IPlaceBuildingDecorationInfo>())
				foreach (var r in dec.Render(wr, world, actorInfo, offset))
					yield return r;

			var cells = new Dictionary<CPos, bool>();

			var plugInfo = rules.Actors[building].TraitInfoOrDefault<PlugInfo>();
			if (plugInfo != null)
			{
				if (buildingInfo.Dimensions.X != 1 || buildingInfo.Dimensions.Y != 1)
					throw new InvalidOperationException("Plug requires a 1x1 sized Building");

				cells.Add(topLeft, AcceptsPlug(topLeft, plugInfo));
			}
			else if (rules.Actors[building].HasTraitInfo<LineBuildInfo>())
			{
				// Linebuild for walls.
				if (buildingInfo.Dimensions.X != 1 || buildingInfo.Dimensions.Y != 1)
					throw new InvalidOperationException("LineBuild requires a 1x1 sized Building");

				if (!Game.GetModifierKeys().HasModifier(Modifiers.Shift))
					foreach (var t in BuildingUtils.GetLineBuildCells(world, topLeft, building, buildingInfo))
						cells.Add(t, buildingInfo.IsCloseEnoughToBase(world, world.LocalPlayer, building, t));
				else
					cells.Add(topLeft, buildingInfo.IsCloseEnoughToBase(world, world.LocalPlayer, building, topLeft));
			}
			else
			{
				if (!initialized)
				{
					var td = new TypeDictionary()
					{
						new FactionInit(faction),
						new OwnerInit(queue.Actor.Owner),
						new HideBibPreviewInit()
					};

					var init = new ActorPreviewInitializer(rules.Actors[building], wr, td);
					preview = rules.Actors[building].TraitInfos<IRenderActorPreviewInfo>()
						.SelectMany(rpi => rpi.RenderPreview(init))
						.ToArray();

					initialized = true;
				}

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
			var topLeftPos = world.Map.CenterOfCell(topLeft);
			foreach (var c in cells)
			{
				var tile = c.Value ? buildOk : buildBlocked;
				var pos = world.Map.CenterOfCell(c.Key);
				yield return new SpriteRenderable(tile, pos, new WVec(0, 0, topLeftPos.Z - pos.Z),
					-511, pal, 1f, true);
			}
		}

		public string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi) { return "default"; }

		IEnumerable<Order> ClearBlockersOrders(World world, CPos topLeft)
		{
			var allTiles = FootprintUtils.Tiles(world.Map.Rules, building, buildingInfo, topLeft).ToArray();
			var neightborTiles = Util.ExpandFootprint(allTiles, true).Except(allTiles)
				.Where(world.Map.Contains).ToList();

			var blockers = allTiles.SelectMany(world.ActorMap.GetActorsAt)
				.Where(a => a.Owner == queue.Actor.Owner && a.IsIdle)
				.Select(a => new TraitPair<Mobile>(a, a.TraitOrDefault<Mobile>()));

			foreach (var blocker in blockers.Where(x => x.Trait != null))
			{
				var availableCells = neightborTiles.Where(t => blocker.Trait.CanEnterCell(t)).ToList();
				if (availableCells.Count == 0)
					continue;

				yield return new Order("Move", blocker.Actor, false)
				{
					TargetLocation = blocker.Actor.ClosestCell(availableCells)
				};
			}
		}
	}
}
