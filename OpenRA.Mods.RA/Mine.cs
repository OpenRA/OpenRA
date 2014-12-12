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
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class MineInfo : ITraitInfo, IOccupySpaceInfo
	{
		public readonly string[] CrushClasses = { };
		public readonly string[] DetonateClasses = { };
		public readonly Stance TriggerPlayers = Stance.Enemy | Stance.Neutral;

		public object Create(ActorInitializer init) { return new Mine(init, this); }
	}

	class Mine : ICrushable
	{
		readonly Actor self;
		readonly MineInfo info;

		public Mine(ActorInitializer init, MineInfo info)
		{
			this.self = init.self;
			this.info = info;
		}

		public void WarnCrush(Actor crusher) {}

		public void OnCrush(Actor crusher)
		{
			if (crusher.HasTrait<MineImmune>() || (self.Owner.Stances[crusher.Owner].Intersects(info.TriggerPlayers)))
				return;

			var mobile = crusher.TraitOrDefault<Mobile>();
			if (mobile != null && !info.DetonateClasses.Intersect(mobile.Info.Crushes).Any())
				return;

			self.Kill(crusher);
		}

		public bool CrushableBy(string[] crushClasses, Player owner)
		{
			return info.CrushClasses.Intersect(crushClasses).Any();
		}
	}

	[Desc("Tag trait for stuff that should not trigger mines.")]
	class MineImmuneInfo : TraitInfo<MineImmune> { }
	class MineImmune { }
}
