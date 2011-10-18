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
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;
using OpenRA.Mods.RA.Effects;

namespace OpenRA.Mods.RA
{
	class ChronoshiftPowerInfo : SupportPowerInfo
	{
		public readonly int Range = 1; // Range in cells
		public readonly int Duration = 30; // Seconds
		public readonly bool KillCargo = true;

		public override object Create(ActorInitializer init) { return new ChronoshiftPower(init.self,this); }
	}

	class ChronoshiftPower : SupportPower
	{
		public ChronoshiftPower(Actor self, ChronoshiftPowerInfo info) : base(self, info) { }

		public override IOrderGenerator OrderGenerator(string order, SupportPowerManager manager)
		{
			Sound.PlayToPlayer(manager.self.Owner, Info.SelectTargetSound);
			return new SelectTarget(order, manager, this);
		}

		public override void Activate(Actor self, Order order)
		{
			self.Trait<RenderBuilding>().PlayCustomAnim(self, "active");

			// Trigger screen desaturate effect
			foreach (var a in self.World.ActorsWithTrait<ChronoshiftPaletteEffect>())
				a.Trait.Enable();

			Sound.Play("chrono2.aud", Game.CellSize * order.TargetLocation);
			Sound.Play("chrono2.aud", Game.CellSize * order.ExtraLocation);
			foreach (var target in UnitsInRange(order.ExtraLocation))
			{
				var cs = target.Trait<Chronoshiftable>();
				var targetCell = target.Location + order.TargetLocation - order.ExtraLocation;
				var cpi = Info as ChronoshiftPowerInfo;

				if (cs.CanChronoshiftTo(target, targetCell, true))
					cs.Teleport(target, targetCell,
						cpi.Duration * 25, cpi.KillCargo, self);
			}
		}

		public IEnumerable<Actor> UnitsInRange(int2 xy)
		{
			int range = (Info as ChronoshiftPowerInfo).Range;
			var tiles = self.World.FindTilesInCircle(xy, range);
			var units = new List<Actor>();
			foreach (var t in tiles)
				units.AddRange(self.World.ActorMap.GetUnitsAt(t));

			return units.Distinct().Where(a => a.HasTrait<Chronoshiftable>());
		}

		class SelectTarget : IOrderGenerator
		{
			readonly ChronoshiftPower power;
			readonly int range;
			readonly Sprite tile;
			readonly SupportPowerManager manager;
			readonly string order;

			public SelectTarget(string order, SupportPowerManager manager, ChronoshiftPower power)
			{
				this.manager = manager;
				this.order = order;
				this.power = power;
				this.range = (power.Info as ChronoshiftPowerInfo).Range;
				tile = SequenceProvider.GetSequence("overlay", "target-select").GetSprite(0);
			}

			public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
			{
				world.CancelInputMode();
				if (mi.Button == MouseButton.Left)
					world.OrderGenerator = new SelectDestination(order, manager, power, xy);

				yield break;
			}

			public void Tick(World world)
			{
				// Cancel the OG if we can't use the power
				if (!manager.Powers.ContainsKey(order))
					world.CancelInputMode();
			}

			public void RenderAfterWorld(WorldRenderer wr, World world)
			{
				var xy = Game.viewport.ViewToWorld(Viewport.LastMousePos).ToInt2();
				var targetUnits = power.UnitsInRange(xy);
				foreach (var unit in targetUnits)
					wr.DrawSelectionBox(unit, Color.Red);
			}

			public void RenderBeforeWorld(WorldRenderer wr, World world)
			{
				var xy = Game.viewport.ViewToWorld(Viewport.LastMousePos).ToInt2();
				var tiles = world.FindTilesInCircle(xy, range);
				foreach (var t in tiles)
					tile.DrawAt( wr, Game.CellSize * t, "terrain" );
			}

			public string GetCursor(World world, int2 xy, MouseInput mi)
			{
				return "chrono-select";
			}
		}

		class SelectDestination : IOrderGenerator
		{
			readonly ChronoshiftPower power;
			readonly int2 sourceLocation;
			readonly int range;
			readonly Sprite validTile, invalidTile, sourceTile;
			readonly SupportPowerManager manager;
			readonly string order;

			public SelectDestination(string order, SupportPowerManager manager, ChronoshiftPower power, int2 sourceLocation)
			{
				this.manager = manager;
				this.order = order;
				this.power = power;
				this.sourceLocation = sourceLocation;
				this.range = (power.Info as ChronoshiftPowerInfo).Range;

				validTile = SequenceProvider.GetSequence("overlay", "target-valid").GetSprite(0);
				invalidTile = SequenceProvider.GetSequence("overlay", "target-invalid").GetSprite(0);
				sourceTile = SequenceProvider.GetSequence("overlay", "target-select").GetSprite(0);
			}

			public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Right)
				{
					world.CancelInputMode();
					yield break;
				}

				var ret = OrderInner( world, xy, mi ).FirstOrDefault();
				if (ret == null)
					yield break;

				world.CancelInputMode();
				yield return ret;
			}

			IEnumerable<Order> OrderInner(World world, int2 xy, MouseInput mi)
			{
				// Cannot chronoshift into unexplored location
				if (IsValidTarget(xy))
					yield return new Order(order, manager.self, false)
					{
						TargetLocation = xy,
						ExtraLocation = sourceLocation
					};
			}

			public void Tick(World world)
			{
				// Cancel the OG if we can't use the power
				if (!manager.Powers.ContainsKey(order))
					world.CancelInputMode();
			}

			public void RenderAfterWorld(WorldRenderer wr, World world)
			{
				foreach (var unit in power.UnitsInRange(sourceLocation))
					wr.DrawSelectionBox(unit, Color.Red);
			}

			public void RenderBeforeWorld(WorldRenderer wr, World world)
			{
				var xy = Game.viewport.ViewToWorld(Viewport.LastMousePos).ToInt2();

				// Source tiles
				foreach (var t in world.FindTilesInCircle(sourceLocation, range))
					sourceTile.DrawAt( wr, Game.CellSize * t, "terrain" );

				// Destination tiles
				foreach (var t in world.FindTilesInCircle(xy, range))
					sourceTile.DrawAt( wr, Game.CellSize * t, "terrain" );

				// Unit previews
				foreach (var unit in power.UnitsInRange(sourceLocation))
				{
					var targetCell = unit.Location + xy - sourceLocation;
					foreach (var r in unit.Render())
						r.Sprite.DrawAt(r.Pos - Traits.Util.CenterOfCell(unit.Location) + Traits.Util.CenterOfCell(targetCell),
							wr.GetPaletteIndex(r.Palette),
							r.Scale*r.Sprite.size);
				}

				// Unit tiles
				foreach (var unit in power.UnitsInRange(sourceLocation))
				{
					var targetCell = unit.Location + xy - sourceLocation;
					var canEnter = unit.Trait<Chronoshiftable>().CanChronoshiftTo(unit,targetCell, false);
					var tile = canEnter ? validTile : invalidTile;
					tile.DrawAt( wr, Game.CellSize * targetCell, "terrain" );
				}
			}

			bool IsValidTarget(int2 xy)
			{
				var canTeleport = false;
				foreach (var unit in power.UnitsInRange(sourceLocation))
				{
					var targetCell = unit.Location + xy - sourceLocation;
					if (unit.Trait<Chronoshiftable>().CanChronoshiftTo(unit,targetCell, false))
					{
						canTeleport = true;
						break;
					}
				}
				return canTeleport;
			}

			public string GetCursor(World world, int2 xy, MouseInput mi)
			{
				return IsValidTarget(xy) ? "chrono-target" : "move-blocked";
			}
		}
	}
}
