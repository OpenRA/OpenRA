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
	[Desc("Throws particles when the actor is destroyed that do damage on impact.")]
	public class ThrowsShrapnelInfo : ConditionalTraitInfo, IRulesetLoaded
	{
		[WeaponReference]
		[FieldLoader.Require]
		[Desc("The weapons used for shrapnel.")]
		public readonly string[] Weapons = Array.Empty<string>();

		[Desc("The amount of pieces of shrapnel to expel. Two values indicate a range.")]
		public readonly int[] Pieces = { 3, 10 };

		[Desc("The minimum and maximum distances the shrapnel may travel.")]
		public readonly WDist[] Range = { WDist.FromCells(2), WDist.FromCells(5) };

		public WeaponInfo[] WeaponInfos { get; private set; }

		public override object Create(ActorInitializer actor) { return new ThrowsShrapnel(this); }
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

	class ThrowsShrapnel : ConditionalTrait<ThrowsShrapnelInfo>, INotifyKilled
	{
		public ThrowsShrapnel(ThrowsShrapnelInfo info)
			: base(info) { }

		public void Killed(Actor self, AttackInfo attack)
		{
			if (IsTraitDisabled)
				return;

			foreach (var wep in Info.WeaponInfos)
			{
				var pieces = self.World.SharedRandom.Next(Info.Pieces[0], Info.Pieces[1]);
				var range = self.World.SharedRandom.Next(Info.Range[0].Length, Info.Range[1].Length);

				for (var i = 0; pieces > i; i++)
				{
					var rotation = WRot.FromYaw(new WAngle(self.World.SharedRandom.Next(1024)));
					var args = new ProjectileArgs
					{
						Weapon = wep,
						Facing = new WAngle(self.World.SharedRandom.Next(1024)),
						CurrentMuzzleFacing = () => WAngle.Zero,

						DamageModifiers = self.TraitsImplementing<IFirepowerModifier>()
							.Select(a => a.GetFirepowerModifier()).ToArray(),

						InaccuracyModifiers = self.TraitsImplementing<IInaccuracyModifier>()
							.Select(a => a.GetInaccuracyModifier()).ToArray(),

						RangeModifiers = self.TraitsImplementing<IRangeModifier>()
							.Select(a => a.GetRangeModifier()).ToArray(),

						Source = self.CenterPosition,
						CurrentSource = () => self.CenterPosition,
						SourceActor = self,
						PassiveTarget = self.CenterPosition + new WVec(range, 0, 0).Rotate(rotation)
					};

					self.World.AddFrameEndTask(x =>
					{
						if (args.Weapon.Projectile != null)
						{
							var projectile = args.Weapon.Projectile.Create(args);
							if (projectile != null)
								self.World.Add(projectile);

							if (args.Weapon.Report != null && args.Weapon.Report.Length > 0)
								Game.Sound.Play(SoundType.World, args.Weapon.Report, self.World, self.CenterPosition);
						}
					});
				}
			}
		}
	}
}
