#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Mods.Common.Crates
{
	public class CloakCrateActionInfo : CrateActionInfo
	{
		public override object Create(ActorInitializer init) { return new CloakCrateAction(init.self, this); }
	}

	public class CloakCrateAction : CrateAction
	{
		public CloakCrateAction(Actor self, CloakCrateActionInfo info)
			: base(self, info) { }

		public override int GetSelectionShares(Actor collector)
		{
			var cloak = collector.TraitOrDefault<Cloak>();
			if (cloak == null || !cloak.AcceptsCloakCrate)
				return 0;

			return base.GetSelectionShares(collector);
		}

		public override void Activate(Actor collector)
		{
			collector.Trait<Cloak>().ReceivedCloakCrate(collector);
			base.Activate(collector);
		}
	}
}
