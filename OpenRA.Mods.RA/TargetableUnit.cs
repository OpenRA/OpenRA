#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class TargetableUnitInfo : ITraitInfo
	{
		public readonly string[] TargetTypes = { };

		public virtual object Create(ActorInitializer init) { return new TargetableUnit(init.self, this); }
	}

	public class TargetableUnit : ITargetable
	{
		readonly TargetableUnitInfo info;
		protected Cloak cloak;

		public TargetableUnit(Actor self, TargetableUnitInfo info)
		{
			this.info = info;
			ReceivedCloak(self);
		}

		// Arbitrary units can receive cloak via a crate during gameplay
		public void ReceivedCloak(Actor self)
		{
			cloak = self.TraitOrDefault<Cloak>();
		}

		public virtual bool TargetableBy(Actor self, Actor byActor)
		{
			if (cloak == null || !cloak.Cloaked)
				return true;

			if (self.Owner.IsAlliedWith(byActor.Owner))
				return true;

			return self.World.ActorsWithTrait<DetectCloaked>().Any(a => (self.Location - a.Actor.Location).Length < a.Actor.Info.Traits.Get<DetectCloakedInfo>().Range);
		}

		public virtual string[] TargetTypes { get { return info.TargetTypes; } }

		public virtual IEnumerable<WPos> TargetablePositions(Actor self)
		{
			yield return self.CenterPosition;
		}
	}
}
