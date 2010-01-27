using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using OpenRa.Orders;

namespace OpenRa.Traits
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
		protected override void OnBeginCharging() { Sound.PlayToPlayer(Owner, "chrochr1.aud"); }
		protected override void OnFinishCharging() { Sound.PlayToPlayer(Owner, "chrordy1.aud"); }
		protected override void OnActivate()
		{
			Game.controller.orderGenerator = new SelectTarget();
			Sound.Play("slcttgt1.aud");
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "ChronosphereSelect" && self.Owner == self.World.LocalPlayer)
			{
				Game.controller.orderGenerator = new SelectDestination(order.TargetActor);
			}
			
			if (order.OrderString == "ChronosphereActivate")
			{
				if (self.Owner == self.World.LocalPlayer)
					Game.controller.CancelInputMode();

				// Cannot chronoshift into unexplored location
				if (!self.Owner.Shroud.IsExplored(order.TargetLocation))
					return;
				
				// Ensure the target cell is valid for the unit
				var movement = order.TargetActor.traits.GetOrDefault<IMovement>();
				if (!movement.CanEnterCell(order.TargetLocation))
					return;

				var chronosphere = self.World.Actors.Where(a => a.Owner == self.Owner
									&& a.traits.Contains<Chronosphere>()).FirstOrDefault();
				
				bool success = order.TargetActor.traits.Get<Chronoshiftable>().Activate(order.TargetActor,
					order.TargetLocation,
					(int)((Info as ChronoshiftPowerInfo).Duration * 25 * 60),
					(Info as ChronoshiftPowerInfo).KillCargo,
					chronosphere);
					
				if (success)
				{
					Sound.Play("chrono2.aud");
					
					// Trigger screen desaturate effect
					foreach (var a in self.World.Actors.Where(a => a.traits.Contains<ChronoshiftPaletteEffect>()))
						a.traits.Get<ChronoshiftPaletteEffect>().DoChronoshift();

					if (chronosphere != null)
						chronosphere.traits.Get<RenderBuilding>().PlayCustomAnim(chronosphere, "active");
				}
				
				Game.controller.CancelInputMode();
				FinishActivate();
			}
		}

		class SelectTarget : IOrderGenerator
		{
			public SelectTarget() { }
			
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
					var loc = mi.Location + Game.viewport.Location;
					var underCursor = world.FindUnits(loc, loc)
						.Where(a => a.Owner != null && a.traits.Contains<Chronoshiftable>()
							&& a.traits.Contains<Selectable>()).FirstOrDefault();

					if (underCursor != null)
						yield return new Order("ChronosphereSelect", world.LocalPlayer.PlayerActor, underCursor);
				}

				yield break;
			}

			public void Tick( World world )
			{
				var hasChronosphere = world.Actors
					.Any(a => a.Owner == world.LocalPlayer && a.traits.Contains<Chronosphere>());

				if (!hasChronosphere)
					Game.controller.CancelInputMode();
					
				// TODO: Check if the selected unit is still alive
			}

			public void Render( World world ) { }

			public Cursor GetCursor(World world, int2 xy, MouseInput mi)
			{
				mi.Button = MouseButton.Left;
				return OrderInner(world, xy, mi).Any()
					? Cursor.ChronoshiftSelect : Cursor.MoveBlocked;
			}
		}

		class SelectDestination : IOrderGenerator
		{
			Actor self;
			public SelectDestination(Actor self)
			{
				this.self = self;
			}
			
			public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Right)
				{
					Game.controller.CancelInputMode();
					yield break;
				}

				yield return new Order("ChronosphereActivate", world.LocalPlayer.PlayerActor, self, xy);
			}

			public void Tick(World world)
			{
				var hasChronosphere = world.Actors
					.Any(a => a.Owner == world.LocalPlayer && a.traits.Contains<Chronosphere>());

				if (!hasChronosphere)
					Game.controller.CancelInputMode();

				// TODO: Check if the selected unit is still alive
			}
			
			public void Render(World world)
			{
				world.WorldRenderer.DrawSelectionBox(self, Color.Red, true);
			}

			public Cursor GetCursor(World world, int2 xy, MouseInput mi)
			{
				if (!world.LocalPlayer.Shroud.IsExplored(xy))
					return Cursor.MoveBlocked;
				
				var movement = self.traits.GetOrDefault<IMovement>();
				return (movement.CanEnterCell(xy)) ? Cursor.Chronoshift : Cursor.MoveBlocked;
			}
		}
	}
	
	// tag trait to identify the building
	class ChronosphereInfo : StatelessTraitInfo<Chronosphere> { }
	public class Chronosphere { }
}
