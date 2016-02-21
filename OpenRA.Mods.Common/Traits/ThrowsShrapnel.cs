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

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Throws particles when the actor is destroyed that do damage on impact.")]
	public class ThrowsShrapnelInfo : ITraitInfo, IRulesetLoaded
	{
		[WeaponReference, FieldLoader.Require]
		[Desc("The weapons used for shrapnel.")]
		public readonly string[] Weapons = { };

		[Desc("The amount of pieces of shrapnel to expel. Two values indicate a range.")]
		public readonly int[] Pieces = { 3, 10 };

		[Desc("The minimum and maximum distances the shrapnel may travel.")]
		public readonly WDist[] Range = { WDist.FromCells(2), WDist.FromCells(5) };

		public WeaponInfo[] WeaponInfos { get; private set; }

		public object Create(ActorInitializer actor) { return new ThrowsShrapnel(this); }
		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			WeaponInfos = Weapons.Select(w => rules.Weapons[w.ToLowerInvariant()]).ToArray();
		}
	}

	class ThrowsShrapnel : INotifyKilled
	{
		readonly ThrowsShrapnelInfo info;

		public ThrowsShrapnel(ThrowsShrapnelInfo info)
		{
			this.info = info;
		}

		public void Killed(Actor self, AttackInfo attack)
		{
			foreach (var wep in info.WeaponInfos)
			{
				var pieces = self.World.SharedRandom.Next(info.Pieces[0], info.Pieces[1]);
				var range = self.World.SharedRandom.Next(info.Range[0].Length, info.Range[1].Length);

				for (var i = 0; pieces > i; i++)
				{
					var rotation = WRot.FromFacing(self.World.SharedRandom.Next(1024));
					var args = new ProjectileArgs
					{
						Weapon = wep,
						Facing = self.World.SharedRandom.Next(-1, 255),

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

							if (args.Weapon.Report != null && args.Weapon.Report.Any())
								Game.Sound.Play(args.Weapon.Report.Random(self.World.SharedRandom), self.CenterPosition);
						}
					});
				}
			}
		}
	}
}
