#region Copyright & License Information
/*
 * Modded by Boolbada of OP Mod.
 * Modded from CreateResourceWarhead by OpenRA devs.
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Mods.yupgi_alert.Traits;

namespace OpenRA.Mods.yupgi_alert.Warheads
{
	public class CreateRadioactivityWarhead : DamageWarhead, IRulesetLoaded<WeaponInfo>
	{
		[Desc("Range between falloff steps, in cells")]
		public readonly int Spread = 1;

		[Desc("Radioactivity level percentage at each range step")]
		public readonly int[] Falloff = { 100, 37, 14, 5, 0 };

		// Since radioactivity level is accumulative, we pre-compute this var from Falloff. (Lookup table)
		private int[] FalloffDifference;

		[Desc("Ranges at which each Falloff step is defined (in cells). Overrides Spread.")]
		public int[] Range = null;

		[Desc("Radio activity level this weapon puts on the ground. Accumulates over previously contaminated area. (Sievert?)")]
		public int Level = 32; // in RA2, they used 500 for most weapons

		[Desc("Radio activity saturates at this level, by this weapon.")]
		// If you fire a weapon with Level = 500 twice, the level will never go beyond 500 (=MaxLevel).
		public int MaxLevel = 500;

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (Range != null)
			{
				if (Range.Length != 1 && Range.Length != Falloff.Length)
					throw new YamlException("Number of range values must be 1 or equal to the number of Falloff values.");

				for (var i = 0; i < Range.Length - 1; i++)
					if (Range[i] > Range[i + 1])
						throw new YamlException("Range values must be specified in an increasing order.");
			}
			else
				Range = Exts.MakeArray(Falloff.Length, i => i * Spread);

			// Compute FalloffDifference LUT.
			FalloffDifference = new int[Falloff.Length];
			for(var i = 0; i < FalloffDifference.Length-1; i++)
			{
				// with Falloff = { 100, 37, 14, 5, 0 }, you get
				// { 63, 23, 9, 5, 0 }
				FalloffDifference[i] = Falloff[i] - Falloff[i + 1];
			}
			FalloffDifference[FalloffDifference.Length - 1] = 0;
		}

		public override void DoImpact(WPos pos, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			var world = firedBy.World;
			var resLayer = world.WorldActor.Trait<RadioactivityLayer>();

			if (world.LocalPlayer != null)
			{
				var devMode = world.LocalPlayer.PlayerActor.TraitOrDefault<DeveloperMode>();
				if (devMode != null && devMode.ShowCombatGeometry)
				{
					WDist[] rng = Exts.MakeArray(Range.Length, i => WDist.FromCells(Range[i]));
					world.WorldActor.Trait<WarheadDebugOverlay>().AddImpact(pos, rng, DebugOverlayColor);
				}
			}

			// Accumulate radiation
			var targetTile = world.Map.CellContaining(pos);
			//for (var i = Range.Length-1; i >=0; i--)
			for (var i = 0; i < Range.Length; i++)
			{
				// Find affected cells, from outer Range down to inner range.
				var affectedCells = world.Map.FindTilesInAnnulus(targetTile, 0, Range[i]);

				var ra_layer = world.WorldActor.Trait<RadioactivityLayer>();

				foreach (var cell in affectedCells)
				{
					var foff = FalloffDifference[i];
					IncreaseRALevel(cell, foff, ra_layer);
				}
			}
		}

		// Increase radiation level of the cell at given pos, considering falloff
		void IncreaseRALevel(CPos pos, int foff, RadioactivityLayer ra_layer)
		{
			// increase RA level of the cell by this amount.
			int level = this.Level * foff / 100;
			ra_layer.IncreaseLevel(pos, level, MaxLevel);
		}
	}
}
