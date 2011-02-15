#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Traits;
using OpenRA.Mods.RA.Render;

namespace OpenRA.Mods.RA
{
	class TakeCoverInfo : ITraitInfo, ITraitPrerequisite<RenderInfantryInfo>
	{
		public readonly int ProneTime = 100;	/* ticks, =4s */
		public readonly float ProneDamage = .5f;
		public readonly decimal ProneSpeed = .5m;
		public object Create(ActorInitializer init) { return new TakeCover(this); }
	}

	// Infantry prone behavior
	class TakeCover : ITick, INotifyDamage, IDamageModifier, ISpeedModifier, ISync
	{
		TakeCoverInfo Info;
		[Sync]
		int remainingProneTime = 0;
		
		public TakeCover(TakeCoverInfo info)
		{
			Info = info;
		}

		public bool IsProne { get { return remainingProneTime > 0; } }

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage > 0) /* Don't go prone when healed */
			{
				if (e.Warhead == null || !e.Warhead.PreventProne)
				{
					remainingProneTime = Info.ProneTime;
					self.Trait<RenderInfantry>().Prone = true;
				}
			}
		}

		public void Tick(Actor self)
		{
			if (!IsProne)
				return;
			
			//var ri = self.Trait<RenderInfantry>();

			if (--remainingProneTime <= 0)
				self.Trait<RenderInfantry>().Prone = false;
		}
		
		public float GetDamageModifier(Actor attacker, WarheadInfo warhead )
		{
			return IsProne ? Info.ProneDamage : 1f;
		}

		public decimal GetSpeedModifier()
		{
			return IsProne ? Info.ProneSpeed : 1m;
		}
	}
}
