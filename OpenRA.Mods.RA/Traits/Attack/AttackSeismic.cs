#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
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
	[Desc("Requires `ScreenShaker` attached to the world actor.")]
	class AttackSeismicInfo : ITraitInfo, Requires<AttackDetonatesInfo>
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

		public object Create(ActorInitializer init) { return new AttackSeismic(init.Self, this); }
	}

	class AttackSeismic : ITick, IPreventsTeleport
	{
		readonly Actor self;
		readonly AttackSeismicInfo info;
		readonly ScreenShaker screenShaker;
		readonly AttackDetonates attackDetonates;
		bool wasDeployed;
		int tick;

		public AttackSeismic(Actor self, AttackSeismicInfo info)
		{
			this.self = self;
			this.info = info;
			screenShaker = self.World.WorldActor.Trait<ScreenShaker>();

			attackDetonates = self.Trait<AttackDetonates>();
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
			if (!attackDetonates.Deployed)
				return;

			if (!wasDeployed && attackDetonates.Deployed)
			{
				EjectDriver();
				wasDeployed = true;
			}

			if (++tick >= info.ThumpInterval)
			{
				var weapon = self.World.Map.Rules.Weapons[info.ThumpDamageWeapon.ToLowerInvariant()];
				weapon.Impact(Target.FromPos(self.CenterPosition), self, Enumerable.Empty<int>());

				screenShaker.AddEffect(info.ThumpShakeTime, self.CenterPosition, info.ThumpShakeIntensity, info.ThumpShakeMultiplier);
				tick = 0;
			}
		}

		public bool PreventsTeleport(Actor self) { return attackDetonates.Deployed; }
	}
}