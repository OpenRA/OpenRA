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
using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public class LeaveSmudgeWarhead : Warhead
	{
		[Desc("Size of the area. A smudge will be created in each tile.", "Provide 2 values for a ring effect (outer/inner).")]
		public readonly int[] Size = { 0, 0 };

		[Desc("Type of smudge to apply to terrain.")]
		public readonly HashSet<string> SmudgeType = new HashSet<string>();

		[Desc("Percentual chance the smudge is created.")]
		public readonly int Chance = 100;

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			if (target.Type == TargetType.Invalid)
				return;

			var firedBy = args.SourceActor;
			var world = firedBy.World;

			if (Chance < world.LocalRandom.Next(100))
				return;

			var pos = target.CenterPosition;
			var dat = world.Map.DistanceAboveTerrain(pos);

			if (dat > AirThreshold)
				return;

			var targetTile = world.Map.CellContaining(pos);
			var smudgeLayers = world.WorldActor.TraitsImplementing<SmudgeLayer>().ToDictionary(x => x.Info.Type);

			var minRange = (Size.Length > 1 && Size[1] > 0) ? Size[1] : 0;
			var allCells = world.Map.FindTilesInAnnulus(targetTile, minRange, Size[0]);

			// Draw the smudges:
			foreach (var sc in allCells)
			{
				var smudgeType = world.Map.GetTerrainInfo(sc).AcceptsSmudgeType.FirstOrDefault(SmudgeType.Contains);
				if (smudgeType == null)
					continue;

				var cellActors = world.ActorMap.GetActorsAt(sc);
				if (cellActors.Any(a => !IsValidAgainst(a, firedBy)))
					continue;

				if (!smudgeLayers.TryGetValue(smudgeType, out var smudgeLayer))
					throw new NotImplementedException("Unknown smudge type `{0}`".F(smudgeType));

				smudgeLayer.AddSmudge(sc);
			}
		}
	}
}
