#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Where the unit should leave the building. Multiples are allowed if IDs are added: Exit@2, ...")]
	public class ExitInfo : ConditionalTraitInfo, Requires<IOccupySpaceInfo>
	{
		[Desc("Offset at which that the exiting actor is spawned relative to the center of the producing actor.")]
		public readonly WVec SpawnOffset = WVec.Zero;

		[Desc("Cell offset where the exiting actor enters the ActorMap relative to the topleft cell of the producing actor.")]
		public readonly CVec ExitCell = CVec.Zero;
		public readonly int Facing = -1;

		[Desc("Type tags on this exit.")]
		public readonly HashSet<string> ProductionTypes = new HashSet<string>();

		[Desc("Number of ticks to wait before moving into the world.")]
		public readonly int ExitDelay = 0;

		public override object Create(ActorInitializer init) { return new Exit(init, this); }
	}

	public class Exit : ConditionalTrait<ExitInfo>
	{
		public Exit(ActorInitializer init, ExitInfo info)
			: base(info) { }
	}

	public static class ExitExts
	{
		public static Exit FirstExitOrDefault(this Actor actor, string productionType = null)
		{
			var all = actor.TraitsImplementing<Exit>()
				.Where(Exts.IsTraitEnabled);

			if (string.IsNullOrEmpty(productionType))
				return all.FirstOrDefault(e => e.Info.ProductionTypes.Count == 0);

			return all.FirstOrDefault(e => e.Info.ProductionTypes.Count == 0 || e.Info.ProductionTypes.Contains(productionType));
		}

		public static IEnumerable<Exit> Exits(this Actor actor, string productionType = null)
		{
			var all = actor.TraitsImplementing<Exit>()
				.Where(Exts.IsTraitEnabled);

			if (string.IsNullOrEmpty(productionType))
				return all.Where(e => e.Info.ProductionTypes.Count == 0);

			return all.Where(e => e.Info.ProductionTypes.Count == 0 || e.Info.ProductionTypes.Contains(productionType));
		}

		public static Exit RandomExitOrDefault(this Actor actor, World world, string productionType, Func<Exit, bool> p = null)
		{
			var allOfType = Exits(actor, productionType);
			if (!allOfType.Any())
				return null;

			var shuffled = allOfType.Shuffle(world.SharedRandom);
			return p != null ? shuffled.FirstOrDefault(p) : shuffled.First();
		}
	}
}
