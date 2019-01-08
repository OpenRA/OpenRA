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

using System.Collections.Generic;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class ScaleDefaultModHealth : ScaleModHealth
	{
		static readonly Dictionary<string, int> ModScales = new Dictionary<string, int>()
		{
			{ "cnc", 100 },
			{ "ra", 100 },
			{ "d2k", 10 },
			{ "ts", 100 }
		};

		public override IEnumerable<string> BeforeUpdate(ModData modData)
		{
			ModScales.TryGetValue(modData.Manifest.Id, out scale);
			return base.BeforeUpdate(modData);
		}
	}
}
