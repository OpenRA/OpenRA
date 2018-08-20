#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Warheads
{
	public class CreateTintedCellsWarhead : DamageWarhead, IRulesetLoaded<WeaponInfo>
	{
		[Desc("Range between falloff steps in cells.")]
		public readonly WDist Spread = new WDist(1024);

		[Desc("Level percentage at each range step.")]
		public readonly int[] Falloff = { 100, 37, 14, 5, 0 };

		[Desc("The name of the layer we want to increase the level of.")]
		public readonly string LayerName = "radioactivity";

		[Desc("Determins whether you can go beyond Falloff[step] * MaxLevel for cells.")]
		public readonly bool ApplyFalloffToLevel = true;

		[Desc("Ranges at which each Falloff step is defined (in cells). Overrides Spread.")]
		public WDist[] Range = null;

		[Desc("Level this weapon puts on the ground. Accumulates over previously contaminated area.")]
		public int Level = 100;

		[Desc("It saturates at this level, by this weapon.")]
		public int MaxLevel = 500;

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (Range == null)
				Range = Exts.MakeArray(Falloff.Length, i => i * Spread);
			else
			{
				if (Range.Length != 1 && Range.Length != Falloff.Length)
					throw new YamlException("Number of range values must be 1 or equal to the number of Falloff values.");

				for (var i = 0; i < Range.Length - 1; i++)
					if (Range[i] > Range[i + 1])
						throw new YamlException("Range values must be specified in an increasing order.");
			}
		}

		public override void DoImpact(WPos pos, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			var world = firedBy.World;

			if (world.LocalPlayer != null)
			{
				var devMode = world.LocalPlayer.PlayerActor.TraitOrDefault<DebugVisualizations>();
				if (devMode != null && devMode.CombatGeometry)
				{
					WDist[] rng = Exts.MakeArray(Range.Length, i => WDist.FromCells(Range[i].Length));
					world.WorldActor.Trait<WarheadDebugOverlay>().AddImpact(pos, rng, DebugOverlayColor);
				}
			}

			var targetTile = world.Map.CellContaining(pos);
			for (var i = 0; i < Range.Length; i++)
			{
				var affectedCells = world.Map.FindTilesInCircle(targetTile, (int)Math.Ceiling((decimal)Range[i].Length / 1024));

				var raLayer = world.WorldActor.TraitsImplementing<TintedCellsLayer>()
					.First(l => l.Info.Name == LayerName);

				foreach (var cell in affectedCells)
				{
					int mul = GetIntensityFalloff((pos - world.Map.CenterOfCell(cell)).Length);
					IncreaseTintedCellLevel(cell, mul, Falloff[i], raLayer);
				}
			}
		}

		void IncreaseTintedCellLevel(CPos pos, int mul, int foff, TintedCellsLayer tcLayer)
		{
			if (ApplyFalloffToLevel)
				tcLayer.IncreaseLevel(pos, Level * mul / 100, MaxLevel * foff / 100);
			else
				tcLayer.IncreaseLevel(pos, Level * mul / 100, MaxLevel);
		}

		int GetIntensityFalloff(int distance)
		{
			var inner = Range[0].Length;
			for (var i = 1; i < Range.Length; i++)
			{
				var outer = Range[i].Length;
				if (outer > distance)
					return int2.Lerp(Falloff[i - 1], Falloff[i], distance - inner, outer - inner);

				inner = outer;
			}

			return 0;
		}
	}
}
