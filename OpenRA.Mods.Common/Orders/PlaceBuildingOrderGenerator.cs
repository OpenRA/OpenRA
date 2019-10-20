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
		IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, CPos topLeft);
	}

	public class PlaceBuildingOrderGenerator : IOrderGenerator
	{
		class VariantWrapper
		{
			public readonly ActorInfo ActorInfo;
			public readonly BuildingInfo BuildingInfo;
			public readonly PlugInfo PlugInfo;
			public readonly LineBuildInfo LineBuildInfo;
			public readonly IPlaceBuildingPreview Preview;

			public VariantWrapper(WorldRenderer wr, ProductionQueue queue, ActorInfo ai)
			{
				ActorInfo = ai;
				BuildingInfo = ActorInfo.TraitInfo<BuildingInfo>();
				PlugInfo = ActorInfo.TraitInfoOrDefault<PlugInfo>();
				LineBuildInfo = ActorInfo.TraitInfoOrDefault<LineBuildInfo>();

				var previewGeneratorInfo = ActorInfo.TraitInfoOrDefault<IPlaceBuildingPreviewGeneratorInfo>();
				if (previewGeneratorInfo != null)
				{
					string faction;
					var buildableInfo = ActorInfo.TraitInfoOrDefault<BuildableInfo>();
					if (buildableInfo != null && buildableInfo.ForceFaction != null)
						faction = buildableInfo.ForceFaction;
					else
					{
						var mostLikelyProducer = queue.MostLikelyProducer();
						faction = mostLikelyProducer.Trait != null ? mostLikelyProducer.Trait.Faction : queue.Actor.Owner.Faction.InternalName;
					}

					var td = new TypeDictionary()
					{
						new FactionInit(faction),
						new OwnerInit(queue.Actor.Owner),
					};

					foreach (var api in ActorInfo.TraitInfos<IActorPreviewInitInfo>())
						foreach (var o in api.ActorPreviewInits(ActorInfo, ActorPreviewType.PlaceBuilding))
							td.Add(o);

					Preview = previewGeneratorInfo.CreatePreview(wr, ActorInfo, td);
				}
			}
		}

		readonly ProductionQueue queue;
		readonly PlaceBuildingInfo placeBuildingInfo;
		readonly BuildingInfluence buildingInfluence;
		readonly ResourceLayer resourceLayer;
		readonly Viewport viewport;
		readonly VariantWrapper[] variants;
		int variant;

		public PlaceBuildingOrderGenerator(ProductionQueue queue, string name, WorldRenderer worldRenderer)
		{
			var world = queue.Actor.World;
			this.queue = queue;
			placeBuildingInfo = queue.Actor.Owner.PlayerActor.Info.TraitInfo<PlaceBuildingInfo>();
			buildingInfluence = world.WorldActor.Trait<BuildingInfluence>();
			resourceLayer = world.WorldActor.TraitOrDefault<ResourceLayer>();
			viewport = worldRenderer.Viewport;

			// Clear selection if using Left-Click Orders
			if (Game.Settings.Game.UseClassicMouseStyle)
				world.Selection.Clear();

			var variants = new List<VariantWrapper>()
			{
				new VariantWrapper(worldRenderer, queue, world.Map.Rules.Actors[name])
			};

			foreach (var v in variants[0].ActorInfo.TraitInfos<PlaceBuildingVariantsInfo>())
				foreach (var a in v.Actors)
					variants.Add(new VariantWrapper(worldRenderer, queue, world.Map.Rules.Actors[a]));

			this.variants = variants.ToArray();
		}

		PlaceBuildingCellType MakeCellType(bool valid, bool lineBuild = false)
		{
			var cell = valid ? PlaceBuildingCellType.Valid : PlaceBuildingCellType.Invalid;
			if (lineBuild)
				cell |= PlaceBuildingCellType.LineBuild;

			return cell;
		}

		public IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if ((mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Down) || (mi.Button == MouseButton.Right && mi.Event == MouseInputEvent.Up))
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

			return Enumerable.Empty<Order>();
		}

		CPos TopLeft
		{
			get
			{
				var offsetPos = Viewport.LastMousePos;
				if (variants[variant].Preview != null)
					offsetPos += variants[variant].Preview.TopLeftScreenOffset;

				return viewport.ViewToWorld(offsetPos);
			}
		}

		IEnumerable<Order> InnerOrder(World world, CPos cell, MouseInput mi)
		{
			if (world.Paused)
				yield break;

			var owner = queue.Actor.Owner;
			var ai = variants[variant].ActorInfo;
			var bi = variants[variant].BuildingInfo;

			if (mi.Button == MouseButton.Left)
			{
				var orderType = "PlaceBuilding";
				var topLeft = TopLeft;

				var plugInfo = ai.TraitInfoOrDefault<PlugInfo>();
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
					if (!world.CanPlaceBuilding(topLeft, ai, bi, null)
						|| !bi.IsCloseEnoughToBase(world, owner, ai, topLeft))
					{
						foreach (var order in ClearBlockersOrders(world, topLeft))
							yield return order;

						Game.Sound.PlayNotification(world.Map.Rules, owner, "Speech", placeBuildingInfo.CannotPlaceNotification, owner.Faction.InternalName);
						yield break;
					}

					if (ai.HasTraitInfo<LineBuildInfo>() && !mi.Modifiers.HasModifier(Modifiers.Shift))
						orderType = "LineBuild";
				}

				yield return new Order(orderType, owner.PlayerActor, Target.FromCell(world, topLeft), false)
				{
					// Building to place
					TargetString = variants[0].ActorInfo.Name,

					// Actor ID to associate with placement may be quite large, so it gets its own uint
					ExtraData = queue.Actor.ActorID,

					// Actor variant will always be small enough to safely pack in a CPos
					ExtraLocation = new CPos(variant, 0),

					SuppressVisualFeedback = true
				};
			}
		}

		void IOrderGenerator.Tick(World world)
		{
			if (queue.AllQueued().All(i => !i.Done || i.Item != variants[0].ActorInfo.Name))
				world.CancelInputMode();

			foreach (var v in variants)
				if (v.Preview != null)
					v.Preview.Tick();
		}

		bool AcceptsPlug(CPos cell, PlugInfo plug)
		{
			var host = buildingInfluence.GetBuildingAt(cell);
			if (host == null)
				return false;

			var location = host.Location;
			return host.TraitsImplementing<Pluggable>().Any(p => p.AcceptsPlug(host, plug.Type));
		}

		IEnumerable<IRenderable> IOrderGenerator.Render(WorldRenderer wr, World world) { yield break; }
		IEnumerable<IRenderable> IOrderGenerator.RenderAboveShroud(WorldRenderer wr, World world)
		{
			var topLeft = TopLeft;
			var footprint = new Dictionary<CPos, PlaceBuildingCellType>();
			var activeVariant = variants[variant];
			var actorInfo = activeVariant.ActorInfo;
			var buildingInfo = activeVariant.BuildingInfo;
			var plugInfo = activeVariant.PlugInfo;
			var lineBuildInfo = activeVariant.LineBuildInfo;
			var preview = activeVariant.Preview;

			if (plugInfo != null)
			{
				if (buildingInfo.Dimensions.X != 1 || buildingInfo.Dimensions.Y != 1)
					throw new InvalidOperationException("Plug requires a 1x1 sized Building");

				footprint.Add(topLeft, MakeCellType(AcceptsPlug(topLeft, plugInfo)));
			}
			else if (lineBuildInfo != null)
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
				var isCloseEnough = buildingInfo.IsCloseEnoughToBase(world, world.LocalPlayer, actorInfo, topLeft);
				foreach (var t in buildingInfo.Tiles(topLeft))
					footprint.Add(t, MakeCellType(isCloseEnough && world.IsCellBuildable(t, actorInfo, buildingInfo) && (resourceLayer == null || resourceLayer.GetResource(t) == null)));
			}

			return preview != null ? preview.Render(wr, topLeft, footprint) : Enumerable.Empty<IRenderable>();
		}

		IEnumerable<IRenderable> IOrderGenerator.RenderAnnotations(WorldRenderer wr, World world)
		{
			var preview = variants[variant].Preview;
			return preview != null ? preview.RenderAnnotations(wr, TopLeft) : Enumerable.Empty<IRenderable>();
		}

		string IOrderGenerator.GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi) { return "default"; }

		bool IOrderGenerator.HandleKeyPress(KeyInput e)
		{
			if (variants.Length > 0 && placeBuildingInfo.ToggleVariantKey.IsActivatedBy(e))
			{
				if (++variant >= variants.Length)
					variant = 0;

				return true;
			}

			return false;
		}

		void IOrderGenerator.Deactivate() { }

		IEnumerable<Order> ClearBlockersOrders(World world, CPos topLeft)
		{
			var allTiles = variants[variant].BuildingInfo.Tiles(topLeft).ToArray();
			var adjacentTiles = Util.ExpandFootprint(allTiles, true).Except(allTiles)
				.Where(world.Map.Contains).ToList();

			var blockers = allTiles.SelectMany(world.ActorMap.GetActorsAt)
				.Where(a => a.Owner == queue.Actor.Owner && a.IsIdle)
				.Select(a => new TraitPair<IMove>(a, a.TraitOrDefault<IMove>()))
				.Where(x => x.Trait != null);

			foreach (var blocker in blockers)
			{
				CPos moveCell;
				var mobile = blocker.Trait as Mobile;
				if (mobile != null)
				{
					var availableCells = adjacentTiles.Where(t => mobile.CanEnterCell(t)).ToList();
					if (availableCells.Count == 0)
						continue;

					moveCell = blocker.Actor.ClosestCell(availableCells);
				}
				else if (blocker.Trait is Aircraft)
					moveCell = blocker.Actor.Location;
				else
					continue;

				yield return new Order("Move", blocker.Actor, Target.FromCell(world, moveCell), false)
				{
					SuppressVisualFeedback = true
				};
			}
		}
	}
}
