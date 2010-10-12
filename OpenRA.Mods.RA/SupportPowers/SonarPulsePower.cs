#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class SonarPulsePowerInfo : SupportPowerInfo
	{
		public override object Create(ActorInitializer init) { return new SonarPulsePower(init.self, this); }
	}

	public class SonarPulsePower : SupportPower, IResolveOrder
	{
		public SonarPulsePower(Actor self, SonarPulsePowerInfo info) : base(self, info) { }

		protected override void OnBeginCharging() { }
		protected override void OnFinishCharging() { Sound.PlayToPlayer(Owner, "pulse1.aud"); }

		protected override void OnActivate()
		{
			Self.World.IssueOrder(new Order("SonarPulse", Owner.PlayerActor));
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (!IsAvailable) return;

			if (order.OrderString == "SonarPulse")
			{
				// TODO: Reveal submarines

				// Should this play for all players?
				Sound.Play("sonpulse.aud");
				FinishActivate();
			}
		}
	}
}
