#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Handle demolitions from C4 explosives.")]
	public class DemolishableInfo : IDemolishableInfo, ITraitInfo
	{
		public bool IsValidTarget(ActorInfo actorInfo, Actor saboteur) { return true; }

		[Desc("If true and this actor has EjectOnDeath, no actor will be spawned.")]
		public readonly bool PreventsEjectOnDeath = false;

		public object Create(ActorInitializer init) { return new Demolishable(init.Self, this); }
	}

	public class Demolishable : IDemolishable, IPreventsEjectOnDeath
	{
		readonly DemolishableInfo info;

		public Demolishable(Actor self, DemolishableInfo info)
		{
			this.info = info;
		}

		public bool PreventsEjectOnDeath(Actor self)
		{
			return info.PreventsEjectOnDeath;
		}

		public void Demolish(Actor self, Actor saboteur)
		{
			self.Kill(saboteur);
		}

		public bool IsValidTarget(Actor self, Actor saboteur)
		{
			return true;
		}
	}
}