#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ArmorUpgradeCrateActionInfo : CrateActionInfo
	{
		public float Multiplier = 2.0f;
		public override object Create(ActorInitializer init) { return new ArmorUpgradeCrateAction(init.self, this); }
	}

	class ArmorUpgradeCrateAction : CrateAction
	{
		public ArmorUpgradeCrateAction(Actor self, ArmorUpgradeCrateActionInfo info)
			: base(self, info) {}

		public override void Activate(Actor collector)
		{
			collector.World.AddFrameEndTask(w =>
			{
				var multiplier = (info as ArmorUpgradeCrateActionInfo).Multiplier;
				collector.traits.Add(new ArmorUpgrade(multiplier));
			});
			
			base.Activate(collector);
		}
	}

	class ArmorUpgrade : IDamageModifier
	{
		float multiplier;
		public ArmorUpgrade(float multiplier) { this.multiplier = 1/multiplier; }
		public float GetDamageModifier( WarheadInfo warhead ) { return multiplier; }
	}
}
