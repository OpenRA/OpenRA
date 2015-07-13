#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	public class TargetableSubmarineInfo : TargetableUnitInfo, Requires<CloakInfo>
	{
		public readonly string[] CloakedTargetTypes = { };

		public override object Create(ActorInitializer init) { return new TargetableSubmarine(init.Self, this); }
	}

	public class TargetableSubmarine : TargetableUnit
	{
		readonly TargetableSubmarineInfo info;

		public TargetableSubmarine(Actor self, TargetableSubmarineInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override string[] TargetTypes
		{
			get
			{
				return IsTraitDisabled ? None
					: (cloak.Cloaked ? info.CloakedTargetTypes : info.TargetTypes);
			}
		}
	}
}
