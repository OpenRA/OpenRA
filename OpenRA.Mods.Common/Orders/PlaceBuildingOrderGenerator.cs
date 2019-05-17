#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
	[Flags]
	public enum PlaceBuildingCellType { None = 0, Valid = 1, Invalid = 2, LineBuild = 4 }

	[RequireExplicitImplementation]
	public interface IPlaceBuildingPreviewGeneratorInfo : ITraitInfoInterface
	{
		IPlaceBuildingPreview CreatePreview(WorldRenderer wr, ActorInfo ai, TypeDictionary init);
	}

	public interface IPlaceBuildingPreview
	{
		int2 TopLeftScreenOffset { get; }
		void Tick();
		IEnumerable<IRenderable> Render(WorldRenderer wr, CPos topLeft, Dictionary<CPos, PlaceBuildingCellType> footprint);
	}

	public class PlaceBuildingOrderGenerator : OrderGenerator
	{
		readonly ProductionQueue queue;
		readonly PlaceBuildingInfo placeBuildingInfo;
		readonly BuildingInfluence buildingInfluence;
		readonly Viewport viewport;
		readonly ActorInfo actorInfo;
		readonly BuildingInfo buildingInfo;
		readonly IPlaceBuildingPreview preview;

		public PlaceBuildingOrderGenerator(ProductionQueue queue, string name, WorldRenderer worldRenderer)
		{
			var world = queue.Actor.World;
			this.queue = queue;
			placeBuildingInfo = queue.Actor.Owner.PlayerActor.Info.TraitInfo<PlaceBuildingInfo>();
			buildingInfluence = world.WorldActor.Trait<BuildingInfluence>();
			viewport = worldRenderer.Viewport;

			// Clear selection if using Left-Click Orders
			if (Game.Settings.Game.UseClassicMouseStyle)
				world.Selection.Clear();

			actorInfo = world.Map.Rules.Actors[name];
			buildingInfo = actorInfo.TraitInfo<BuildingInfo>();

			var previewGeneratorInfo = actorInfo.TraitInfoOrDefault<IPlaceBuildingPreviewGeneratorInfo>();
			if (previewGeneratorInfo != null)
			{
				var faction = actorInfo.TraitInfo<BuildableInfo>().ForceFaction;
				if (faction == null)
				{
					var mostLikelyProducer = queue.MostLikelyProducer();
					faction = mostLikelyProducer.Trait != null ? mostLikelyProducer.Trait.Faction : queue.Actor.Owner.Faction.InternalName;
				}

				var td = new TypeDictionary()
				{
					new FactionInit(faction),
					new OwnerInit(queue.Actor.Owner),
				};

				foreach (var api in actorInfo.TraitInfos<IActorPreviewInitInfo>())
					foreach (var o in api.ActorPreviewInits(actorInfo, ActorPreviewType.PlaceBuilding))
						td.Add(o);

				preview = previewGeneratorInfo.CreatePreview(worldRenderer, actorInfo, td);
			}
		}

		PlaceBuildingCellType MakeCellType(bool valid, bool lineBuild = false)
		{
			var cell = valid ? PlaceBuildingCellType.Valid : PlaceBuildingCellType.Invalid;
			if (lineBuild)
				cell |= PlaceBuildingCellType.LineBuild;

			return cell;
		}

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
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

		CPos TopLeft
		{
			get
			{
				var offsetPos = Viewport.LastMousePos;
				if (preview != null)
					offsetPos += preview.TopLeftScreenOffset;

				return viewport.ViewToWorld(offsetPos);
			}
		}

		IEnumerable<Order> InnerOrder(World world, CPos cell, MouseInput mi)
		{
			if (world.Paused)
				yield break;

			var owner = queue.Actor.Owner;
			if (mi.Button == MouseButton.Left)
			{
				var orderType = "PlaceBuilding";
				var topLeft = TopLeft;

				var plugInfo = actorInfo.TraitInfoOrDefault<PlugInfo>();
				if (plugInfo != null)
				{
					orderType = "PlacePlug";
					if (!AcceptsPlug(topLeft, plugInfo))
					{
						Game.Sound.PlayNotification(world.Map.Rules, owner, "Speech", placeBuildingInfo.CannotPlaceNotification, owner.Faction.InternalName);
						yield break;
					}
				}
				else
				{
					if (!world.CanPlaceBuilding(topLeft, actorInfo, buildingInfo, null)
						|| !buildingInfo.IsCloseEnoughToBase(world, owner, actorInfo, topLeft))
					{
						foreach (var order in ClearBlockersOrders(world, topLeft))
							yield return order;

						Game.Sound.PlayNotification(world.Map.Rules, owner, "Speech", placeBuildingInfo.CannotPlaceNotification, owner.Faction.InternalName);
						yield break;
					}

					if (actorInfo.HasTraitInfo<LineBuildInfo>() && !mi.Modifiers.HasModifier(Modifiers.Shift))
						orderType = "LineBuild";
				}

				yield return new Order(orderType, owner.PlayerActor, Target.FromCell(world, topLeft), false)
				{
					// Building to place
					TargetString = actorInfo.Name,

					// Actor to associate the placement with
					ExtraData = queue.Actor.ActorID,
					SuppressVisualFeedback = true
				};
			}
		}

		protected override void Tick(World world)
		{
			if (queue.AllQueued().All(i => !i.Done || i.Item != actorInfo.Name))
				world.CancelInputMode();

			if (preview != null)
				preview.Tick();
		}

		bool AcceptsPlug(CPos cell, PlugInfo plug)
		{
			var host = buildingInfluence.GetBuildingAt(cell);
			if (host == null)
				return false;

			var location = host.Location;
			return host.TraitsImplementing<Pluggable>().Any(p => location + p.Info.Offset == cell && p.AcceptsPlug(host, plug.Type));
		}

		protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world)
		{
			var topLeft = TopLeft;
			var footprint = new Dictionary<CPos, PlaceBuildingCellType>();
			var plugInfo = actorInfo.TraitInfoOrDefault<PlugInfo>();
			if (plugInfo != null)
			{
				if (buildingInfo.Dimensions.X != 1 || buildingInfo.Dimensions.Y != 1)
					throw new InvalidOperationException("Plug requires a 1x1 sized Building");

				footprint.Add(topLeft, MakeCellType(AcceptsPlug(topLeft, plugInfo)));
			}
			else if (actorInfo.HasTraitInfo<LineBuildInfo>())
			{
				// Linebuild for walls.
				if (buildingInfo.Dimensions.X != 1 || buildingInfo.Dimensions.Y != 1)
					throw new InvalidOperationException("LineBuild requires a 1x1 sized Building");

				if (!Game.GetModifierKeys().HasModifier(Modifiers.Shift))
				{
					foreach (var t in BuildingUtils.GetLineBuildCells(world, topLeft, actorInfo, buildingInfo))
					{
						var lineBuildable = world.IsCellBuildable(t.First, actorInfo, buildingInfo);
						var lineCloseEnough = buildingInfo.IsCloseEnoughToBase(world, world.LocalPlayer, actorInfo, t.First);
						footprint.Add(t.First, MakeCellType(lineBuildable && lineCloseEnough, true));
					}
				}

				var buildable = world.IsCellBuildable(topLeft, actorInfo, buildingInfo);
				var closeEnough = buildingInfo.IsCloseEnoughToBase(world, world.LocalPlayer, actorInfo, topLeft);
				footprint[topLeft] = MakeCellType(buildable && closeEnough);
			}
			else
			{
				var res = world.WorldActor.TraitOrDefault<ResourceLayer>();
				var isCloseEnough = buildingInfo.IsCloseEnoughToBase(world, world.LocalPlayer, actorInfo, topLeft);
				foreach (var t in buildingInfo.Tiles(topLeft))
					footprint.Add(t, MakeCellType(isCloseEnough && world.IsCellBuildable(t, actorInfo, buildingInfo) && (res == null || res.GetResource(t) == null)));
			}

			return preview != null ? preview.Render(wr, topLeft, footprint) : Enumerable.Empty<IRenderable>();
		}

		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi) { return "default"; }

		IEnumerable<Order> ClearBlockersOrders(World world, CPos topLeft)
		{
			var allTiles = buildingInfo.Tiles(topLeft).ToArray();
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

				yield return new Order("Move", blocker.Actor, Target.FromCell(world, blocker.Actor.ClosestCell(availableCells)), false)
				{
					SuppressVisualFeedback = true
				};
			}
		}
	}
}
