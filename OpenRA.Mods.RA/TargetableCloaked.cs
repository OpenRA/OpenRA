#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using OpenRA.Graphics;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class TargetableCloakedInfo : ITraitInfo, ITraitPrerequisite<CloakInfo>
	{
		public readonly string[] TargetTypes = {};
		public readonly string[] CloakedTargetTypes = {};
		public object Create( ActorInitializer init ) { return new TargetableCloaked(init.self, this); }
	}

	public class TargetableCloaked : ITargetable
	{
		TargetableCloakedInfo Info;
		Cloak Cloak;
		public TargetableCloaked(Actor self, TargetableCloakedInfo info)
		{
			Info = info;
			Cloak = self.Trait<Cloak>();
		}
		
		public string[] TargetTypes
		{
			get { return (Cloak.Cloaked) ? Info.CloakedTargetTypes : Info.TargetTypes;}
		}
	}
}
