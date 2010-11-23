#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Mods.Cnc.Effects;
using OpenRA.Mods.RA;
using OpenRA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc
{
	class IonCannonPowerInfo : SupportPowerInfo
	{
		public override object Create(ActorInitializer init) { return new IonCannonPower(init.self, this); }
	}

	class IonCannonPower : SupportPower, IResolveOrder
	{
		public IonCannonPower(Actor self, IonCannonPowerInfo info) : base(self, info) { }

		public void ResolveOrder(Actor self, Order order)
		{
			if (!IsReady) return;

			if (order.OrderString == "IonCannon")
			{
				Owner.World.AddFrameEndTask(w =>
					{
						Sound.Play(Info.LaunchSound, Game.CellSize * order.TargetLocation.ToFloat2());
						w.Add(new IonCannon(self, w, order.TargetLocation));
					});

				FinishActivate();
			}
		}

		protected override void OnActivate()
		{
			Self.World.OrderGenerator =
				new GenericSelectTargetWithBuilding<IonControl>(Owner.PlayerActor, "IonCannon", "ability");
		}
	}

	class IonControlInfo : TraitInfo<IonControl> { }
	class IonControl { }
}
