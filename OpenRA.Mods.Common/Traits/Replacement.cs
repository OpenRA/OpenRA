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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class ReplacementInfo : TraitInfo<Replacement>
	{
		[FieldLoader.Require]
		[Desc("Replacement type (matched against Conditions in Replaceable).")]
		public readonly HashSet<string> ReplaceableTypes = new HashSet<string>();
	}

	public class Replacement { }
}
