#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA.Activities;

namespace OpenRA.Mods.RA.Traits
{
	class ChronoshiftPowerInfo : SupportPowerInfo
	{
		[Desc("Target actor selection radius in cells.")]
		public readonly int Range = 1;

		[Desc("Seconds until returning after teleportation.")]
		public readonly int Duration = 30;

		public readonly bool KillCargo = true;

		public override object Create(ActorInitializer init) { return new ChronoshiftPower(init.Self, this); }
	}

	class ChronoshiftPower : SupportPower
	{
		public ChronoshiftPower(Actor self, ChronoshiftPowerInfo info) : base(self, info) { }

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			Game.Sound.PlayToPlayer(manager.Self.Owner, Info.SelectTargetSound);
			self.World.OrderGenerator = new SelectChronoshiftTarget(Self.World, order, manager, this);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			foreach (var target in UnitsInRange(order.ExtraLocation))
			{
				var cs = target.Trait<Chronoshiftable>();
				var targetCell = target.Location + (order.TargetLocation - order.ExtraLocation);
				var cpi = Info as ChronoshiftPowerInfo;

				if (self.Owner.Shroud.IsExplored(targetCell) && cs.CanChronoshiftTo(target, targetCell))
					cs.Teleport(target, targetCell, cpi.Duration * 25, cpi.KillCargo, self);
			}
		}

		public IEnumerable<Actor> UnitsInRange(CPos xy)
		{
			var range = ((ChronoshiftPowerInfo)Info).Range;
			var tiles = Self.World.Map.FindTilesInCircle(xy, range);
			var units = new HashSet<Actor>();
			foreach (var t in tiles)
				units.UnionWith(Self.World.ActorMap.GetUnitsAt(t));

			return units.Where(a => a.Info.HasTraitInfo<ChronoshiftableInfo>() &&
				!a.TraitsImplementing<IPreventsTeleport>().Any(condition => condition.PreventsTeleport(a)));
		}

		public bool SimilarTerrain(CPos xy, CPos sourceLocation)
		{
			if (!Self.Owner.Shroud.IsExplored(xy))
				return false;

			var range = ((ChronoshiftPowerInfo)Info).Range;
			var sourceTiles = Self.World.Map.FindTilesInCircle(xy, range);
			var destTiles = Self.World.Map.FindTilesInCircle(sourceLocation, range);

			using (var se = sourceTiles.GetEnumerator())
			using (var de = destTiles.GetEnumerator())
				while (se.MoveNext() && de.MoveNext())
				{
					var a = se.Current;
					var b = de.Current;

					if (!Self.Owner.Shroud.IsExplored(a) || !Self.Owner.Shroud.IsExplored(b))
						return false;

					if (Self.World.Map.GetTerrainIndex(a) != Self.World.Map.GetTerrainIndex(b))
						return false;
				}

			return true;
		}

		class SelectChronoshiftTarget : IOrderGenerator
		{
			readonly ChronoshiftPower power;
			readonly int range;
			readonly Sprite tile;
			readonly SupportPowerManager manager;
			readonly string order;

			public SelectChronoshiftTarget(World world, string order, SupportPowerManager manager, ChronoshiftPower power)
			{
				// Clear selection if using Left-Click Orders
				if (Game.Settings.Game.UseClassicMouseStyle)
					manager.Self.World.Selection.Clear();

				this.manager = manager;
				this.order = order;
				this.power = power;
				this.range = ((ChronoshiftPowerInfo)power.Info).Range;
				tile = world.Map.SequenceProvider.GetSequence("overlay", "target-select").GetSprite(0);
			}

			public IEnumerable<Order> Order(World world, CPos xy, MouseInput mi)
			{
				world.CancelInputMode();
				if (mi.Button == MouseButton.Left)
					world.OrderGenerator = new SelectDestination(world, order, manager, power, xy);

				yield break;
			}

			public void Tick(World world)
			{
				// Cancel the OG if we can't use the power
				if (!manager.Powers.ContainsKey(order))
					world.CancelInputMode();
			}

			public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr, World world)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				var targetUnits = power.UnitsInRange(xy).Where(a => !world.FogObscures(a));

				foreach (var unit in targetUnits)
					if (manager.Self.Owner.CanTargetActor(unit))
						yield return new SelectionBoxRenderable(unit, Color.Red);
			}

			public IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				var tiles = world.Map.FindTilesInCircle(xy, range);
				var pal = wr.Palette("terrain");
				foreach (var t in tiles)
					yield return new SpriteRenderable(tile, wr.World.Map.CenterOfCell(t), WVec.Zero, -511, pal, 1f, true);
			}

			public string GetCursor(World world, CPos xy, MouseInput mi)
			{
				return "chrono-select";
			}
		}

		class SelectDestination : IOrderGenerator
		{
			readonly ChronoshiftPower power;
			readonly CPos sourceLocation;
			readonly int range;
			readonly Sprite validTile, invalidTile, sourceTile;
			readonly SupportPowerManager manager;
			readonly string order;

			public SelectDestination(World world, string order, SupportPowerManager manager, ChronoshiftPower power, CPos sourceLocation)
			{
				this.manager = manager;
				this.order = order;
				this.power = power;
				this.sourceLocation = sourceLocation;
				this.range = ((ChronoshiftPowerInfo)power.Info).Range;

				var tileset = manager.Self.World.TileSet.Id.ToLowerInvariant();
				validTile = world.Map.SequenceProvider.GetSequence("overlay", "target-valid-{0}".F(tileset)).GetSprite(0);
				invalidTile = world.Map.SequenceProvider.GetSequence("overlay", "target-invalid").GetSprite(0);
				sourceTile = world.Map.SequenceProvider.GetSequence("overlay", "target-select").GetSprite(0);
			}

			public IEnumerable<Order> Order(World world, CPos xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Right)
				{
					world.CancelInputMode();
					yield break;
				}

				var ret = OrderInner(xy).FirstOrDefault();
				if (ret == null)
					yield break;

				world.CancelInputMode();
				yield return ret;
			}

			IEnumerable<Order> OrderInner(CPos xy)
			{
				// Cannot chronoshift into unexplored location
				if (IsValidTarget(xy))
					yield return new Order(order, manager.Self, false)
					{
						TargetLocation = xy,
						ExtraLocation = sourceLocation,
						SuppressVisualFeedback = true
					};
			}

			public void Tick(World world)
			{
				// Cancel the OG if we can't use the power
				if (!manager.Powers.ContainsKey(order))
					world.CancelInputMode();
			}

			public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr, World world)
			{
				foreach (var unit in power.UnitsInRange(sourceLocation))
					if (manager.Self.Owner.CanTargetActor(unit))
						yield return new SelectionBoxRenderable(unit, Color.Red);
			}

			public IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				var pal = wr.Palette("terrain");

				// Source tiles
				foreach (var t in world.Map.FindTilesInCircle(sourceLocation, range))
					yield return new SpriteRenderable(sourceTile, wr.World.Map.CenterOfCell(t), WVec.Zero, -511, pal, 1f, true);

				// Destination tiles
				foreach (var t in world.Map.FindTilesInCircle(xy, range))
					yield return new SpriteRenderable(sourceTile, wr.World.Map.CenterOfCell(t), WVec.Zero, -511, pal, 1f, true);

				// Unit previews
				foreach (var unit in power.UnitsInRange(sourceLocation))
				{
					var offset = world.Map.CenterOfCell(xy) - world.Map.CenterOfCell(sourceLocation);
					if (manager.Self.Owner.CanTargetActor(unit))
						foreach (var r in unit.Render(wr))
							yield return r.OffsetBy(offset);
				}

				// Unit tiles
				foreach (var unit in power.UnitsInRange(sourceLocation))
				{
					if (manager.Self.Owner.CanTargetActor(unit))
					{
						var targetCell = unit.Location + (xy - sourceLocation);
						var canEnter = manager.Self.Owner.Shroud.IsExplored(targetCell) &&
							unit.Trait<Chronoshiftable>().CanChronoshiftTo(unit, targetCell);
						var tile = canEnter ? validTile : invalidTile;
						yield return new SpriteRenderable(tile, wr.World.Map.CenterOfCell(targetCell), WVec.Zero, -511, pal, 1f, true);
					}
				}
			}

			bool IsValidTarget(CPos xy)
			{
				var canTeleport = false;
				foreach (var unit in power.UnitsInRange(sourceLocation))
				{
					var targetCell = unit.Location + (xy - sourceLocation);
					if (manager.Self.Owner.Shroud.IsExplored(targetCell) && unit.Trait<Chronoshiftable>().CanChronoshiftTo(unit, targetCell))
					{
						canTeleport = true;
						break;
					}
				}

				if (!canTeleport)
				{
					// Check the terrain types. This will allow chronoshifts to occur on empty terrain to terrain of
					// a similar type. This also keeps the cursor from changing in non-visible property, alerting the
					// chronoshifter of enemy unit presence
					canTeleport = power.SimilarTerrain(sourceLocation, xy);
				}

				return canTeleport;
			}

			public string GetCursor(World world, CPos xy, MouseInput mi)
			{
				return IsValidTarget(xy) ? "chrono-target" : "move-blocked";
			}
		}
	}
}
