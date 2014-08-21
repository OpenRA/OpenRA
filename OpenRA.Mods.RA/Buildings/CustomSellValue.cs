#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Buildings
{
	[Desc("Allow a non-standard sell/repair value to avoid buy-sell exploits.")]
	public class CustomSellValueInfo : TraitInfo<CustomSellValue>
	{
		public readonly int Value = 0;
	}

	public class CustomSellValue { }

	public static class CustomSellValueExts
	{
		public static int GetSellValue( this Actor a )
		{
			var csv = a.Info.Traits.GetOrDefault<CustomSellValueInfo>();
			if (csv != null) return csv.Value;

			var valued = a.Info.Traits.GetOrDefault<ValuedInfo>();
			if (valued != null) return valued.Cost;

			return 0;
		}
	}
}
