#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	class MineInfo : ITraitInfo
	{
		public readonly HashSet<string> CrushClasses = new HashSet<string>();
		public readonly bool AvoidFriendly = true;
		public readonly HashSet<string> DetonateClasses = new HashSet<string>();

		public object Create(ActorInitializer init) { return new Mine(init, this); }
	}

	class Mine : ICrushable
	{
		readonly Actor self;
		readonly MineInfo info;

		public Mine(ActorInitializer init, MineInfo info)
		{
			self = init.Self;
			this.info = info;
		}

		public void WarnCrush(Actor crusher) { }

		public void OnCrush(Actor crusher)
		{
			if (crusher.Info.HasTraitInfo<MineImmuneInfo>() || (self.Owner.Stances[crusher.Owner] == Stance.Ally && info.AvoidFriendly))
				return;

			var mobile = crusher.TraitOrDefault<Mobile>();
			if (mobile != null && !info.DetonateClasses.Overlaps(mobile.Info.Crushes))
				return;

			self.Kill(crusher);
		}

		public bool CrushableBy(HashSet<string> crushClasses, Player owner)
		{
			return info.CrushClasses.Overlaps(crushClasses);
		}
	}

	[Desc("Tag trait for stuff that should not trigger mines.")]
	class MineImmuneInfo : TraitInfo<MineImmune> { }
	class MineImmune { }
}
