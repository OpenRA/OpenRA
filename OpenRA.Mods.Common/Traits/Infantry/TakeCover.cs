#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Make the unit go prone when under attack, in an attempt to reduce damage.")]
	public class TakeCoverInfo : UpgradableTraitInfo
	{
		[Desc("How long (in ticks) the actor remains prone.")]
		public readonly int ProneTime = 100;

		[Desc("Prone movement speed as a percentage of the normal speed.")]
		public readonly int SpeedModifier = 50;

		[FieldLoader.Require]
		[Desc("Damage types that trigger prone state. Defined on the warheads.")]
		public readonly string[] DamageTriggers = null;

		[FieldLoader.LoadUsing("LoadModifiers")]
		[Desc("Damage modifiers for each damage type (defined on the warheads) while the unit is prone.")]
		public readonly Dictionary<string, int> DamageModifiers = new Dictionary<string, int>();

		[SequenceReference(null, true)] public readonly string ProneSequencePrefix = "prone-";

		public override object Create(ActorInitializer init) { return new TakeCover(init, this); }

		public static object LoadModifiers(MiniYaml yaml)
		{
			var md = yaml.ToDictionary();

			return md.ContainsKey("DamageModifiers")
				? md["DamageModifiers"].ToDictionary(my => FieldLoader.GetValue<int>("(value)", my.Value))
				: new Dictionary<string, int>();
		}
	}

	public class TakeCover : UpgradableTrait<TakeCoverInfo>, ITick, ISync, INotifyDamage, IDamageModifier, ISpeedModifier, IRenderInfantrySequenceModifier
	{
		[Sync] int remainingProneTime = 0;
		bool IsProne { get { return remainingProneTime > 0; } }

		public bool IsModifyingSequence { get { return IsProne; } }
		public string SequencePrefix { get { return Info.ProneSequencePrefix; } }

		public TakeCover(ActorInitializer init, TakeCoverInfo info)
			: base(info) { }

		public void Damaged(Actor self, AttackInfo e)
		{
			var warhead = e.Warhead as DamageWarhead;
			if (e.Damage <= 0 || warhead == null || !warhead.DamageTypes.Any(x => Info.DamageTriggers.Contains(x)))
				return;

			remainingProneTime = Info.ProneTime;
		}

		public void Tick(Actor self)
		{
			if (remainingProneTime > 0)
				--remainingProneTime;
		}

		public int GetDamageModifier(Actor attacker, IWarhead warhead)
		{
			if (!IsProne)
				return 100;

			var damageWh = warhead as DamageWarhead;
			if (damageWh == null)
				return 100;

			var modifierPercentages = Info.DamageModifiers.Where(x => damageWh.DamageTypes.Contains(x.Key)).Select(x => x.Value);
			return Util.ApplyPercentageModifiers(100, modifierPercentages);
		}

		public int GetSpeedModifier()
		{
			return IsProne ? Info.SpeedModifier : 100;
		}
	}
}
