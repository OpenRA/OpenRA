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
	public class ProjectileArgs
	{
		public WeaponInfo Weapon;
		public float FirepowerModifier = 1.0f;
		public int Facing;
		public WPos Source;
		public Actor SourceActor;
		public WPos PassiveTarget;
		public Target GuidedTarget;
	}

	public interface IProjectileInfo { IEffect Create(ProjectileArgs args); }

	public class WeaponInfo
	{
		[Desc("The maximum range the weapon can fire.")]
		public readonly WRange Range = WRange.Zero;

		[Desc("The sound played when the weapon is fired.")]
		public readonly string[] Report = null;

		[Desc("Rate of Fire = Delay in ticks between reloading ammo clips.")]
		public readonly int ROF = 1;

		[Desc("Number of shots in a single ammo clip.")]
		public readonly int Burst = 1;

		public readonly bool Charges = false;

		public readonly string Palette = "effect";

		[Desc("What types of targets are affected.")]
		public readonly string[] ValidTargets = { "Ground", "Water", "Ally", "Neutral", "Enemy" };

		[Desc("What types of targets are unaffected.", "Overrules ValidTargets.")]
		public readonly string[] InvalidTargets = { };

		[Desc("Delay in ticks between firing shots from the same ammo clip.")]
		public readonly int BurstDelay = 5;

		[Desc("The minimum range the weapon can fire.")]
		public readonly WRange MinRange = WRange.Zero;

		[FieldLoader.LoadUsing("LoadProjectile")] public readonly IProjectileInfo Projectile;
		[FieldLoader.LoadUsing("LoadWarheads")]	public readonly List<IWarheadInfo> Warheads = new List<IWarheadInfo>();

		public WeaponInfo(string name, MiniYaml content)
		{
			FieldLoader.Load(this, content);
		}

		static object LoadProjectile(MiniYaml yaml)
		{
			MiniYaml proj;
			if (!yaml.ToDictionary().TryGetValue("Projectile", out proj))
				return null;
			var ret = Game.CreateObject<IProjectileInfo>(proj.Value + "Info");
			FieldLoader.Load(ret, proj);
			return ret;
		}

		static object LoadWarheads(MiniYaml yaml)
		{
			List<IWarheadInfo> retList = new List<IWarheadInfo>();
			foreach (var node in yaml.Nodes)
			{
				if (node.Key.Split('@')[0] == "Warhead")
				{
					var ret = Game.CreateObject<IWarheadInfo>(node.Value.Value + "WarheadInfo");
					ret.LoadYaml(node.Value);
					retList.Add(ret);
				}
			}

			return retList;
		}

		public bool IsValidAgainst(Actor victim, Actor firedBy)
		{
			//Find Warhead type and cast so that we can access the correct method
			var hasValidWarhead = false;
			for (int i = 0; i < Warheads.Count; i++)
			{
				var rawObj = Warheads[i];
				hasValidWarhead = hasValidWarhead || rawObj.IsValidAgainst(victim, firedBy);

				if (hasValidWarhead) break;
			}

			var hasValidTarget = true;
			var targetable = victim.TraitOrDefault<ITargetable>();
			if (targetable == null || !ValidTargets.Intersect(targetable.TargetTypes).Any()
				|| InvalidTargets.Intersect(targetable.TargetTypes).Any())
				hasValidTarget = false;

			if (hasValidTarget &&
				hasValidWarhead &&
				CheckTargetList(firedBy, victim, this.ValidTargets) &&
				!CheckTargetList(firedBy, victim, this.InvalidTargets))
				return true;

			return false;
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
				if (!ValidTargets.Intersect(cellInfo.TargetTypes).Any()
					|| InvalidTargets.Intersect(cellInfo.TargetTypes).Any())
					return false;

				return true;
			}

			return false;
		}

		static bool CheckTargetList(Actor firedBy, Actor victim, string[] targetList)
		{
			if (targetList.Length < 1)
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
