#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actor can capture ProximityCapturable actors.")]
	public class ProximityCaptorInfo : TraitInfo<ProximityCaptor>
	{
		[FieldLoader.Require]
		public readonly BitSet<CaptureType> Types = default;
	}

	public class ProximityCaptor { }
}
