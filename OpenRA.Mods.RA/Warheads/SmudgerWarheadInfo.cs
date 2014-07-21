#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class SmudgerWarheadInfo : BaseWarhead, IWarheadInfo
	{
		[Desc("Size of the area. A smudge will be created in each tile.", "Provide 2 values for a ring effect (outer/inner).")]
		public readonly int[] Size = { 0, 0 };

		[Desc("Type of smudge to apply to terrain.")]
		public readonly string[] SmudgeType = { };

		public SmudgerWarheadInfo() : base() { }

		public new void DoImpact(WPos pos, WeaponInfo weapon, Actor firedBy, float firepowerModifier)
		{
			var world = firedBy.World;
			var targetTile = world.Map.CellContaining(pos);
			var smudgeLayers = world.WorldActor.TraitsImplementing<SmudgeLayer>().ToDictionary(x => x.Info.Type);

			if (Size[0] > 0)
			{
				var allCells = world.Map.FindTilesInCircle(targetTile, Size[0]).ToList();

				// `smudgeCells` might want to just be an outer shell of the cells:
				IEnumerable<CPos> smudgeCells = allCells;
				if (Size.Length == 2)
					smudgeCells = smudgeCells.Except(world.Map.FindTilesInCircle(targetTile, Size[1]));

				// Draw the smudges:
				foreach (var sc in smudgeCells)
				{
					var smudgeType = world.Map.GetTerrainInfo(sc).AcceptsSmudgeType.FirstOrDefault(t => SmudgeType.Contains(t));
					if (smudgeType == null) continue;

					SmudgeLayer smudgeLayer;
					if (!smudgeLayers.TryGetValue(smudgeType, out smudgeLayer))
						throw new NotImplementedException("Unknown smudge type `{0}`".F(smudgeType));

					smudgeLayer.AddSmudge(sc);
				}
			}
			else
			{
				var smudgeType = world.Map.GetTerrainInfo(targetTile).AcceptsSmudgeType.FirstOrDefault(t => SmudgeType.Contains(t));
				if (smudgeType != null)
				{
					SmudgeLayer smudgeLayer;
					if (!smudgeLayers.TryGetValue(smudgeType, out smudgeLayer))
						throw new NotImplementedException("Unknown smudge type `{0}`".F(smudgeType));

					smudgeLayer.AddSmudge(targetTile);
				}
			}
		}
	}
}
