#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Effects;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class NukePowerInfo : SupportPowerInfo
	{
		[WeaponReference]
		public readonly string MissileWeapon = "";
		public readonly int2 SpawnOffset = int2.Zero;

		public override object Create(ActorInitializer init) { return new NukePower(init.self, this); }
	}

	class NukePower : SupportPower
	{
		public NukePower(Actor self, NukePowerInfo info) : base(self, info) { }
		public override IOrderGenerator OrderGenerator(string order, SupportPowerManager manager)
		{
			Sound.PlayToPlayer(manager.self.Owner, Info.SelectTargetSound);
			return new SelectGenericPowerTarget(order, manager, "nuke", MouseButton.Left);
		}

		public override void Activate(Actor self, Order order)
		{
			// Play to everyone but the current player
			if (self.Owner != self.World.LocalPlayer)
				Sound.Play(Info.LaunchSound);

			var npi = Info as NukePowerInfo;

			self.Trait<RenderBuilding>().PlayCustomAnim(self, "active");
			self.World.AddFrameEndTask(w => w.Add(
				new NukeLaunch(self.Owner, self, npi.MissileWeapon, (PVecInt)npi.SpawnOffset, order.TargetLocation)));
		}
	}
}
