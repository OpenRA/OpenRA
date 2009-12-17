using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	// infantry prone behavior
	class TakeCover : ITick, INotifyDamageEx, IDamageModifier, ISpeedModifier
	{
		const int defaultProneTime = 100;	/* ticks, =4s */
		const float proneDamage = .5f;
		const float proneSpeed = .5f;

		int remainingProneTime = 0;

		public bool IsProne { get { return remainingProneTime > 0; } }

		public TakeCover(Actor self) {}

		public void Damaged(Actor self, int damage, WarheadInfo warhead)
		{
			remainingProneTime = defaultProneTime;
		}

		public void Tick(Actor self)
		{
			if (IsProne)
				--remainingProneTime;
		}

		public float GetDamageModifier()
		{
			return IsProne ? proneDamage : 1f;
		}

		public float GetSpeedModifier()
		{
			return IsProne ? proneSpeed : 1f;
		}
	}
}
