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
	class FirepowerUpgradeCrateActionInfo : CrateActionInfo
	{
		public float Multiplier = 2.0f;
		public override object Create(ActorInitializer init) { return new FirepowerUpgradeCrateAction(init.self, this); }
	}

	class FirepowerUpgradeCrateAction : CrateAction
	{
		public FirepowerUpgradeCrateAction(Actor self, FirepowerUpgradeCrateActionInfo info)
			: base(self, info) {}
		
		public override int GetSelectionShares(Actor collector)
		{
			if (collector.GetPrimaryWeapon() == null && collector.GetSecondaryWeapon() == null)
				return 0;
			
			return base.GetSelectionShares(collector);
		}

		public override void Activate(Actor collector)
		{
			collector.World.AddFrameEndTask(w =>
			{
				var multiplier = (info as FirepowerUpgradeCrateActionInfo).Multiplier;
				collector.traits.Add(new FirepowerUpgrade(multiplier));
			});
			
			base.Activate(collector);
		}
	}

	class FirepowerUpgrade : IFirepowerModifier
	{
		float multiplier;
		public FirepowerUpgrade(float multiplier) { this.multiplier = multiplier; }
		public float GetFirepowerModifier() { return multiplier; }
	}
}
