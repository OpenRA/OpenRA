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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class DeathSoundsInfo : ITraitInfo
	{
		public readonly string DeathVoice = "Die";
		public readonly string Burned = null;
		public readonly string Zapped = null;

		public object Create(ActorInitializer init) { return new DeathSounds(this); }
	}

	public class DeathSounds : INotifyKilled
	{
		DeathSoundsInfo info;

		public DeathSounds(DeathSoundsInfo info) { this.info = info; }

		public void Killed(Actor self, AttackInfo e)
		{
			// Killed by some non-standard means
			if (e.Warhead == null)
				return;

			var cp = self.CenterPosition;

			// Killed by fire
			if (info.Burned != null && e.Warhead.InfDeath == 5)
				Sound.Play(info.Burned, cp);

			// Killed by Tesla/Laser zap
			if (info.Zapped != null && e.Warhead.InfDeath == 6)
				Sound.Play(info.Zapped, cp);

			if ((e.Warhead.InfDeath < 5) || (info.Burned == null && info.Zapped == null))
				Sound.PlayVoiceLocal(info.DeathVoice, self, self.Owner.Country.Race, cp);
		}
	}
}