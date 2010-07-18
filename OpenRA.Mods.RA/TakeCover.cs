#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class TakeCoverInfo : TraitInfo<TakeCover> { }

	// infantry prone behavior
	class TakeCover : ITick, INotifyDamage, IDamageModifier, ISpeedModifier
	{
		const int defaultProneTime = 100;	/* ticks, =4s */
		const float proneDamage = .5f;
		const float proneSpeed = .5f;

		[Sync]
		int remainingProneTime = 0;

		public bool IsProne { get { return remainingProneTime > 0; } }

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage > 0)		/* fix to allow healing via `damage` */
				remainingProneTime = defaultProneTime;
		}

		public void Tick(Actor self)
		{
			if (IsProne)
				--remainingProneTime;
		}

		public float GetDamageModifier( WarheadInfo warhead )
		{
			return IsProne ? proneDamage : 1f;
		}

		public float GetSpeedModifier()
		{
			return IsProne ? proneSpeed : 1f;
		}
	}
}
