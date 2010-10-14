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

namespace OpenRA.Mods.RA
{
	class ChronoshiftPowerInfo : SupportPowerInfo
	{
		public override object Create(ActorInitializer init) { return new ChronoshiftPower(init.self,this); }
	}

	class ChronoshiftPower : SupportPower, IResolveOrder
	{	
		public ChronoshiftPower(Actor self, ChronoshiftPowerInfo info) : base(self, info) { }
		protected override void OnActivate() { Self.World.OrderGenerator = new SelectTarget(); }

		public void ResolveOrder(Actor self, Order order)
		{
			if (!IsAvailable) return;

			if (order.OrderString == "ChronosphereSelect" && self.Owner == self.World.LocalPlayer)
			{
				//self.World.OrderGenerator = new SelectDestination(order.TargetActor);
			}
			
			if (order.OrderString == "ChronosphereActivate")
			{
				// Ensure the target cell is valid for the unit
				var movement = order.TargetActor.TraitOrDefault<IMove>();
				if (!movement.CanEnterCell(order.TargetLocation))
					return;

				var chronosphere = self.World.Queries
					.OwnedBy[self.Owner]
					.WithTrait<Chronosphere>()
					.Select(x => x.Actor).FirstOrDefault();
				
				chronosphere.Trait<Chronosphere>().Teleport(order.TargetActor, order.TargetLocation);

				FinishActivate();
			}
		}

		class SelectTarget : IOrderGenerator
		{
			public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Right)
					world.CancelInputMode();

				var ret = OrderInner( world, xy, mi ).ToList();
				foreach( var order in ret )
				{
					world.OrderGenerator = new SelectDestination(order.TargetActor);
					break;
				}
				return ret;
			}

			IEnumerable<Order> OrderInner(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Left)
				{
					var underCursor = world.FindUnitsAtMouse(mi.Location)
						.Where(a => a.Owner != null && a.HasTrait<Chronoshiftable>()
							&& a.HasTrait<Selectable>()).FirstOrDefault();

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
					world.CancelInputMode();
					
				// TODO: Check if the selected unit is still alive
			}

			public void RenderAfterWorld( WorldRenderer wr, World world ) { }
			public void RenderBeforeWorld( WorldRenderer wr, World world ) { }

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
					world.CancelInputMode();

				var ret = OrderInner( world, xy, mi ).ToList();
				if (ret.Count > 0)
					world.CancelInputMode();
				return ret;
			}

			IEnumerable<Order> OrderInner(World world, int2 xy, MouseInput mi)
			{
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
					world.CancelInputMode();

				// TODO: Check if the selected unit is still alive
			}
			
			public void RenderAfterWorld(WorldRenderer wr, World world)
			{
				wr.DrawSelectionBox(self, Color.Red);
			}

			public void RenderBeforeWorld(WorldRenderer wr, World world) { }

			public string GetCursor(World world, int2 xy, MouseInput mi)
			{
				if (!world.LocalPlayer.Shroud.IsExplored(xy))
					return "move-blocked";
				
				var movement = self.TraitOrDefault<IMove>();
				return (movement.CanEnterCell(xy)) ? "chrono-target" : "move-blocked";
			}
		}
	}
	
	// tag trait for the building
	class ChronosphereInfo : ITraitInfo
	{
		public readonly int Duration = 30;
		public readonly bool KillCargo = true;
		public object Create(ActorInitializer init) { return new Chronosphere(init.self); }
	}
	
	class Chronosphere
	{
		Actor self;
		public Chronosphere(Actor self)
		{
			this.self = self;
		}
		
		public void Teleport(Actor targetActor, int2 targetLocation)
		{
			var info = self.Info.Traits.Get<ChronosphereInfo>();
			bool success = targetActor.Trait<Chronoshiftable>().Activate(targetActor, targetLocation, info.Duration * 25, info.KillCargo, self);
			
			if (success)
			{
				Sound.Play("chrono2.aud", self.CenterLocation);
				Sound.Play("chrono2.aud", targetActor.CenterLocation);
				
				// Trigger screen desaturate effect
				foreach (var a in self.World.Queries.WithTrait<ChronoshiftPaletteEffect>())
					a.Trait.Enable();

				self.Trait<RenderBuilding>().PlayCustomAnim(self, "active");
			}
		}
	}
}
