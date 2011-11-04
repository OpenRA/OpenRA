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
using OpenRA.Mods.RA.Effects;
using OpenRA.Mods.RA.Move;

namespace OpenRA.Mods.RA
{
	class CrushableInfantryInfo : ITraitInfo, Requires<MobileInfo>
	{
		public readonly string CrushSound = "squish2.aud";
		public readonly string CorpseSequence = "die-crushed";
		public readonly string[] CrushClasses = { "infantry" };
		public readonly int WarnProbability = 75;
		public object Create(ActorInitializer init) { return new CrushableInfantry(init.self, this); }
	}

	class CrushableInfantry : ICrushable
	{
		readonly Actor self;
		readonly CrushableInfantryInfo Info;

		public CrushableInfantry(Actor self, CrushableInfantryInfo info)
		{
			this.self = self;
			this.Info = info;
		}

		public void WarnCrush(Actor crusher)
		{
			if (self.World.SharedRandom.Next(100) <= Info.WarnProbability)
				self.Trait<Mobile>().OnNudge(self, crusher, true);
		}

		public void OnCrush(Actor crusher)
		{
			Sound.Play(Info.CrushSound, crusher.CenterLocation);
			self.World.AddFrameEndTask(w =>
			{
				if (!self.Destroyed)
					w.Add(new Corpse(self, Info.CorpseSequence));
			});

			self.Kill(crusher);
		}

		public bool CrushableBy(string[] crushClasses, Player crushOwner)
		{
			if (crushOwner.Stances[self.Owner] == Stance.Ally)
				return false;

			return Info.CrushClasses.Intersect(crushClasses).Any();
		}
	}
}
