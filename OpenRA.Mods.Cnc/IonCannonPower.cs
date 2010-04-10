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
using OpenRA.Orders;

namespace OpenRA.Mods.Cnc
{
	class IonCannonPowerInfo : SupportPowerInfo
	{
		public override object Create(Actor self) { return new IonCannonPower(self, this); }
	}

	class IonCannonPower : SupportPower, IResolveOrder
	{
		public IonCannonPower(Actor self, IonCannonPowerInfo info) : base(self, info) { }

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "IonCannon")
			{
				Owner.World.AddFrameEndTask(w =>
					{
						Sound.Play(Info.LaunchSound);
						w.Add(new IonCannon(self, w, order.TargetLocation));
					});

				if (Owner == Owner.World.LocalPlayer)
					Game.controller.CancelInputMode();

				FinishActivate();
			}
		}

		protected override void OnBeginCharging() { Sound.PlayToPlayer(Owner, Info.BeginChargeSound); }
		protected override void OnFinishCharging() { Sound.PlayToPlayer(Owner, Info.EndChargeSound); }

		protected override void OnActivate()
		{
			Game.controller.orderGenerator =
				new GenericSelectTargetWithBuilding<IonControl>(Owner.PlayerActor, "IonCannon", "ability");
			Sound.PlayToPlayer(Owner, Info.SelectTargetSound);
		}
	}

	class IonControlInfo : StatelessTraitInfo<IonControl> { }
	class IonControl { }
}
