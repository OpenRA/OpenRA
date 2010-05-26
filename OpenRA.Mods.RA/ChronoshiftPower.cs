#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ChronoshiftPowerInfo : SupportPowerInfo
	{
		public readonly float Duration = 0f;
		public readonly bool KillCargo = true;
		public override object Create(Actor self) { return new ChronoshiftPower(self,this); }
	}

	class ChronoshiftPower : SupportPower, IResolveOrder
	{	
		public ChronoshiftPower(Actor self, ChronoshiftPowerInfo info) : base(self, info) { }
		protected override void OnActivate() { Game.controller.orderGenerator = new SelectTarget(); }

		public void ResolveOrder(Actor self, Order order)
		{
			if (!IsAvailable) return;

			if (order.OrderString == "ChronosphereSelect" && self.Owner == self.World.LocalPlayer)
			{
				Game.controller.orderGenerator = new SelectDestination(order.TargetActor);
			}
			
			if (order.OrderString == "ChronosphereActivate")
			{
				if (self.Owner == self.World.LocalPlayer)
					Game.controller.CancelInputMode();
				
				// Ensure the target cell is valid for the unit
				var movement = order.TargetActor.traits.GetOrDefault<IMovement>();
				if (!movement.CanEnterCell(order.TargetLocation))
					return;

				var chronosphere = self.World.Queries
					.OwnedBy[self.Owner]
					.WithTrait<Chronosphere>()
					.Select(x => x.Actor).FirstOrDefault();
				
				bool success = order.TargetActor.traits.Get<Chronoshiftable>().Activate(order.TargetActor,
					order.TargetLocation,
					(int)((Info as ChronoshiftPowerInfo).Duration * 25 * 60),
					(Info as ChronoshiftPowerInfo).KillCargo,
					chronosphere);
					
				if (success)
				{
					Sound.Play("chrono2.aud", chronosphere.CenterLocation);
					
					// Trigger screen desaturate effect
					foreach (var a in self.World.Queries.WithTrait<ChronoshiftPaletteEffect>())
						a.Trait.DoChronoshift();

					if (chronosphere != null)
						chronosphere.traits.Get<RenderBuilding>().PlayCustomAnim(chronosphere, "active");
				}

				FinishActivate();
			}
		}

		class SelectTarget : IOrderGenerator
		{
			public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Right)
					Game.controller.CancelInputMode();

				return OrderInner(world, xy, mi);
			}

			IEnumerable<Order> OrderInner(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Left)
				{
					var underCursor = world.FindUnitsAtMouse(mi.Location)
						.Where(a => a.Owner != null && a.traits.Contains<Chronoshiftable>()
							&& a.traits.Contains<Selectable>()).FirstOrDefault();

					if (underCursor != null)
						yield return new Order("ChronosphereSelect", world.LocalPlayer.PlayerActor, underCursor);
				}

				yield break;
			}

			public void Tick( World world )
			{
				var hasChronosphere = world.Queries.OwnedBy[world.LocalPlayer]
					.WithTrait<Chronosphere>()
					.Any();

				if (!hasChronosphere)
					Game.controller.CancelInputMode();
					
				// TODO: Check if the selected unit is still alive
			}

			public void Render( World world ) { }

			public string GetCursor(World world, int2 xy, MouseInput mi)
			{
				mi.Button = MouseButton.Left;
				return OrderInner(world, xy, mi).Any()
					? "chrono-select" : "move-blocked";
			}
		}

		class SelectDestination : IOrderGenerator
		{
			Actor self;
			public SelectDestination(Actor self) { this.self = self; }
			
			public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Right)
				{
					Game.controller.CancelInputMode();
					yield break;
				}

				// Cannot chronoshift into unexplored location
				if (world.LocalPlayer.Shroud.IsExplored(xy))
					yield return new Order("ChronosphereActivate", world.LocalPlayer.PlayerActor, self, xy);
			}

			public void Tick(World world)
			{
				var hasChronosphere = world.Queries.OwnedBy[world.LocalPlayer]
					.WithTrait<Chronosphere>()
					.Any();

				if (!hasChronosphere)
					Game.controller.CancelInputMode();

				// TODO: Check if the selected unit is still alive
			}
			
			public void Render(World world)
			{
				world.WorldRenderer.DrawSelectionBox(self, Color.Red, true);
			}

			public string GetCursor(World world, int2 xy, MouseInput mi)
			{
				if (!world.LocalPlayer.Shroud.IsExplored(xy))
					return "move-blocked";
				
				var movement = self.traits.GetOrDefault<IMovement>();
				return (movement.CanEnterCell(xy)) ? "chrono-target" : "move-blocked";
			}
		}
	}
	
	// tag trait to identify the building
	class ChronosphereInfo : TraitInfo<Chronosphere> { }
	public class Chronosphere { }
}
