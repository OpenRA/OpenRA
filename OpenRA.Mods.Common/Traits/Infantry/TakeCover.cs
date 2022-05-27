#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Make the unit go prone when under attack, in an attempt to reduce damage.")]
	public class TakeCoverInfo : TurretedInfo
	{
		[Desc("How long (in ticks) the actor remains prone.",
			"Negative values mean actor remains prone permanently.")]
		public readonly int Duration = 100;

		[Desc("Prone movement speed as a percentage of the normal speed.")]
		public readonly int SpeedModifier = 50;

		[Desc("Damage types that trigger prone state. Defined on the warheads.",
			"If Duration is negative (permanent), you can leave this empty to trigger prone state immediately.")]
		public readonly BitSet<DamageType> DamageTriggers = default(BitSet<DamageType>);

		[Desc("Damage modifiers for each damage type (defined on the warheads) while the unit is prone.")]
		public readonly Dictionary<string, int> DamageModifiers = new Dictionary<string, int>();

		[Desc("Muzzle offset modifier to apply while prone.")]
		public readonly WVec ProneOffset = new WVec(500, 0, 0);

		[SequenceReference(prefix: true)]
		[Desc("Sequence prefix to apply while prone.")]
		public readonly string ProneSequencePrefix = "prone-";

		public override object Create(ActorInitializer init) { return new TakeCover(init, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (Duration > -1 && DamageTriggers.IsEmpty)
				throw new YamlException("TakeCover: If Duration isn't negative (permanent), DamageTriggers is required.");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class TakeCover : Turreted, INotifyDamage, IDamageModifier, ISpeedModifier, ISync, IRenderInfantrySequenceModifier
	{
		readonly TakeCoverInfo info;

		[Sync]
		int remainingDuration = 0;

		bool IsProne => !IsTraitDisabled && remainingDuration != 0;

		bool IRenderInfantrySequenceModifier.IsModifyingSequence => IsProne;
		string IRenderInfantrySequenceModifier.SequencePrefix => info.ProneSequencePrefix;

		public TakeCover(ActorInitializer init, TakeCoverInfo info)
			: base(init, info)
		{
			this.info = info;
			if (info.Duration < 0 && info.DamageTriggers.IsEmpty)
				remainingDuration = info.Duration;
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (IsTraitPaused || IsTraitDisabled)
				return;

			if (e.Damage.Value <= 0 || !e.Damage.DamageTypes.Overlaps(info.DamageTriggers))
				return;

			if (!IsProne)
				localOffset = info.ProneOffset;

			remainingDuration = info.Duration;
		}

		protected override void Tick(Actor self)
		{
			base.Tick(self);

			if (!IsTraitPaused && remainingDuration > 0)
				remainingDuration--;

			if (remainingDuration == 0)
				localOffset = WVec.Zero;
		}

		public override bool HasAchievedDesiredFacing => true;

		int IDamageModifier.GetDamageModifier(Actor attacker, Damage damage)
		{
			if (!IsProne)
				return 100;

			if (damage == null || damage.DamageTypes.IsEmpty)
				return 100;

			var modifierPercentages = info.DamageModifiers.Where(x => damage.DamageTypes.Contains(x.Key)).Select(x => x.Value);
			return Util.ApplyPercentageModifiers(100, modifierPercentages);
		}

		int ISpeedModifier.GetSpeedModifier()
		{
			return IsProne ? info.SpeedModifier : 100;
		}

		protected override void TraitDisabled(Actor self)
		{
			remainingDuration = 0;
		}

		protected override void TraitEnabled(Actor self)
		{
			if (info.Duration < 0 && info.DamageTriggers.IsEmpty)
			{
				remainingDuration = info.Duration;
				localOffset = info.ProneOffset;
			}
		}
	}
}
