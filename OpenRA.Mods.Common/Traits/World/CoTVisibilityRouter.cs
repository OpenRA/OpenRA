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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Routes CoT emissions based on FoW/visibility policy.")]
	public sealed class CoTVisibilityRouterInfo : TraitInfo
	{
		[Desc("Union allied detection/vision when deciding hostile visibility.")]
		public readonly bool TeamSharing = true;

		[Desc("When true, hostiles use a generic MIL-STD-2525C identifier by domain.")]
		public readonly bool UseGenericMilsym = true;

		[Desc("When true, also override CoT type string for hostiles by domain.")]
		public readonly bool OverrideType = true;

		[Desc("Generic MIL-STD-2525C ID for ground mobile hostiles.")]
		public readonly string GroundMobileMilsymId = "SHGP-----------";
		[Desc("Generic MIL-STD-2525C ID for building/installation hostiles.")]
		public readonly string BuildingMilsymId = "SHGPI-----H----";
		[Desc("Generic MIL-STD-2525C ID for aircraft hostiles.")]
		public readonly string AircraftMilsymId = "SHAP-----------";
		[Desc("Generic MIL-STD-2525C ID for vessel/submarine hostiles.")]
		public readonly string VesselMilsymId = "SHUP-----------";

		[Desc("CoT type for ground mobile hostiles.")]
		public readonly string GroundMobileType = "a-h-G";
		[Desc("CoT type for building/installation hostiles.")]
		public readonly string BuildingType = "a-h-G-I";
		[Desc("CoT type for aircraft hostiles.")]
		public readonly string AircraftType = "a-h-A";
		[Desc("CoT type for vessel/submarine hostiles.")]
		public readonly string VesselType = "a-h-U";

		[Desc("Seconds a hostile marker should remain after detection is lost.")]
		public readonly int StaleSecondsWhenLost = 1;

		[Desc("Always emit CoT for friendly (self + allies) regardless of FoW.")]
		public readonly bool FriendlyAlwaysEmit = true;

		[Desc("Treat stealth actors as non-emitting unless attacking.")]
		public readonly bool StealthEmitOnlyWhenAttacking = true;

		public override object Create(ActorInitializer init) { return new CoTVisibilityRouter(init.Self, this); }
	}

	public enum CoTDomain : byte { GroundMobile, Building, Aircraft, Vessel }

	public sealed class CoTVisibilityRouter
	{
		readonly CoTVisibilityRouterInfo info;
		readonly World world;

		public CoTVisibilityRouter(Actor self, CoTVisibilityRouterInfo info)
		{
			this.info = info;
			world = self.World;
		}

		public int StaleSecondsOnLoss => Math.Max(1, info.StaleSecondsWhenLost);

		public bool ShouldEmit(Actor self, CoTDomain domain, out string overrideType, out string overrideMilsym)
		{
			// Prefer the local controlling player to avoid leaking visibility from observers or debug render contexts.
			var viewer = world.LocalPlayer ?? world.RenderPlayer;
			return ShouldEmitInternal(self, viewer, domain, out overrideType, out overrideMilsym);
		}

		public bool ShouldEmit(Actor self, Player viewer, CoTDomain domain, out string overrideType, out string overrideMilsym)
		{
			return ShouldEmitInternal(self, viewer, domain, out overrideType, out overrideMilsym);
		}

		public static bool EvaluatePolicy(
			bool isFriendly,
			bool friendlyAlwaysEmit,
			bool teamDetected,
			bool useGenericMilsym,
			bool overrideTypeFlag,
			CoTDomain domain,
			CoTVisibilityRouterInfo info,
			out string overrideType,
			out string overrideMilsym)
		{
			overrideType = null;
			overrideMilsym = null;

			if (isFriendly && friendlyAlwaysEmit)
				return true;

			if (!teamDetected)
				return false;

			if (useGenericMilsym)
				overrideMilsym = domain switch
				{
					CoTDomain.GroundMobile => info.GroundMobileMilsymId,
					CoTDomain.Building => info.BuildingMilsymId,
					CoTDomain.Aircraft => info.AircraftMilsymId,
					CoTDomain.Vessel => info.VesselMilsymId,
					_ => info.GroundMobileMilsymId
				};

			if (overrideTypeFlag)
				overrideType = domain switch
				{
					CoTDomain.GroundMobile => info.GroundMobileType,
					CoTDomain.Building => info.BuildingType,
					CoTDomain.Aircraft => info.AircraftType,
					CoTDomain.Vessel => info.VesselType,
					_ => info.GroundMobileType
				};

			return true;
		}

		public static bool EvaluateStealthGate(bool cloakActive, bool anyArmamentAiming, bool stealthEmitOnlyWhenAttacking)
		{
			if (!stealthEmitOnlyWhenAttacking)
				return false;

			if (!cloakActive)
				return false;

			return !anyArmamentAiming;
		}

		bool ShouldEmitInternal(Actor self, Player viewer, CoTDomain domain, out string overrideType, out string overrideMilsym)
		{
			overrideType = null;
			overrideMilsym = null;

			if (viewer == null)
			{
				// No viewer: be conservative, emit friendlies (if allowed), suppress enemies
				var alliedWithLocal = self.Owner != null
					&& world.LocalPlayer != null
					&& self.Owner.IsAlliedWith(world.LocalPlayer);
				return EvaluatePolicy(
					alliedWithLocal,
					info.FriendlyAlwaysEmit,
					false,
					info.UseGenericMilsym,
					info.OverrideType,
					domain,
					info,
					out overrideType,
					out overrideMilsym);
			}

			var isFriendly = self.Owner != null && (self.Owner == viewer || self.Owner.IsAlliedWith(viewer));
			if (isFriendly && info.FriendlyAlwaysEmit)
				return EvaluatePolicy(true, info.FriendlyAlwaysEmit, true, info.UseGenericMilsym, info.OverrideType, domain, info, out overrideType, out overrideMilsym);

			// Enemy logic
			var detected = self.CanBeViewedByPlayer(viewer);

			// Stealth exception: when enabled, cloaked actors should only emit while actively attacking.
			// We approximate "attacking" as any AttackBase reporting IsAiming=true this tick.
			var stealthAttacking = false;
			if (info.StealthEmitOnlyWhenAttacking)
			{
				var cloak = self.TraitOrDefault<Cloak>();
				if (cloak != null && cloak.Cloaked)
				{
					stealthAttacking = self
						.TraitsImplementing<AttackBase>()
						.Any(ab => ab.IsAiming);

					if (EvaluateStealthGate(true, stealthAttacking, info.StealthEmitOnlyWhenAttacking))
						return false; // cloaked and not attacking: suppress regardless of team detection
				}
			}

			if (!detected && info.TeamSharing)
			{
				foreach (var p in world.Players)
				{
					// Only consider real playable teammates. Exclude spectators/non-combatants (e.g., the shared observer player)
					if (p == viewer || !p.Playable || p.Spectating || p.NonCombatant)
						continue;
					if (!p.IsAlliedWith(viewer))
						continue;
					if (self.CanBeViewedByPlayer(p))
					{
						detected = true;
						break;
					}
				}
			}

			// Treat stealth-attacking as detected to allow emission even if not (yet) visible via normal means.
			detected |= stealthAttacking;

			return EvaluatePolicy(
				isFriendly,
				info.FriendlyAlwaysEmit,
				detected,
				info.UseGenericMilsym,
				info.OverrideType,
				domain,
				info,
				out overrideType,
				out overrideMilsym);
		}
	}
}
