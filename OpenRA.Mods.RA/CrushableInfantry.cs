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
	class CrushableInfantryInfo : ITraitInfo, Requires<MobileInfo>, Requires<RenderInfantryInfo>
	{
		public readonly string CrushSound = null;
		public readonly string CorpseSequence = "die-crushed";
		public readonly string[] CrushClasses = { "infantry" };
		public readonly int WarnProbability = 75;
		public object Create(ActorInitializer init) { return new CrushableInfantry(init.self, this); }
	}

	class CrushableInfantry : ICrushable
	{
		readonly Actor self;
		readonly CrushableInfantryInfo Info;
		readonly RenderInfantry ri;

		public CrushableInfantry(Actor self, CrushableInfantryInfo info)
		{
			this.self = self;
			this.Info = info;
			ri = self.Trait<RenderInfantry>();
		}

		public void WarnCrush(Actor crusher)
		{
			if (self.World.SharedRandom.Next(100) <= Info.WarnProbability)
				self.Trait<Mobile>().Nudge(self, crusher, true);
		}

		public void OnCrush(Actor crusher)
		{
			Sound.Play(Info.CrushSound, crusher.CenterPosition);
			ri.SpawnCorpse(self, Info.CorpseSequence);
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
