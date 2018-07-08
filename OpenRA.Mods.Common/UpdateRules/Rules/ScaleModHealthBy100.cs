﻿#region Copyright & License Information
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

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class ScaleModHealthBy100 : ScaleModHealth
	{
		public override IEnumerable<string> BeforeUpdate(ModData modData)
		{
			scale = 100;
			return base.BeforeUpdate(modData);
		}
	}
}
