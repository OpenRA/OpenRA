#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Traits;

namespace OpenRA.GameRules
{
	public interface IWarheadInfo
	{
		void DoImpact(WPos pos, WeaponInfo weapon, Actor firedBy, float modifier);
		float EffectivenessAgainst(ActorInfo ai);
		bool IsValidAgainst(Actor victim, Actor firedBy);
		bool IsValidAgainst(FrozenActor victim, Actor firedBy);
		bool IsValidAgainst(Target target, World world, Actor firedBy);
		void LoadYaml(MiniYaml yaml);

		int DelayTicks { get; }
	}

	public class BaseWarhead : IWarheadInfo
	{
		[Desc("What types of targets are affected.", "Diplomacy keywords: Ally, Neutral, Enemy")]
		public readonly string[] ValidTargets = { "Air", "Ground", "Water", "Ally", "Neutral", "Enemy" };

		[Desc("What types of targets are unaffected.", "Overrules ValidTargets.", "Diplomacy keywords: Ally, Neutral, Enemy")]
		public readonly string[] InvalidTargets = { };

		[Desc("Delay in ticks before applying the warhead effect.","0 = instant (old model).")]
		public readonly int Delay = 0;

		public BaseWarhead() { }

		public void LoadYaml(MiniYaml yaml)
		{
			FieldLoader.Load(this, yaml);
		}

		public void DoImpact(WPos pos, WeaponInfo weapon, Actor firedBy, float firepowerModifier)
		{
			Log.Write("debug", "A Warhead called the base warhead type. This shouldn't happen. Offending actor is {0}", firedBy.Info.Name);
		}

		public float EffectivenessAgainst(ActorInfo ai) { return 1f; }

		public bool IsValidAgainst(Actor victim, Actor firedBy)
		{
			//A target type is valid if it is in the valid targets list, and not in the invalid targets list.
			return CheckTargetList(victim, firedBy, this.ValidTargets) &&
				!CheckTargetList(victim, firedBy, this.InvalidTargets);
		}

		public bool IsValidAgainst(FrozenActor victim, Actor firedBy)
		{
			return IsValidAgainst(victim.Actor, firedBy);
		}

		public bool IsValidAgainst(Target target, World world, Actor firedBy)
		{
			if (target.Type == TargetType.Actor)
				return IsValidAgainst(target.Actor, firedBy);

			if (target.Type == TargetType.FrozenActor)
				return IsValidAgainst(target.FrozenActor, firedBy);

			if (target.Type == TargetType.Terrain)
			{
				var cell = world.Map.CellContaining(target.CenterPosition);
				if (!world.Map.Contains(cell))
					return false;

				var cellInfo = world.Map.GetTerrainInfo(cell);
				if (!this.ValidTargets.Intersect(cellInfo.TargetTypes).Any()
					|| this.InvalidTargets.Intersect(cellInfo.TargetTypes).Any())
					return false;

				return true;
			}

			return false;
		}

		public static bool CheckTargetList(Actor victim, Actor firedBy, string[] targetList)
		{
			if (targetList.Length < 1)
				return false;

			var targetable = victim.Info.Traits.GetOrDefault<ITargetableInfo>();
			if (targetable == null)
				return false;
			if (!targetList.Intersect(targetable.GetTargetTypes()).Any())
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

		public int DelayTicks
		{
			get
			{
				return this.Delay;
			}
		}
   }
}
