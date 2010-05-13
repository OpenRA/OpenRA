#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using OpenRA.GameRules;

namespace OpenRA.Traits
{
	class TakeCoverInfo : ITraitInfo
	{
		public object Create(Actor self) { return new TakeCover(self); }
	}

	// infantry prone behavior
	class TakeCover : ITick, INotifyDamage, IDamageModifier, ISpeedModifier
	{
		const int defaultProneTime = 100;	/* ticks, =4s */
		const float proneDamage = .5f;
		const float proneSpeed = .5f;

		[Sync]
		int remainingProneTime = 0;

		public bool IsProne { get { return remainingProneTime > 0; } }

		public TakeCover(Actor self) {}

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
