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
	class SpeedUpgradeCrateActionInfo : CrateActionInfo
	{
		public float Multiplier = 1.7f;
		public override object Create(ActorInitializer init) { return new SpeedUpgradeCrateAction(init.self, this); }
	}

	class SpeedUpgradeCrateAction : CrateAction
	{
		public SpeedUpgradeCrateAction(Actor self, SpeedUpgradeCrateActionInfo info)
			: base(self, info) {}
				
		public override void Activate(Actor collector)
		{
			collector.World.AddFrameEndTask(w => 
			{
				var multiplier = (info as SpeedUpgradeCrateActionInfo).Multiplier;
				collector.traits.Add(new SpeedUpgrade(multiplier));
			});
			base.Activate(collector);
		}
	}
	
	class SpeedUpgrade : ISpeedModifier
	{
		float multiplier;
		public SpeedUpgrade(float multiplier) {	this.multiplier = multiplier; }
		public float GetSpeedModifier()	{ return multiplier; }
	}
}
