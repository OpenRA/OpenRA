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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Make the unit go prone when under attack, in an attempt to reduce damage.")]
	public class TakeCoverInfo : TurretedInfo
	{
		[Desc("How long (in ticks) the actor remains prone.")]
		public readonly int ProneTime = 100;

		[Desc("Prone movement speed as a percentage of the normal speed.")]
		public readonly int SpeedModifier = 50;

		[FieldLoader.Require]
		[Desc("Damage types that trigger prone state. Defined on the warheads.")]
		public readonly HashSet<string> DamageTriggers = new HashSet<string>();

		[Desc("Damage modifiers for each damage type (defined on the warheads) while the unit is prone.")]
		public readonly Dictionary<string, int> DamageModifiers = new Dictionary<string, int>();

		public readonly WVec ProneOffset = new WVec(85, 0, -171);

		[SequenceReference(null, true)] public readonly string ProneSequencePrefix = "prone-";

		public override object Create(ActorInitializer init) { return new TakeCover(init, this); }
	}

	public class TakeCover : Turreted, INotifyDamage, IDamageModifier, ISpeedModifier, ISync, IRenderInfantrySequenceModifier
	{
		readonly TakeCoverInfo info;
		[Sync] int remainingProneTime = 0;
		bool IsProne { get { return remainingProneTime > 0; } }

		public bool IsModifyingSequence { get { return IsProne; } }
		public string SequencePrefix { get { return info.ProneSequencePrefix; } }

		public TakeCover(ActorInitializer init, TakeCoverInfo info)
			: base(init, info)
		{
			this.info = info;
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage.Value <= 0 || !e.Damage.DamageTypes.Overlaps(info.DamageTriggers))
				return;

			if (!IsProne)
				localOffset = info.ProneOffset;

			remainingProneTime = info.ProneTime;
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);

			if (IsProne && --remainingProneTime == 0)
				localOffset = WVec.Zero;
		}

		public override bool HasAchievedDesiredFacing
		{
			get { return true; }
		}

		public int GetDamageModifier(Actor attacker, Damage damage)
		{
			if (!IsProne)
				return 100;

			if (damage.DamageTypes.Count == 0)
				return 100;

			var modifierPercentages = info.DamageModifiers.Where(x => damage.DamageTypes.Contains(x.Key)).Select(x => x.Value);
			return Util.ApplyPercentageModifiers(100, modifierPercentages);
		}

		public int GetSpeedModifier()
		{
			return IsProne ? info.SpeedModifier : 100;
		}
	}
}
