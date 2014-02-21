#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.GameRules
{
	public class WarheadInfo
	{
		[Desc("Distance from the explosion center at which damage is 1/2.")]
		public readonly WDist Spread = new WDist(43);
		[FieldLoader.LoadUsing("LoadVersus")]
		[Desc("Damage vs each armortype. 0% = can't target.")]
		public readonly Dictionary<string, float> Versus;
		[Desc("Can this damage ore?")]
		public readonly bool Ore = false;
		[Desc("Explosion effect to use.")]
		public readonly string Explosion = null;
		[Desc("Explosion effect on hitting water (usually a splash).")]
		public readonly string WaterExplosion = null;
		[Desc("Type of smudge to apply to terrain.")]
		public readonly string[] SmudgeType = { };
		[Desc("Size of the explosion. provide 2 values for a ring effect (outer/inner).")]
		public readonly int[] Size = { 0, 0 };
		[Desc("Infantry death animation to use")]
		public readonly int InfDeath = 1;
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
			return y.NodesDict.ContainsKey("Versus")
				? y.NodesDict["Versus"].NodesDict.ToDictionary(
					a => a.Key,
					a => FieldLoader.GetValue<float>("(value)", a.Value.Value))
				: new Dictionary<string, float>();
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
		public readonly WDist Range = WDist.Zero;
		public readonly string[] Report = null;
		[Desc("Rate of Fire")]
		public readonly int ROF = 1;
		public readonly int Burst = 1;
		public readonly bool Charges = false;
		public readonly string Palette = "effect";
		public readonly string[] ValidTargets = { "Ground", "Water" };
		public readonly string[] InvalidTargets = { };
		public readonly int BurstDelay = 5;
		public readonly WDist MinRange = WDist.Zero;

		[FieldLoader.LoadUsing("LoadProjectile")] public IProjectileInfo Projectile;
		[FieldLoader.LoadUsing("LoadWarheads")] public List<WarheadInfo> Warheads;

		public WeaponInfo(string name, MiniYaml content)
		{
			FieldLoader.Load(this, content);
		}

		static object LoadProjectile(MiniYaml yaml)
		{
			MiniYaml proj;
			if (!yaml.NodesDict.TryGetValue("Projectile", out proj))
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

		public bool IsValidAgainst(Target target, World world)
		{
			if (target.Type == TargetType.Actor)
				return IsValidAgainst(target.Actor);

			if (target.Type == TargetType.FrozenActor)
				return IsValidAgainst(target.FrozenActor);

			if (target.Type == TargetType.Terrain)
			{
				var cell = target.CenterPosition.ToCPos();
				if (!world.Map.IsInMap(cell))
					return false;

				var cellInfo = world.GetTerrainInfo(cell);
				if (!ValidTargets.Intersect(cellInfo.TargetTypes).Any()
				    || InvalidTargets.Intersect(cellInfo.TargetTypes).Any())
					return false;

				return true;
			}

			return false;
		}
	}
}
