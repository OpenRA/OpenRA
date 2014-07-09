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
	public class WarheadInfo
	{
		[Desc("Distance from the explosion center at which damage is 1/2.")]
		public readonly WRange Spread = new WRange(43);

		[FieldLoader.LoadUsing("LoadVersus")]
		[Desc("Damage vs each armortype. 0% = can't target.")]
		public readonly Dictionary<string, float> Versus;

		[Desc("What types of targets are affected.", "Diplomacy keywords: Ally, Neutral, Enemy")]
		public readonly string[] ValidTargets = { "Air", "Ground", "Water", "Ally", "Neutral", "Enemy" };

		[Desc("What types of targets are unaffected.", "Overrules ValidTargets.", "Diplomacy keywords: Ally, Neutral, Enemy")]
		public readonly string[] InvalidTargets = { };

		[Desc("Can this damage resource patches?")]
		public readonly bool DestroyResources = false;

		[Desc("Will this splatter resources and which?")]
		public readonly string AddsResourceType = null;

		[Desc("Explosion effect to use.")]
		public readonly string Explosion = null;

		[Desc("Palette to use for explosion effect.")]
		public readonly string ExplosionPalette = "effect";

		[Desc("Explosion effect on hitting water (usually a splash).")]
		public readonly string WaterExplosion = null;

		[Desc("Palette to use for effect on hitting water (usually a splash).")]
		public readonly string WaterExplosionPalette = "effect";

		[Desc("Type of smudge to apply to terrain.")]
		public readonly string[] SmudgeType = { };

		[Desc("Size of the explosion. provide 2 values for a ring effect (outer/inner).")]
		public readonly int[] Size = { 0, 0 };

		[Desc("Infantry death animation to use")]
		public readonly string InfDeath = "1";

		[Desc("Sound to play on impact.")]
		public readonly string ImpactSound = null;

		[Desc("Sound to play on impact with water")]
		public readonly string WaterImpactSound = null;

		[Desc("How much (raw) damage to deal")]
		public readonly int Damage = 0;

		[Desc("Delay in ticks before dealing the damage, 0 = instant (old model).")]
		public readonly int Delay = 0;

		[Desc("Which damage model to use.")]
		public readonly DamageModel DamageModel = DamageModel.Normal;

		[Desc("Whether we should prevent prone response for infantry.")]
		public readonly bool PreventProne = false;

		[Desc("By what percentage should damage be modified against prone infantry.")]
		public readonly int ProneModifier = 50;

		public float EffectivenessAgainst(ActorInfo ai)
		{
			var health = ai.Traits.GetOrDefault<HealthInfo>();
			if (health == null)
				return 0f;

			var armor = ai.Traits.GetOrDefault<ArmorInfo>();
			if (armor == null || armor.Type == null)
				return 1;

			float versus;
			return Versus.TryGetValue(armor.Type, out versus) ? versus : 1;
		}

		public WarheadInfo(MiniYaml yaml)
		{
			FieldLoader.Load(this, yaml);
		}

		static object LoadVersus(MiniYaml y)
		{
			var nd = y.ToDictionary();
			return nd.ContainsKey("Versus")
				? nd["Versus"].ToDictionary(my => FieldLoader.GetValue<float>("(value)", my.Value))
				: new Dictionary<string, float>();
		}

		public bool IsValidAgainst(Actor victim, Actor firedBy)
		{
			//A target type is valid if it is in the valid targets list, and not in the invalid targets list.
			return CheckTargetList(victim, firedBy, this.ValidTargets) &&
				!CheckTargetList(victim, firedBy, this.InvalidTargets);
		}

		static bool CheckTargetList(Actor victim, Actor firedBy, string[] targetList)
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
	}

	public enum DamageModel
	{
		Normal,								// classic RA damage model: point actors, distance-based falloff
		PerCell,							// like RA's "nuke damage"
		HealthPercentage					// for MAD Tank
	}

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

		[FieldLoader.LoadUsing("LoadProjectile")] public IProjectileInfo Projectile;
		[FieldLoader.LoadUsing("LoadWarheads")] public List<WarheadInfo> Warheads;

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
			var ret = new List<WarheadInfo>();
			foreach (var w in yaml.Nodes)
				if (w.Key.Split('@')[0] == "Warhead")
					ret.Add(new WarheadInfo(w.Value));

			return ret;
		}

		public bool IsValidAgainst(Actor a)
		{
			var targetable = a.TraitOrDefault<ITargetable>();
			if (targetable == null || !ValidTargets.Intersect(targetable.TargetTypes).Any()
				|| InvalidTargets.Intersect(targetable.TargetTypes).Any())
				return false;

			if (Warheads.All(w => w.EffectivenessAgainst(a.Info) <= 0))
				return false;

			return true;
		}

		public bool IsValidAgainst(FrozenActor a)
		{
			var targetable = a.Info.Traits.GetOrDefault<ITargetableInfo>();
			if (targetable == null || !ValidTargets.Intersect(targetable.GetTargetTypes()).Any()
				|| InvalidTargets.Intersect(targetable.GetTargetTypes()).Any())
				return false;

			if (Warheads.All(w => w.EffectivenessAgainst(a.Info) <= 0))
				return false;

			return true;
		}

		public bool IsValidAgainst(Actor victim, Actor firedBy)
		{
			if (IsValidAgainst(victim) &&
				Warheads.Any(w => w.IsValidAgainst(victim, firedBy)) &&
				CheckTargetList(firedBy, victim, this.ValidTargets) && !CheckTargetList(firedBy, victim, this.InvalidTargets))
				return true;

			return false;
		}

		public bool IsValidAgainst(FrozenActor victim, Actor firedBy)
		{
			if (IsValidAgainst(victim) &&
				Warheads.Any(w => w.IsValidAgainst(victim.Actor, firedBy)) &&
				CheckTargetList(firedBy, victim.Actor, this.ValidTargets) && !CheckTargetList(firedBy, victim.Actor, this.InvalidTargets))
				return true;

			return false;
		}

		public bool IsValidAgainst(Target target, World world)
		{
			if (target.Type == TargetType.Actor)
				return IsValidAgainst(target.Actor);

			if (target.Type == TargetType.FrozenActor)
				return IsValidAgainst(target.FrozenActor);

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
