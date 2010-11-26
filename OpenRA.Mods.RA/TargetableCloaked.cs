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

namespace OpenRA.Mods.RA
{
	public class TargetableCloakedInfo : TargetableUnitInfo, ITraitPrerequisite<CloakInfo>
	{
		public readonly string[] CloakedTargetTypes = {};
		public override object Create( ActorInitializer init ) { return new TargetableCloaked(init.self, this); }
	}

	public class TargetableCloaked : TargetableUnit<TargetableCloakedInfo>
	{
		Cloak Cloak;
		public TargetableCloaked(Actor self, TargetableCloakedInfo info)
            : base(info)
		{
			Cloak = self.Trait<Cloak>();
		}
		
		public override string[] TargetTypes
		{
			get { return (Cloak.Cloaked) ? info.CloakedTargetTypes
                                         : info.TargetTypes;}
		}
		
		// Todo: Finish me
		public bool TargetableBy(Actor self, Actor byActor)
		{
			if (!Cloak.Cloaked || self.Owner == byActor.Owner || self.Owner.Stances[byActor.Owner] == Stance.Ally)
				return true;
			
			return self.World.Queries.WithTrait<DetectCloaked>().Any(a => (self.Location - a.Actor.Location).Length < a.Actor.Info.Traits.Get<DetectCloakedInfo>().Range);
		}
	}
}
