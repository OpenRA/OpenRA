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
using System.Linq;
using OpenRA.Mods.Cnc.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc
{
	class IonCannonPowerInfo : SupportPowerInfo
	{
		public override object Create(Actor self) { return new IonCannonPower(self, this); }
	}

	class IonCannonPower : SupportPower, IResolveOrder
	{
		public IonCannonPower(Actor self, IonCannonPowerInfo info) : base(self, info) { }

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
					yield return new Order("IonCannon", world.LocalPlayer.PlayerActor, xy);
			}

			public void Tick(World world)
			{
				var hasStructure = world.Queries.OwnedBy[world.LocalPlayer]
					.WithTrait<IonControl>()
					.Any();

				if (!hasStructure)
					Game.controller.CancelInputMode();
			}

			public void Render(World world) { }

			public string GetCursor(World world, int2 xy, MouseInput mi) { return "ability"; }
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "IonCannon")
			{
				Owner.World.AddFrameEndTask(w =>
					{
						Sound.Play(Info.LaunchSound);
						w.Add(new IonCannon(self, w, order.TargetLocation));
					});

				Game.controller.CancelInputMode();
				FinishActivate();
			}
		}

		protected override void OnBeginCharging() { Sound.PlayToPlayer(Owner, Info.BeginChargeSound); }
		protected override void OnFinishCharging() { Sound.PlayToPlayer(Owner, Info.EndChargeSound); }

		protected override void OnActivate()
		{
			Game.controller.orderGenerator = new SelectTarget();
			Sound.PlayToPlayer(Owner, Info.SelectTargetSound);
		}
	}

	class IonControlInfo : StatelessTraitInfo<IonControl> { }
	class IonControl { }
}
