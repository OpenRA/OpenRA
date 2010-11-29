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
using OpenRA.Mods.RA.Render;

namespace OpenRA.Mods.RA
{
	class TakeCoverInfo : TraitInfo<TakeCover>, ITraitPrerequisite<RenderInfantryInfo> { }

	// infantry prone behavior
	class TakeCover : ITick, INotifyDamage, IDamageModifier, ISpeedModifier
	{
		const int defaultProneTime = 100;	/* ticks, =4s */
		const float proneDamage = .5f;
		const decimal proneSpeed = .5m;

		[Sync]
		int remainingProneTime = 0;

		public bool IsProne { get { return remainingProneTime > 0; } }

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage > 0) /* Don't go prone when healed */
			{
				if (e.Warhead == null || !e.Warhead.PreventProne)
					remainingProneTime = defaultProneTime;
			}
		}

		public void Tick(Actor self)
		{
			if (!IsProne)
				return;
			
			remainingProneTime--;
			
			var ri = self.Trait<RenderInfantry>();
			if (ri.State == RenderInfantry.AnimationState.Idle)
				if (IsProne)
					ri.anim.PlayFetchIndex("crawl", () => 0);
				else
					ri.anim.Play("stand");
			
			if (ri.anim.CurrentSequence.Name == "run" && IsProne)
				ri.anim.ReplaceAnim("crawl");
			else if (ri.anim.CurrentSequence.Name == "crawl" && !IsProne)
				ri.anim.ReplaceAnim("run");
			
			if (ri.anim.CurrentSequence.Name == "shoot" && IsProne)
				ri.anim.ReplaceAnim("prone-shoot");
			else if (ri.anim.CurrentSequence.Name == "prone-shoot" && !IsProne)
				ri.anim.ReplaceAnim("shoot");
		}
		
		public float GetDamageModifier(Actor attacker, WarheadInfo warhead )
		{
			return IsProne ? proneDamage : 1f;
		}

		public decimal GetSpeedModifier()
		{
			return IsProne ? proneSpeed : 1m;
		}
	}
}
