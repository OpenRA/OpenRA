#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA;
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("How much the unit is worth.")]
	public class ValuedInfo : TraitInfo<Valued>
	{
		[FieldLoader.Require]
		[DictionaryFromYamlKey]
		[Desc("The costs, listed separately for each resource type.",
		      "Used in production, but also for bounties so remember to set it even for NPCs.")]
		public Dictionary<string, int> Costs;

		public int TotalCost
		{
			get { return Costs.Values.Sum(); }
		}

		public IReadOnlyDictionary<string, int> GetModifiedCosts(Func<int, int> fn)
		{
			var modified = new Dictionary<string, int>();

			foreach (var p in Costs)
				modified.Add(p.Key, fn(p.Value));

			return modified.AsReadOnly();
		}
	}

	public class Valued { }
}
