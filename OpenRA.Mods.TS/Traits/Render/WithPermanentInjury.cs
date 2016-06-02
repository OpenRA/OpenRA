#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.TS.Traits.Render
{
	[Desc("Change the sprite after a certain amount of damage is taken, even when the hitpoints are regenerated.")]
	public class WithPermanentInjuryInfo : ITraitInfo
	{
		public readonly DamageState TriggeringDamageStage = DamageState.Critical;

		public readonly string InjuredSequencePrefix = "crippled-";

		public object Create(ActorInitializer init) { return new WithPermanentInjury(init, this); }
	}

	public class WithPermanentInjury : INotifyDamage, IRenderInfantrySequenceModifier
	{
		readonly WithPermanentInjuryInfo info;

		bool isInjured;

		public bool IsModifyingSequence { get { return isInjured; } }
		public string SequencePrefix { get { return info.InjuredSequencePrefix; } }

		public WithPermanentInjury(ActorInitializer init, WithPermanentInjuryInfo info)
		{
			this.info = info;
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == info.TriggeringDamageStage)
				isInjured = true;
		}
	}
}
