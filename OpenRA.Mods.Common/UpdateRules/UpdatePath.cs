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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.UpdateRules.Rules;

namespace OpenRA.Mods.Common.UpdateRules
{
	public sealed class UpdatePath
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
			new("release-20210321", "release-20230225", new UpdateRule[]
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
				new RenameSupportPowerDescription(),
				new AttackBomberFacingTolerance(),
				new AttackFrontalFacingTolerance(),
				new RenameCloakTypes(),
				new SplitNukePowerMissileImage(),
				new ReplaceSequenceEmbeddedPalette(),
				new UnhardcodeVeteranProductionIconOverlay(),
				new RenameContrailProperties(),
				new RemoveDomainIndex(),
				new AddControlGroups(),

				// Execute these rules last to avoid premature yaml merge crashes.
				new UnhardcodeSquadManager(),
				new UnhardcodeBaseBuilderBotModule(),
			}),

			new("release-20230225", "release-20231010", new UpdateRule[]
			{
				new TextNotificationsDisplayWidgetRemoveTime(),
				new RenameEngineerRepair(),
				new ProductionTabsWidgetAddTabButtonCollection(),
				new RemoveTSRefinery(),
				new RenameMcvCrateAction(),
				new RenameContrailWidth(),
				new RemoveExperienceFromInfiltrates(),
				new AddColorPickerValueRange(),

				// Execute these rules last to avoid premature yaml merge crashes.
				new ExplicitSequenceFilenames(),
				new RemoveSequenceHasEmbeddedPalette(),
				new RemoveNegativeSequenceLength(),
			}),

			new("release-20231010", new UpdateRule[]
			{
				// bleed only changes here.
				new RemoveValidRelationsFromCapturable(),
				new ExtractResourceStorageFromHarvester(),
				new ReplacePaletteModifiers(),
				new RemoveConyardChronoReturnAnimation(),
				new RemoveEditorSelectionLayerProperties(),
				new AddMarkerLayerOverlay(),
				new AddSupportPowerBlockedCursor(),

				// Execute these rules last to avoid premature yaml merge crashes.
				new ReplaceCloakPalette(),
				new AbstractDocking(),
			}),
		};

		public static IReadOnlyCollection<UpdateRule> FromSource(ObjectCreator objectCreator, string source, bool chain = true)
		{
			// Use reflection to identify types
			var namedType = objectCreator.FindType(source);
			if (namedType != null && namedType.IsSubclassOf(typeof(UpdateRule)))
				return new[] { (UpdateRule)objectCreator.CreateBasic(namedType) };

			return Paths.FirstOrDefault(p => p.source == source)?.Rules(chain);
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

		IReadOnlyCollection<UpdateRule> Rules(bool chain = true)
		{
			if (chainToSource != null && chain)
			{
				var child = Paths.First(p => p.source == chainToSource);
				return rules.Concat(child.Rules(chain)).ToList();
			}

			return rules;
		}
	}
}
