#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Detonate defined warheads at the current location at a set interval.")]
	public class FireWarheadsInfo : PausableConditionalTraitInfo, Requires<IMoveInfo>, IRulesetLoaded
	{
		[WeaponReference]
		[FieldLoader.Require]
		[Desc("Weapons to fire.")]
		public readonly string[] Weapons = Array.Empty<string>();

		[Desc("How long (in ticks) to wait before the first detonation.")]
		public readonly int StartCooldown = 0;

		[Desc("How long (in ticks) to wait after a detonation.")]
		public readonly int Interval = 1;

		public override object Create(ActorInitializer init) { return new FireWarheads(this); }

		public WeaponInfo[] WeaponInfos { get; private set; }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			WeaponInfos = Weapons.Select(w =>
			{
				var weaponToLower = w.ToLowerInvariant();
				if (!rules.Weapons.TryGetValue(weaponToLower, out var weapon))
					throw new YamlException($"Weapons Ruleset does not contain an entry '{weaponToLower}'");
				return weapon;
			}).ToArray();
		}
	}

	public class FireWarheads : PausableConditionalTrait<FireWarheadsInfo>, ITick
	{
		[Sync]
		int cooldown = 0;

		public FireWarheads(FireWarheadsInfo info)
			: base(info)
		{
			cooldown = info.StartCooldown;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (cooldown > 0)
				cooldown--;
			else
			{
				cooldown = Info.Interval;
				foreach (var wep in Info.WeaponInfos)
				{
					wep.Impact(Target.FromPos(self.CenterPosition), self);
					self.World.AddFrameEndTask(world =>
					{
						if (wep.Report != null && wep.Report.Length > 0)
							Game.Sound.Play(SoundType.World, wep.Report, world, self.CenterPosition);
					});
				}
			}
		}

		protected override void TraitDisabled(Actor self)
		{
			cooldown = Info.StartCooldown;
		}
	}
}
