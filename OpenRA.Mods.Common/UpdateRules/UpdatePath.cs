#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.UpdateRules.Rules;

namespace OpenRA.Mods.Common.UpdateRules
{
	public class UpdatePath
	{
		// Define known update paths from stable tags to the current bleed tip
		//
		// This file should be maintained separately on prep branches vs bleed.
		// The bleed version of this file should ignore the presence of the prep branch
		// and list rules from the playtest that forked the prep branch and current bleed.
		// The prep branch should maintain its own list of rules along the prep branch
		// until the eventual final release.
		//
		// When a final release has been tagged the update paths from the prep branch
		// can be merged back into bleed by replacing the forking-playtest-to-bleed path
		// with the prep playtest-to-playtest-to-release paths and finally a new/modified
		// release-to-bleed path.
		static readonly UpdatePath[] Paths =
		{
			new UpdatePath("release-20200202", "release-20200503", new UpdateRule[]
			{
				new RemoveYesNo(),
				new RemoveInitialFacingHardcoding(),
				new RemoveAirdropActorTypeDefault(),
				new RenameProneTime(),
				new RemoveWithPermanentInjury(),
				new AddResourceRenderer(),
				new ReformatChromeProvider(),
				new RenameSpins(),
				new CreateScreenShakeWarhead(),
				new RenameRallyPointPath(),
			}),

			new UpdatePath("release-20200503", "release-20210321", new UpdateRule[]
			{
				new AddPipDecorationTraits(),
				new ModernizeDecorationTraits(),
				new RenameHealCrateAction(),
				new RenameInfiltrationNotifications(),
				new MoveClassicFacingFudge(),
				new RenameWithNukeLaunch(),
				new SpawnActorPowerDefaultEffect(),
				new RemoveConditionManager(),
				new ConvertSupportPowerRangesToFootprint(),
				new UpdateTilesetColors(),
				new UpdateMapInits(),
				new CreateFlashPaletteEffectWarhead(),
				new ChangeTargetLineDelayToMilliseconds(),
				new ReplaceFacingAngles(),
				new RenameSelfHealing(),
				new ReplaceBurns(),
				new RemoveMuzzleSplitFacings(),
				new RenameStances(),
				new RemoveTurnToDock(),
				new RenameSmudgeSmokeFields(),
				new RenameCircleContrast(),
				new SplitDamagedByTerrain(),
				new RemoveLaysTerrain(),
			}),

			new UpdatePath("release-20210321", "playtest-20221203", new UpdateRule[]
			{
				new RenameMPTraits(),
				new RemovePlayerHighlightPalette(),
				new ReplaceWithColoredOverlayPalette(),
				new RemoveRenderSpritesScale(),
				new RemovePlaceBuildingPalette(),
				new ReplaceShadowPalette(),
				new ReplaceResourceValueModifiers(),
				new RemoveResourceType(),
				new ConvertBoundsToWDist(),
				new RemoveSmokeTrailWhenDamaged(),
				new ReplaceCrateSecondsWithTicks(),
				new UseMillisecondsForSounds(),
				new UnhardcodeSquadManager(),
				new RenameSupportPowerDescription(),
				new AttackBomberFacingTolerance(),
				new AttackFrontalFacingTolerance(),
				new RenameCloakTypes(),
				new SplitNukePowerMissileImage(),
				new ReplaceSequenceEmbeddedPalette(),
				new UnhardcodeBaseBuilderBotModule(),
				new UnhardcodeVeteranProductionIconOverlay(),
				new RenameContrailProperties(),
				new RemoveDomainIndex(),
			}),

			new UpdatePath("playtest-20221203", new UpdateRule[]
			{
				new TextNotificationsDisplayWidgetRemoveTime(),
			})
		};

		public static IEnumerable<UpdateRule> FromSource(ObjectCreator objectCreator, string source, bool chain = true)
		{
			// Use reflection to identify types
			var namedType = objectCreator.FindType(source);
			if (namedType != null && namedType.IsSubclassOf(typeof(UpdateRule)))
				return new[] { (UpdateRule)objectCreator.CreateBasic(namedType) };

			var namedPath = Paths.FirstOrDefault(p => p.source == source);
			return namedPath != null ? namedPath.Rules(chain) : null;
		}

		public static IEnumerable<string> KnownPaths { get { return Paths.Select(p => p.source); } }
		public static IEnumerable<string> KnownRules(ObjectCreator objectCreator)
		{
			return objectCreator.GetTypesImplementing<UpdateRule>().Select(t => t.Name);
		}

		readonly string source;
		readonly string chainToSource;
		readonly UpdateRule[] rules;
		UpdatePath(string source, UpdateRule[] rules)
			: this(source, null, rules) { }

		UpdatePath(string source, string chainToSource, UpdateRule[] rules)
		{
			this.source = source;
			this.rules = rules;
			this.chainToSource = chainToSource;
		}

		IEnumerable<UpdateRule> Rules(bool chain = true)
		{
			if (chainToSource != null && chain)
			{
				var child = Paths.First(p => p.source == chainToSource);
				return rules.Concat(child.Rules(chain));
			}

			return rules;
		}
	}
}
