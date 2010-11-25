#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
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
		public readonly int Range = 2; // Range in cells
		public readonly int Duration = 30;
		public readonly bool KillCargo = true;
		
		public override object Create(ActorInitializer init) { return new ChronoshiftPower(init.self,this); }
	}

	class ChronoshiftPower : SupportPower, IResolveOrder
	{	
		public ChronoshiftPower(Actor self, ChronoshiftPowerInfo info) : base(self, info) { }
		protected override void OnActivate() { Self.World.OrderGenerator = new SelectTarget(this); }

		public void ResolveOrder(Actor self, Order order)
		{
			if (!IsReady) return;

			if (order.OrderString == "Chronoshift")
			{
				var chronosphere = self.World.Queries
					.OwnedBy[self.Owner]
					.WithTrait<Chronosphere>()
					.Select(x => x.Actor).FirstOrDefault();
				
				if (chronosphere != null)
					chronosphere.Trait<RenderBuilding>().PlayCustomAnim(chronosphere, "active");
				
				// Trigger screen desaturate effect
				foreach (var a in self.World.Queries.WithTrait<ChronoshiftPaletteEffect>())
					a.Trait.Enable();
				
				Sound.Play("chrono2.aud", Game.CellSize * order.TargetLocation);
				Sound.Play("chrono2.aud", Game.CellSize * order.ExtraLocation);
				
				var targets = UnitsInRange(order.ExtraLocation);
				foreach (var target in targets)
				{									
					var cs = target.Trait<Chronoshiftable>();
					var targetCell = target.Location + order.TargetLocation - order.ExtraLocation;
					
					if (cs.CanChronoshiftTo(target, targetCell))
						target.Trait<Chronoshiftable>().Teleport(target,
						                                         targetCell,
						                                         (int)((Info as ChronoshiftPowerInfo).Duration * 25 * 60),
						                                         (Info as ChronoshiftPowerInfo).KillCargo,
						                                         chronosphere);
				}

				FinishActivate();
			}
		}

		public IEnumerable<Actor> UnitsInRange(int2 xy)
		{
			int range = (Info as ChronoshiftPowerInfo).Range;
			var uim = Self.World.WorldActor.Trait<UnitInfluence>();
			var tiles = Self.World.FindTilesInCircle(xy, range);
			var units = new List<Actor>();
			foreach (var t in tiles)
				units.AddRange(uim.GetUnitsAt(t));
			
			return units.Distinct().Where(a => a.HasTrait<Chronoshiftable>());
		}
		
		class SelectTarget : IOrderGenerator
		{
			ChronoshiftPower power;
			int range;
			Sprite tile;
			
			public SelectTarget(ChronoshiftPower power)
			{
				this.power = power;
				this.range = (power.Info as ChronoshiftPowerInfo).Range;
				tile = UiOverlay.SynthesizeTile(0x04);
			}
						
			public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Right)
					world.CancelInputMode();
				
				world.OrderGenerator = new SelectDestination(power, xy);
				yield break;
			}

			public void Tick( World world )
			{
				var hasChronosphere = world.Queries.OwnedBy[world.LocalPlayer]
					.WithTrait<Chronosphere>()
					.Any();

				if (!hasChronosphere)
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
			ChronoshiftPower power;
			int2 sourceLocation;
			int range;
			Sprite validTile, invalidTile, sourceTile;
			
			public SelectDestination(ChronoshiftPower power, int2 sourceLocation)
			{
				this.power = power;
				this.sourceLocation = sourceLocation;
				this.range = (power.Info as ChronoshiftPowerInfo).Range;
				validTile = UiOverlay.SynthesizeTile(0x0f);
				invalidTile = UiOverlay.SynthesizeTile(0x08);
				sourceTile = UiOverlay.SynthesizeTile(0x04);
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
				if (isValidTarget(xy))
					yield return new Order("Chronoshift", world.LocalPlayer.PlayerActor, false)
					{
						TargetLocation = xy,
						ExtraLocation = sourceLocation
					};
			}

			public void Tick(World world)
			{
				var hasChronosphere = world.Queries.OwnedBy[world.LocalPlayer]
					.WithTrait<Chronosphere>()
					.Any();

				if (!hasChronosphere)
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
						r.Sprite.DrawAt(wr, r.Pos - Traits.Util.CenterOfCell(unit.Location) + Traits.Util.CenterOfCell(targetCell),
						                r.Palette ?? unit.Owner.Palette);
				}
				
				// Unit tiles
				foreach (var unit in power.UnitsInRange(sourceLocation))
				{
					var targetCell = unit.Location + xy - sourceLocation;
					var canEnter = unit.Trait<Chronoshiftable>().CanChronoshiftTo(unit,targetCell);
					var tile = canEnter ? validTile : invalidTile;
					tile.DrawAt( wr, Game.CellSize * targetCell, "terrain" );
				}
			}

			bool isValidTarget(int2 xy)
			{
				var canTeleport = false;
				foreach (var unit in power.UnitsInRange(sourceLocation))
				{
					var targetCell = unit.Location + xy - sourceLocation;
					if (unit.Trait<Chronoshiftable>().CanChronoshiftTo(unit,targetCell))
					{
						canTeleport = true;
						break;
					}
				}
				return canTeleport;
			}
			public string GetCursor(World world, int2 xy, MouseInput mi)
			{
				return isValidTarget(xy) ? "chrono-target" : "move-blocked";
			}
		}
	}
	
	// tag trait for the building
	class ChronosphereInfo : TraitInfo<Chronosphere> {}
	class Chronosphere {}
}
