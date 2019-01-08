#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits.Render
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

		bool IRenderInfantrySequenceModifier.IsModifyingSequence { get { return isInjured; } }
		string IRenderInfantrySequenceModifier.SequencePrefix { get { return info.InjuredSequencePrefix; } }

		public WithPermanentInjury(ActorInitializer init, WithPermanentInjuryInfo info)
		{
			this.info = info;
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == info.TriggeringDamageStage)
				isInjured = true;
		}
	}
}
