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

using NUnit.Framework;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Test
{
	[TestFixture]
	public class CoTVisibilityRouterTests
	{
		[Test]
		public void FriendlyAlwaysEmit_AllowsWithoutOverrides()
		{
			var info = new CoTVisibilityRouterInfo();
			var result = CoTVisibilityRouter.EvaluatePolicy(
				isFriendly: true,
				friendlyAlwaysEmit: true,
				teamDetected: false,
				useGenericMilsym: true,
				overrideTypeFlag: true,
				domain: CoTDomain.GroundMobile,
				info: info,
				out var overrideType,
				out var overrideMilsym);

			Assert.That(result, Is.True);
			Assert.That(overrideType, Is.Null);
			Assert.That(overrideMilsym, Is.Null);
		}

		[Test]
		public void HostileDetected_OverridesMilsymAndType_ByDomain()
		{
			var info = new CoTVisibilityRouterInfo();
			var result = CoTVisibilityRouter.EvaluatePolicy(
				isFriendly: false,
				friendlyAlwaysEmit: true, // Irrelevant in hostile case
				teamDetected: true,
				useGenericMilsym: true,
				overrideTypeFlag: true,
				domain: CoTDomain.GroundMobile,
				info: info,
				out var overrideType,
				out var overrideMilsym);

			Assert.That(result, Is.True);
			Assert.That(overrideMilsym, Is.EqualTo(info.GroundMobileMilsymId));
			Assert.That(overrideType, Is.EqualTo(info.GroundMobileType));
		}

		[Test]
		public void HostileNotDetected_Suppresses()
		{
			var info = new CoTVisibilityRouterInfo();
			var result = CoTVisibilityRouter.EvaluatePolicy(
				isFriendly: false,
				friendlyAlwaysEmit: true,
				teamDetected: false,
				useGenericMilsym: true,
				overrideTypeFlag: true,
				domain: CoTDomain.Aircraft,
				info: info,
				out var overrideType,
				out var overrideMilsym);

			Assert.That(result, Is.False);
			Assert.That(overrideType, Is.Null);
			Assert.That(overrideMilsym, Is.Null);
		}

		[Test]
		public void StealthGate_CloakedNotAttacking_Suppresses()
		{
			var suppress = CoTVisibilityRouter.EvaluateStealthGate(
				cloakActive: true,
				anyArmamentAiming: false,
				stealthEmitOnlyWhenAttacking: true);
			Assert.That(suppress, Is.True);
		}

		[Test]
		public void StealthGate_CloakedAttacking_Allows()
		{
			var suppress = CoTVisibilityRouter.EvaluateStealthGate(
				cloakActive: true,
				anyArmamentAiming: true,
				stealthEmitOnlyWhenAttacking: true);
			Assert.That(suppress, Is.False);
		}

		[Test]
		public void StealthGate_NotCloaked_DoesNotSuppress()
		{
			var suppress = CoTVisibilityRouter.EvaluateStealthGate(
				cloakActive: false,
				anyArmamentAiming: false,
				stealthEmitOnlyWhenAttacking: true);
			Assert.That(suppress, Is.False);
		}
	}
}
