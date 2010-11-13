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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class IronCurtainPowerInfo : SupportPowerInfo
	{
		public readonly float Duration = 0f;
		public override object Create(ActorInitializer init) { return new IronCurtainPower(init.self, this); }
	}

	class IronCurtainPower : SupportPower, IResolveOrder
	{
		public IronCurtainPower(Actor self, IronCurtainPowerInfo info) : base(self, info) { }

		protected override void OnBeginCharging() { Sound.PlayToPlayer(Owner, "ironchg1.aud"); }
		protected override void OnFinishCharging() { Sound.PlayToPlayer(Owner, "ironrdy1.aud"); }
		protected override void OnActivate()
		{
			Self.World.OrderGenerator = new SelectTarget();
			Sound.Play("slcttgt1.aud");
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (!IsAvailable) return;

			if (order.OrderString == "IronCurtain")
			{
				var curtain = self.World.Queries.WithTrait<IronCurtain>()
					.Where(a => a.Actor.Owner != null)
					.FirstOrDefault().Actor;
				if (curtain != null)
					curtain.Trait<RenderBuilding>().PlayCustomAnim(curtain, "active");

				Sound.Play("ironcur9.aud", order.TargetActor.CenterLocation);
				
				order.TargetActor.Trait<IronCurtainable>().Activate(order.TargetActor,
					(int)((Info as IronCurtainPowerInfo).Duration * 25 * 60));
				
				FinishActivate();
			}
		}

		class SelectTarget : IOrderGenerator
		{
			public SelectTarget() {	}

			public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Right)
					world.CancelInputMode();

				return OrderInner(world, xy, mi);
			}

			IEnumerable<Order> OrderInner(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Left)
				{
					var underCursor = world.FindUnitsAtMouse(mi.Location)
						.Where(a => a.Owner != null
							&& a.HasTrait<IronCurtainable>()
							&& a.HasTrait<Selectable>()).FirstOrDefault();

					if( underCursor != null )
						yield return new Order( "IronCurtain", underCursor.Owner.PlayerActor, underCursor, false );
				}
			}

			public void Tick(World world)
			{
				var hasStructure = world.Queries.OwnedBy[world.LocalPlayer]
					.WithTrait<IronCurtain>()
					.Any();

				if (!hasStructure)
					world.CancelInputMode();
			}

			public void RenderAfterWorld(WorldRenderer wr, World world) { }
			public void RenderBeforeWorld(WorldRenderer wr, World world) { }

			public string GetCursor(World world, int2 xy, MouseInput mi)
			{
				mi.Button = MouseButton.Left;
				return OrderInner(world, xy, mi).Any()
					? "ability" : "move-blocked";
			}
		}
	}

	// tag trait for the building
	class IronCurtainInfo : TraitInfo<IronCurtain> { }
	class IronCurtain { }
}
