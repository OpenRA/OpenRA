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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Traits;

namespace OpenRA.GameRules
{
	public class ProjectileArgs
	{
		public WeaponInfo Weapon;
		public int[] DamageModifiers;
		public int[] InaccuracyModifiers;
		public int[] RangeModifiers;
		public int Facing;
		public WPos Source;
		public Func<WPos> CurrentSource;
		public Actor SourceActor;
		public WPos PassiveTarget;
		public Target GuidedTarget;
	}

	public interface IProjectile : IEffect { }
	public interface IProjectileInfo { IProjectile Create(ProjectileArgs args); }

	public sealed class WeaponInfo
	{
		[Desc("The maximum range the weapon can fire.")]
		public readonly WDist Range = WDist.Zero;

		[Desc("The sound played when the weapon is fired.")]
		public readonly string[] Report = null;

		[Desc("Delay in ticks between reloading ammo magazines.")]
		public readonly int ReloadDelay = 1;

		[Desc("Number of shots in a single ammo magazine.")]
		public readonly int Burst = 1;

		[Desc("What types of targets are affected.")]
		public readonly HashSet<string> ValidTargets = new HashSet<string> { "Ground", "Water" };

		[Desc("What types of targets are unaffected.", "Overrules ValidTargets.")]
		public readonly HashSet<string> InvalidTargets = new HashSet<string>();

		[Desc("Delay in ticks between firing shots from the same ammo magazine.")]
		public readonly int BurstDelay = 5;

		[Desc("The minimum range the weapon can fire.")]
		public readonly WDist MinRange = WDist.Zero;

		[FieldLoader.LoadUsing("LoadProjectile")]
		public readonly IProjectileInfo Projectile;

		[FieldLoader.LoadUsing("LoadWarheads")]
		public readonly List<IWarhead> Warheads = new List<IWarhead>();

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
			var retList = new List<IWarhead>();
			foreach (var node in yaml.Nodes.Where(n => n.Key.StartsWith("Warhead")))
			{
				var ret = Game.CreateObject<IWarhead>(node.Value.Value + "Warhead");
				FieldLoader.Load(ret, node.Value);
				retList.Add(ret);
			}

			return retList;
		}

		public bool IsValidTarget(IEnumerable<string> targetTypes)
		{
			return ValidTargets.Overlaps(targetTypes) && !InvalidTargets.Overlaps(targetTypes);
		}

		/// <summary>Checks if the weapon is valid against (can target) the target.</summary>
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
				if (!IsValidTarget(cellInfo.TargetTypes))
					return false;

				return true;
			}

			return false;
		}

		/// <summary>Checks if the weapon is valid against (can target) the actor.</summary>
		public bool IsValidAgainst(Actor victim, Actor firedBy)
		{
			var targetTypes = victim.GetEnabledTargetTypes();

			if (!IsValidTarget(targetTypes))
				return false;

			// PERF: Avoid LINQ.
			foreach (var warhead in Warheads)
				if (warhead.IsValidAgainst(victim, firedBy))
					return true;

			return false;
		}

		/// <summary>Checks if the weapon is valid against (can target) the frozen actor.</summary>
		public bool IsValidAgainst(FrozenActor victim, Actor firedBy)
		{
			if (!IsValidTarget(victim.TargetTypes))
				return false;

			if (!Warheads.Any(w => w.IsValidAgainst(victim, firedBy)))
				return false;

			return true;
		}

		/// <summary>Applies all the weapon's warheads to the target.</summary>
		public void Impact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			foreach (var warhead in Warheads)
			{
				var wh = warhead; // force the closure to bind to the current warhead

				Action a = () => wh.DoImpact(target, firedBy, damageModifiers);
				if (wh.Delay > 0)
					firedBy.World.AddFrameEndTask(w => w.Add(new DelayedAction(wh.Delay, a)));
				else
					a();
			}
		}
	}
}
