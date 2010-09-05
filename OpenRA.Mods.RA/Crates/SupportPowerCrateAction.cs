#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Crates
{
	class SupportPowerCrateActionInfo : CrateActionInfo
	{
		public readonly string Power = null;
		public override object Create(ActorInitializer init) { return new SupportPowerCrateAction(init.self, this); }
	}

	class SupportPowerCrateAction : CrateAction
	{
		public SupportPowerCrateAction(Actor self, SupportPowerCrateActionInfo info)
			: base(self, info) { }

		public override void Activate(Actor collector)
		{
			// shit and broken. if you have a single-use and a multi-use version of the same
			// support power, this only works by order-coincidence. that's stupid.

			var p = collector.Owner.PlayerActor.TraitsImplementing<SupportPower>()
				.FirstOrDefault(sp => sp.GetType().Name == (info as SupportPowerCrateActionInfo).Power);

			if (p != null) p.Give(1);

			base.Activate(collector);
		}
	}
}
