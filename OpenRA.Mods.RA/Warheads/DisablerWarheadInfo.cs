#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class DisablerWarheadInfo : BaseWarhead, IWarheadInfo
	{
		[Desc("Ranges from the point-of-impact within which actors will be affected by the warhead.","Each range specified must have an associated SpreadFactor defined.","Must be shortest range to longest.")]
		public readonly WRange[] Spread = { new WRange(43) };

		[Desc("What factor to multiply the DisableTicks by for this spread range.", "Each factor specified must have an associated Spread defined.")]
		public readonly float[] SpreadFactor = { 1f };

		[FieldLoader.LoadUsing("LoadVersus")]
		[Desc("Damage vs each disablearmortype. 0% = can't target.")]
		public readonly Dictionary<string, float> Versus;

		[Desc("The raw number of ticks this warhead disabled targets for.")]
		public readonly int DisableTicks = 25;

		[Desc("By what percentage should disable ticks be modified against prone infantry.")]
		public readonly int ProneModifier = 100;

		public DisablerWarheadInfo() : base() { }

		static object LoadVersus(MiniYaml y)
		{
			var nd = y.ToDictionary();
			return nd.ContainsKey("Versus")
				? nd["Versus"].ToDictionary(my => FieldLoader.GetValue<float>("(value)", my.Value))
				: new Dictionary<string, float>();
		}

		public new void DoImpact(WPos pos, WeaponInfo weapon, Actor firedBy, float firepowerModifier)
		{
			var world = firedBy.World;

			for (int i = 0; i < Spread.Length; i++)
			{
				var currentSpread = Spread[i];
				var currentFactor = SpreadFactor[i];
				var previousSpread = WRange.Zero;
				if (i > 0)
					previousSpread = Spread[i - 1];

				var hitActors = world.FindActorsInCircle(pos, currentSpread);
				if (previousSpread.Range > 0)
					hitActors.Except(world.FindActorsInCircle(pos, previousSpread));

				foreach (var victim in hitActors)
				{
					var disablerTrait = victim.TraitOrDefault<DisabledByWarhead>();
					if (disablerTrait == null)
						continue;

					if (IsValidAgainst(victim, firedBy))
					{
						var modifier = victim.TraitsImplementing<IDisableTicksModifier>()
							.Concat(victim.Owner.PlayerActor.TraitsImplementing<IDisableTicksModifier>())
							.Select(t => t.GetDisableTicksModifier(firedBy, this)).Product();

						var ticksLength = (int)((float)DisableTicks * modifier * currentFactor * (float)EffectivenessAgainst(victim.Info));
						disablerTrait.SufferDisableImpact(victim, firedBy, ticksLength);
					}
				}
			}
		}

		public new float EffectivenessAgainst(ActorInfo ai)
		{
			var disabledByTrait = ai.Traits.GetOrDefault<DisabledByWarheadInfo>();
			if (disabledByTrait == null)
				return 0f;

			var armor = ai.Traits.GetOrDefault<DisableArmorInfo>();
			if (armor == null || armor.Type == null)
				return 1;

			float versus;
			return Versus.TryGetValue(armor.Type, out versus) ? versus : 1;
		}

		public new bool IsValidAgainst(Actor victim, Actor firedBy)
		{
			var disablerTrait = victim.TraitOrDefault<DisabledByWarhead>();
			if (disablerTrait == null)
				return false;

			//A target type is valid if it is in the valid targets list, and not in the invalid targets list.
			return CheckTargetList(victim, firedBy, this.ValidTargets) &&
				!CheckTargetList(victim, firedBy, this.InvalidTargets);
		}

		public new static bool CheckTargetList(Actor victim, Actor firedBy, string[] targetList)
		{
			if (targetList.Length < 1)
				return false;

			var targetable = victim.Info.Traits.GetOrDefault<DisabledByWarheadInfo>();
			if (targetable == null)
				return false;
			if (!targetList.Intersect(targetable.TargetTypes).Any())
				return false;

			var stance = firedBy.Owner.Stances[victim.Owner];
			if (targetList.Contains("Ally") && (stance == Stance.Ally))
				return true;
			if (targetList.Contains("Neutral") && (stance == Stance.Neutral))
				return true;
			if (targetList.Contains("Enemy") && (stance == Stance.Enemy))
				return true;
			return false;
		}
	}
}
