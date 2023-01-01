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
using OpenRA.Mods.Common.Projectiles;
using OpenRA.Server;

namespace OpenRA.Mods.Common.Lint
{
	class CheckAngle : ILintRulesPass, ILintServerMapPass
	{
		void ILintRulesPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Ruleset rules)
		{
			Run(emitError, rules);
		}

		void ILintServerMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, MapPreview map, Ruleset mapRules)
		{
			Run(emitError, mapRules);
		}

		void Run(Action<string> emitError, Ruleset rules)
		{
			foreach (var weaponInfo in rules.Weapons)
			{
				if (weaponInfo.Value.Projectile is MissileInfo missile)
				{
					var minAngle = missile.MinimumLaunchAngle.Angle;
					var maxAngle = missile.MaximumLaunchAngle.Angle;

					// If both angles are identical, we only need to test one of them
					var testMaxAngle = minAngle != maxAngle;
					CheckLaunchAngles(weaponInfo.Key, minAngle, testMaxAngle, maxAngle, emitError);
				}

				if (weaponInfo.Value.Projectile is BulletInfo bullet)
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
				emitError($"Weapon `{weaponInfo}`: Projectile minimum LaunchAngle must not exceed (-)255!");
			if (testMaxAngle && InvalidAngle(maxAngle))
				emitError($"Weapon `{weaponInfo}`: Projectile maximum LaunchAngle must not exceed (-)255!");

			if ((minAngle < 256) && (maxAngle < 256) && (minAngle > maxAngle))
				emitError($"Weapon `{weaponInfo}`: Projectile minimum LaunchAngle must not exceed maximum LaunchAngle!");
			if ((minAngle > 768) && (maxAngle > 768) && (minAngle > maxAngle))
				emitError($"Weapon `{weaponInfo}`: Projectile minimum LaunchAngle must not exceed maximum LaunchAngle!");
			if ((minAngle < 256) && (maxAngle > 768))
				emitError($"Weapon `{weaponInfo}`: Projectile minimum LaunchAngle must not exceed maximum LaunchAngle!");
		}
	}
}
