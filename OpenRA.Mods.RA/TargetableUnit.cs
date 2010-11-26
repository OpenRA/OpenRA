#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class TargetableUnitInfo : ITraitInfo
	{
		public readonly string[] TargetTypes = { };
		public virtual object Create( ActorInitializer init ) { return new TargetableUnit<TargetableUnitInfo>( init.self, this ); }
	}

	public class TargetableUnit<Info> : ITargetable
		where Info : TargetableUnitInfo
	{
		protected readonly Info info;
		protected Cloak Cloak;
		
		public TargetableUnit( Actor self, Info info )
		{
			this.info = info;
			RecievedCloak(self);
		}

		// Arbitrary units can recieve cloak via a crate during gameplay
		public void RecievedCloak(Actor self)
		{
			Cloak = self.TraitOrDefault<Cloak>();
		}
		
		public virtual bool TargetableBy(Actor self, Actor byActor)
		{
			if (Cloak == null)
				return true;
			
			if (!Cloak.Cloaked || self.Owner == byActor.Owner || self.Owner.Stances[byActor.Owner] == Stance.Ally)
				return true;
			
			return self.World.Queries.WithTrait<DetectCloaked>().Any(a => (self.Location - a.Actor.Location).Length < a.Actor.Info.Traits.Get<DetectCloakedInfo>().Range);
		}

		public virtual string[] TargetTypes { get { return info.TargetTypes; } }

		public virtual IEnumerable<int2> TargetableCells( Actor self )
		{
			yield return Util.CellContaining( self.CenterLocation );
		}
	}
}
