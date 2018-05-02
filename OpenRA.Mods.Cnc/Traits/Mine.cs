#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	class MineInfo : ITraitInfo
	{
		public readonly HashSet<string> CrushClasses = new HashSet<string>();
		public readonly bool AvoidFriendly = true;
		public readonly bool BlockFriendly = true;
		public readonly HashSet<string> DetonateClasses = new HashSet<string>();

		public object Create(ActorInitializer init) { return new Mine(this); }
	}

	class Mine : ICrushable, INotifyCrushed
	{
		readonly MineInfo info;

		public Mine(MineInfo info)
		{
			this.info = info;
		}

		void INotifyCrushed.WarnCrush(Actor self, Actor crusher, HashSet<string> crushClasses) { }

		void INotifyCrushed.OnCrush(Actor self, Actor crusher, HashSet<string> crushClasses)
		{
			if (!info.CrushClasses.Overlaps(crushClasses))
				return;

			if (crusher.Info.HasTraitInfo<MineImmuneInfo>() || (self.Owner.Stances[crusher.Owner] == Stance.Ally && info.AvoidFriendly))
				return;

			var mobile = crusher.TraitOrDefault<Mobile>();
			if (mobile != null && !info.DetonateClasses.Overlaps(mobile.Info.LocomotorInfo.Crushes))
				return;

			self.Kill(crusher, mobile != null ? mobile.Info.LocomotorInfo.CrushDamageTypes : new HashSet<string>());
		}

		bool ICrushable.CrushableBy(Actor self, Actor crusher, HashSet<string> crushClasses)
		{
			if (info.BlockFriendly && !crusher.Info.HasTraitInfo<MineImmuneInfo>() && self.Owner.Stances[crusher.Owner] == Stance.Ally)
				return false;

			return info.CrushClasses.Overlaps(crushClasses);
		}
	}

	[Desc("Tag trait for stuff that should not trigger mines.")]
	class MineImmuneInfo : TraitInfo<MineImmune> { }
	class MineImmune { }
}
