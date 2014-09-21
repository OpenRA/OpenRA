#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("This actor is crushable.")]
	class CrushableInfo : ITraitInfo
	{
		[Desc("Sound to play when being crushed.")]
		public readonly string CrushSound = null;
		[Desc("Which crush classes does this actor belong to.")]
		public readonly string[] CrushClasses = { "infantry" };
		[Desc("Probability of mobile actors noticing and evading a crush attempt.")]
		public readonly int WarnProbability = 75;
		[Desc("Will friendly units just crush me instead of pathing around.")]
		public readonly bool CrushedByFriendlies = false;

		public object Create(ActorInitializer init) { return new Crushable(init.self, this); }
	}

	class Crushable : ICrushable
	{
		readonly Actor self;
		readonly CrushableInfo info;

		public Crushable(Actor self, CrushableInfo info)
		{
			this.self = self;
			this.info = info;
		}

		public void WarnCrush(Actor crusher)
		{
			var mobile = self.TraitOrDefault<Mobile>();
			if (mobile != null && self.World.SharedRandom.Next(100) <= info.WarnProbability)
				mobile.Nudge(self, crusher, true);
		}

		public void OnCrush(Actor crusher)
		{
			Sound.Play(info.CrushSound, crusher.CenterPosition);
			var wda = self.TraitOrDefault<WithDeathAnimation>();
			if (wda != null)
			{
				var palette = wda.Info.CrushedSequencePalette;
				if (wda.Info.CrushedPaletteIsPlayerPalette)
					palette += self.Owner.InternalName;

				wda.SpawnDeathAnimation(self, wda.Info.CrushedSequence, palette);
			}
			self.Kill(crusher);
		}

		public bool CrushableBy(string[] crushClasses, Player crushOwner)
		{
			if (!info.CrushedByFriendlies && crushOwner.IsAlliedWith(self.Owner))
				return false;

			return info.CrushClasses.Intersect(crushClasses).Any();
		}
	}
}
