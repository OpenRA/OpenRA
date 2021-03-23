#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.Common.Projectiles;

namespace OpenRA.Mods.Common.Lint
{
	class CheckAngle : ILintRulesPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Ruleset rules)
		{
			foreach (var weaponInfo in rules.Weapons)
			{
				var missile = weaponInfo.Value.Projectile as MissileInfo;
				if (missile != null)
				{
					var minAngle = missile.MinimumLaunchAngle.Angle;
					var maxAngle = missile.MaximumLaunchAngle.Angle;

					// If both angles are identical, we only need to test one of them
					var testMaxAngle = minAngle != maxAngle;
					CheckLaunchAngles(weaponInfo.Key, minAngle, testMaxAngle, maxAngle, emitError);
				}

				var bullet = weaponInfo.Value.Projectile as BulletInfo;
				if (bullet != null)
				{
					var minAngle = bullet.LaunchAngle[0].Angle;
					var maxAngle = bullet.LaunchAngle.Length > 1 ? bullet.LaunchAngle[1].Angle : minAngle;

					// If both angles are identical, we only need to test one of them
					var testMaxAngle = minAngle != maxAngle;
					CheckLaunchAngles(weaponInfo.Key, minAngle, testMaxAngle, maxAngle, emitError);
				}
			}
		}

		static bool InvalidAngle(int value)
		{
			return value > 255 && value < 769;
		}

		static void CheckLaunchAngles(string weaponInfo, int minAngle, bool testMaxAngle, int maxAngle, Action<string> emitError)
		{
			if (InvalidAngle(minAngle))
				emitError("Weapon `{0}`: Projectile minimum LaunchAngle must not exceed (-)255!".F(weaponInfo));
			if (testMaxAngle && InvalidAngle(maxAngle))
				emitError("Weapon `{0}`: Projectile maximum LaunchAngle must not exceed (-)255!".F(weaponInfo));

			if ((minAngle < 256) && (maxAngle < 256) && (minAngle > maxAngle))
				emitError("Weapon `{0}`: Projectile minimum LaunchAngle must not exceed maximum LaunchAngle!".F(weaponInfo));
			if ((minAngle > 768) && (maxAngle > 768) && (minAngle > maxAngle))
				emitError("Weapon `{0}`: Projectile minimum LaunchAngle must not exceed maximum LaunchAngle!".F(weaponInfo));
			if ((minAngle < 256) && (maxAngle > 768))
				emitError("Weapon `{0}`: Projectile minimum LaunchAngle must not exceed maximum LaunchAngle!".F(weaponInfo));
		}
	}
}
