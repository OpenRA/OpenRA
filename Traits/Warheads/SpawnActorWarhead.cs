#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.AS.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Warheads
{
	[Desc("Spawn actors upon explosion.")]
	public class SpawnActorWarhead : Warhead
	{
		[Desc("The cell range to try placing the actors within.")]
		public readonly int Range = 10;

		[Desc("Actors to spawn.")]
		public readonly string[] Actors = { };

		[Desc("Try to parachute the actors. When unset, actors will just fall down visually using FallRate."
			+ " Requires the Parachutable trait on all actors if set.")]
		public readonly bool Paradrop = false;

		public readonly int FallRate = 130;

		[Desc("Map player to give the actors to. Defaults to the firer.")]
		public readonly string Owner = null;

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			var map = firedBy.World.Map;
			var targetCells = map.FindTilesInCircle(map.CellContaining(target.CenterPosition), Range);
			var cell = targetCells.GetEnumerator();

			foreach (var a in Actors)
			{
				var placed = false;
				var td = new TypeDictionary();
				if (Owner == null)
					td.Add(new OwnerInit(firedBy.Owner));
				else
					td.Add(new OwnerInit(firedBy.World.Players.First(p => p.InternalName == Owner)));

				var unit = firedBy.World.CreateActor(false, a.ToLowerInvariant(), td);

				while (cell.MoveNext())
				{
					if (unit.Trait<IPositionable>().CanEnterCell(cell.Current))
					{
						var pos = firedBy.World.Map.CenterOfCell(cell.Current) + new WVec(0, 0, target.CenterPosition.Z);
						firedBy.World.AddFrameEndTask(w =>
						{
							w.Add(unit);
							if (Paradrop)
								unit.QueueActivity(new Parachute(unit, pos));
							else
								unit.QueueActivity(new FallDown(unit, pos, FallRate));
						});
						placed = true;
						break;
					}
				}

				if (!placed)
					unit.Dispose();
			}
		}
	}
}
