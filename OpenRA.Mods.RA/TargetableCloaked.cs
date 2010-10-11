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
	public class TargetableCloakedInfo : TargetableInfo, ITraitPrerequisite<CloakInfo>
	{
		public readonly string[] CloakedTargetTypes = {};
		public override object Create( ActorInitializer init ) { return new TargetableCloaked(init.self, this); }
	}

	public class TargetableCloaked : Targetable
	{
		Cloak Cloak;
		public TargetableCloaked(Actor self, TargetableCloakedInfo info)
            : base(info)
		{
			Cloak = self.Trait<Cloak>();
		}
		
		public override string[] TargetTypes
		{
			get { return (Cloak.Cloaked) ? ((TargetableCloakedInfo)Info).CloakedTargetTypes
                                         : ((TargetableCloakedInfo)Info).TargetTypes;}
		}
	}
}
