#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	class MadTankInfo : ITraitInfo, Requires<DetonatesInfo>
	{
		public readonly int ThumpInterval = 8;
		[WeaponReference]
		public readonly string ThumpDamageWeapon = "MADTankThump";
		public readonly int ThumpShakeIntensity = 3;
		public readonly float2 ThumpShakeMultiplier = new float2(1, 0);
		public readonly int ThumpShakeTime = 10;

		[ActorReference]
		public readonly string DriverActor = "e1";

		[VoiceReference] public readonly string Voice = "Action";

		public object Create(ActorInitializer init) { return new MadTank(init.Self, this); }
	}

	class MadTank : ITick, IPreventsTeleport
	{
		readonly Actor self;
		readonly MadTankInfo info;
		readonly ScreenShaker screenShaker;
		readonly Detonates det;
		bool wasDeployed;
		int tick;

		public MadTank(Actor self, MadTankInfo info)
		{
			this.self = self;
			this.info = info;
			screenShaker = self.World.WorldActor.Trait<ScreenShaker>();

			det = self.Trait<Detonates>();
		}

		void EjectDriver()
		{
			var driver = self.World.CreateActor(info.DriverActor.ToLowerInvariant(), new TypeDictionary
			{
				new LocationInit(self.Location),
				new OwnerInit(self.Owner)
			});
			var driverMobile = driver.TraitOrDefault<Mobile>();
			if (driverMobile != null)
				driverMobile.Nudge(driver, driver, true);
		}

		public void Tick(Actor self)
		{
			if (!det.Deployed)
				return;

			if (!wasDeployed && det.Deployed)
			{
				EjectDriver();
				wasDeployed = true;
			}

			if (++tick >= info.ThumpInterval)
			{
				if (info.ThumpDamageWeapon != null)
				{
					var weapon = self.World.Map.Rules.Weapons[info.ThumpDamageWeapon.ToLowerInvariant()];

					// Use .FromPos since this weapon needs to affect more than just the MadTank actor
					weapon.Impact(Target.FromPos(self.CenterPosition), self, Enumerable.Empty<int>());
				}

				screenShaker.AddEffect(info.ThumpShakeTime, self.CenterPosition, info.ThumpShakeIntensity, info.ThumpShakeMultiplier);
				tick = 0;
			}
		}

		public bool PreventsTeleport(Actor self) { return det.Deployed; }
	}
}
