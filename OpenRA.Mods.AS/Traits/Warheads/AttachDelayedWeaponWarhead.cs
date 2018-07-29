#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Warheads
{
	[Desc("This warhead can attach a DelayedWeapon to the target. Requires an appropriate type of DelayedWeaponAttachable trait to function properly.")]
	public class AttachDelayedWeaponWarhead : WarheadAS, IRulesetLoaded<WeaponInfo>
	{
		[WeaponReference, FieldLoader.Require]
		public readonly string Weapon = "";

		[FieldLoader.Require]
		[Desc("Type of the DelayedWeapon.")]
		public readonly string Type = "";

		[Desc("Range of targets to be attached.")]
		public readonly WDist Range = WDist.FromCells(1);

		[Desc("Trigger the DelayedWeapon after these amount of ticks.")]
		public readonly int TriggerTime = 30;

		[Desc("DeathType(s) that trigger the DelayedWeapon to activate. Leave empty to always trigger the DelayedWeapon on death.")]
		public readonly BitSet<DamageType> DeathTypes = default(BitSet<DamageType>);

		public WeaponInfo WeaponInfo;

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (!rules.Weapons.TryGetValue(Weapon.ToLowerInvariant(), out WeaponInfo))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(Weapon.ToLowerInvariant()));
		}

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			if (!target.IsValidFor(firedBy))
				return;

			var pos = target.CenterPosition;

			if (!IsValidImpact(pos, firedBy))
				return;

			var availableActors = firedBy.World.FindActorsOnCircle(pos, Range);
			foreach (var actor in availableActors)
			{
				if (!IsValidAgainst(actor, firedBy))
					continue;

				if (actor.IsDead)
					continue;

				var activeShapes = actor.TraitsImplementing<HitShape>().Where(Exts.IsTraitEnabled);
				if (!activeShapes.Any())
					continue;

				var distance = activeShapes.Min(t => t.Info.Type.DistanceFromEdge(pos, actor));

				if (distance > Range)
					continue;

				var attachable = actor.TraitsImplementing<DelayedWeaponAttachable>().FirstOrDefault(a => a.CanAttach(Type));
				if (attachable != null)
					attachable.Attach(new DelayedWeaponTrigger(this, firedBy));
			}
		}
	}
}
