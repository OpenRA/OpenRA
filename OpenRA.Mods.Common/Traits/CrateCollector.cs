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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor can collect crates.")]
	public class CrateCollectorInfo : ITraitInfo
	{
		[Desc("Define collector type(s) checked by Crate and CrateAction for validity. Leave empty if actor is supposed to be able to collect any crate.")]
		public readonly BitSet<CollectorType> CollectorTypes = default(BitSet<CollectorType>);

		public bool All { get { return CollectorTypes == default(BitSet<CollectorType>); } }

		public object Create(ActorInitializer init) { return new CrateCollector(this); }
	}

	public class CrateCollector
	{
		public readonly CrateCollectorInfo Info;

		public CrateCollector(CrateCollectorInfo info)
		{
			Info = info;
		}
	}
}
