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
	[Desc("Overrides the build time calculated by actor value.")]
	public class CustomBuildTimeValueInfo : TraitInfo<CustomBuildTimeValue>
	{
		public readonly int Value = 0;
	}

	public class CustomBuildTimeValue { }

	public static class CustomBuildTimeValueExts
	{
		public static int GetBuildTime(this ActorInfo a)
		{
			var csv = a.Traits.GetOrDefault<CustomBuildTimeValueInfo>();
			if (csv != null)
				return csv.Value;

			var cost = a.Traits.Contains<ValuedInfo>() ? a.Traits.Get<ValuedInfo>().Cost : 0;
			var time = cost
							* (25 * 60) /* frames per min */
							/ 1000;
			return
				time;
		}
	}
}
