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

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Throws particles when the actor is destroyed that do damage on impact.")]
	public class FireProjectilesOnDeathInfo : ConditionalTraitInfo, IRulesetLoaded
	{
		[WeaponReference]
		[FieldLoader.Require]
		[Desc("The weapons used for shrapnel.")]
		public readonly string[] Weapons = [];

		[Desc("What damage type needs to kill the actor to trigger the firing of projectiles? " +
			"Leave empty to ignore damage types.")]
		public readonly BitSet<DamageType> DeathTypes = default;

		[Desc("The minimal amount of health loss required to trigger projectiles.")]
		public readonly int MinimumDamage = 0;

		[Desc("The maximum amount of health loss required to trigger projectiles.")]
		public readonly int MaximumDamage = int.MaxValue;

		[Desc("The amount of pieces of shrapnel to expel. Two values indicate a range.")]
		public readonly int[] Pieces = [3, 10];

		[Desc("The minimum and maximum distances the shrapnel may travel.")]
		public readonly WDist[] Range = [WDist.FromCells(2), WDist.FromCells(5)];

		public WeaponInfo[] WeaponInfos { get; private set; }

		public override object Create(ActorInitializer actor) { return new FireProjectilesOnDeath(this); }
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

	public class FireProjectilesOnDeath : ConditionalTrait<FireProjectilesOnDeathInfo>, INotifyKilled
	{
		public FireProjectilesOnDeath(FireProjectilesOnDeathInfo info)
			: base(info) { }

		void INotifyKilled.Killed(Actor self, AttackInfo attack)
		{
			if (IsTraitDisabled)
				return;

			if (!Info.DeathTypes.IsEmpty && !attack.Damage.DamageTypes.Overlaps(Info.DeathTypes))
				return;

			if (attack.Damage.Value <= Info.MinimumDamage || attack.Damage.Value >= Info.MaximumDamage)
				return;

			foreach (var wep in Info.WeaponInfos)
			{
				var pieces = Util.RandomInRange(self.World.SharedRandom, Info.Pieces);
				var range = self.World.SharedRandom.Next(Info.Range[0].Length, Info.Range[1].Length);

				for (var i = 0; pieces > i; i++)
				{
					var rotation = WRot.FromYaw(new WAngle(self.World.SharedRandom.Next(1024)));
					var dat = self.World.Map.DistanceAboveTerrain(self.CenterPosition);
					var source = dat.Length < 0 ? self.CenterPosition - new WVec(0, 0, dat.Length) : self.CenterPosition;
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

						Source = source,
						CurrentSource = () => source,
						SourceActor = self,
						PassiveTarget = source + new WVec(range, 0, 0).Rotate(rotation)
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
