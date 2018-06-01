#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
		[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:ParameterMustNotSpanMultipleLines",
			Justification = "Extracting update lists to temporary variables obfuscates the definitions.")]
		static readonly UpdatePath[] Paths =
		{
			new UpdatePath("incomplete-release-20171014", "release-20180218", new UpdateRule[]
			{
				new RemoveMobileOnRails(),
				new AircraftCanHoverGeneralization(),
				new AddNukeLaunchAnimation(),
				new CapturableChanges(),
				new DecoupleSelfReloading(),
				new RemovePlayerPaletteTileset(),
				new RenameWithTurreted(),
				new ScaleDefaultModHealth(),
				new ScaleSupportPowerSecondsToTicks(),
				new ChangeBuildableArea(),
				new ChangeCanPowerDown(),
				new DropPauseAnimationWhenDisabled(),
				new RemoveOutOfAmmo(),
				new ReplaceRequiresPower()
			}),

			new UpdatePath("release-20180218", "release-20180307", new UpdateRule[0]),

			new UpdatePath("release-20180307", new UpdateRule[]
			{
				// Bleed only changes here
				new RemoveTerrainTypeIsWaterFlag(),
				new DefineSquadExcludeHarvester(),
				new RemoveWeaponScanRadius(),
				new SplitAimAnimations(),
				new DefineSoundDefaults(),
				new RenameWormSpawner(),
				new RemoveWithReloadingSpriteTurret(),
				new ChangeIntensityToDuration(),
				new IgnoreAbstractActors(),
				new AddShakeToBridge(),
				new RemovePaletteFromCurrentTileset(),
				new DefineLocomotors(),
				new DefineOwnerLostAction(),
				new RenameEmitInfantryOnSell(),
				new SplitRepairDecoration(),
				new MoveHackyAISupportPowerDecisions(),
			})
		};

		public static IEnumerable<UpdateRule> FromSource(ObjectCreator objectCreator, string source)
		{
			// Use reflection to identify types
			var namedType = objectCreator.FindType(source);
			if (namedType != null && namedType.IsSubclassOf(typeof(UpdateRule)))
				return new[] { (UpdateRule)objectCreator.CreateBasic(namedType) };

			var namedPath = Paths.FirstOrDefault(p => p.source == source);
			return namedPath != null ? namedPath.Rules : null;
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

		IEnumerable<UpdateRule> Rules
		{
			get
			{
				if (chainToSource != null)
				{
					var chain = Paths.First(p => p.source == chainToSource);
					return rules.Concat(chain.Rules);
				}

				return rules;
			}
		}
	}
}
