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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor is crushable.")]
	class CrushableInfo : ITraitInfo
	{
		[Desc("Sound to play when being crushed.")]
		public readonly string CrushSound = null;
		[Desc("Which crush classes does this actor belong to.")]
		public readonly HashSet<string> CrushClasses = new HashSet<string> { "infantry" };
		[Desc("Probability of mobile actors noticing and evading a crush attempt.")]
		public readonly int WarnProbability = 75;
		[Desc("Will friendly units just crush me instead of pathing around.")]
		public readonly bool CrushedByFriendlies = false;

		public object Create(ActorInitializer init) { return new Crushable(init.Self, this); }
	}

	class Crushable : ICrushable, INotifyCrushed
	{
		readonly Actor self;
		readonly CrushableInfo info;

		public Crushable(Actor self, CrushableInfo info)
		{
			this.self = self;
			this.info = info;
		}

		void INotifyCrushed.WarnCrush(Actor self, Actor crusher, HashSet<string> crushClasses)
		{
			if (!CrushableInner(crushClasses, crusher.Owner))
				return;

			var mobile = self.TraitOrDefault<Mobile>();
			if (mobile != null && self.World.SharedRandom.Next(100) <= info.WarnProbability)
				mobile.Nudge(self, crusher, true);
		}

		void INotifyCrushed.OnCrush(Actor self, Actor crusher, HashSet<string> crushClasses)
		{
			if (!CrushableInner(crushClasses, crusher.Owner))
				return;

			Game.Sound.Play(info.CrushSound, crusher.CenterPosition);

			self.Kill(crusher);
		}

		bool ICrushable.CrushableBy(Actor self, Actor crusher, HashSet<string> crushClasses)
		{
			return CrushableInner(crushClasses, crusher.Owner);
		}

		bool CrushableInner(HashSet<string> crushClasses, Player crushOwner)
		{
			// Only make actor crushable if it is on the ground.
			if (!self.IsAtGroundLevel())
				return false;

			if (!info.CrushedByFriendlies && crushOwner.IsAlliedWith(self.Owner))
				return false;

			return info.CrushClasses.Overlaps(crushClasses);
		}
	}
}
