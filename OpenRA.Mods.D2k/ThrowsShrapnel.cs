#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k
{
	[Desc("Throws particles when the actor is destroyed that do damage on impact.")]
	public class ThrowsShrapnelInfo : ITraitInfo
	{
		[WeaponReference]
		public string[] Weapons = { };
		public int[] Pieces = { 3, 10 };
		public WRange[] Range = { WRange.FromCells(2), WRange.FromCells(5) };
		public object Create(ActorInitializer actor) { return new ThrowsShrapnel(this); }
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
			foreach (var name in info.Weapons)
			{
				var wep = self.World.Map.Rules.Weapons[name.ToLowerInvariant()];
				var pieces = self.World.SharedRandom.Next(info.Pieces[0], info.Pieces[1]);
				var range = self.World.SharedRandom.Next(info.Range[0].Range, info.Range[1].Range);

				for (var i = 0; pieces > i; i++)
				{
					var rotation = WRot.FromFacing(self.World.SharedRandom.Next(1024));
					var args = new ProjectileArgs
					{
						Weapon = wep,
						Facing = self.World.SharedRandom.Next(-1, 255),

						// TODO: Convert to ints
						FirepowerModifier = self.TraitsImplementing<IFirepowerModifier>()
							.Select(a => a.GetFirepowerModifier() / 100f)
							.Product(),

						Source = self.CenterPosition,
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
								Sound.Play(args.Weapon.Report.Random(self.World.SharedRandom), self.CenterPosition);
						}
					});
				}
			}
		}
	}
}
