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
	[Desc("Allow a non-standard sell/repair value to avoid buy-sell exploits.")]
	public class CustomSellValueInfo : TraitInfo<CustomSellValue>
	{
		[FieldLoader.Require]
		[DictionaryFromYamlKey]
		[Desc("The values, listed separately for each resource type.")]
		public Dictionary<string, int> Values;
	}

	public class CustomSellValue { }

	public static class CustomSellValueExts
	{
		public static IReadOnlyDictionary<string, int> GetSellValues(this Actor a)
		{
			var csv = a.Info.TraitInfoOrDefault<CustomSellValueInfo>();
			if (csv != null) return csv.Values.AsReadOnly();

			var valued = a.Info.TraitInfoOrDefault<ValuedInfo>();
			if (valued != null) return valued.Costs.AsReadOnly();

			return new ReadOnlyDictionary<string, int>();
		}

		public static int GetTotalSellValue(this Actor a)
		{
			var csv = a.Info.TraitInfoOrDefault<CustomSellValueInfo>();
			if (csv != null) return csv.Values.Values.Sum();

			var valued = a.Info.TraitInfoOrDefault<ValuedInfo>();
			if (valued != null) return valued.TotalCost;

			return 0;
		}

		public static IReadOnlyDictionary<string, int> GetModifiedSellValues(this Actor a, Func<int, int> fn)
		{
			var csv = a.Info.TraitInfoOrDefault<CustomSellValueInfo>();
			if (csv != null)
			{
				var modified = new Dictionary<string, int>();

				foreach (var p in csv.Values)
					modified.Add(p.Key, fn(p.Value));

				return modified.AsReadOnly();
			}

			var valued = a.Info.TraitInfoOrDefault<ValuedInfo>();
			if (valued != null) return valued.GetModifiedCosts(fn);

			return new ReadOnlyDictionary<string, int>();
		}
	}
}
