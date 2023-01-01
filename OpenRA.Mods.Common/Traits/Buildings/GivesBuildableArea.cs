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

using System.Collections.Generic;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor allows placement of other actors with 'RequiresBuildableArea' trait around it.")]
	public class GivesBuildableAreaInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Types of buildable area this actor gives.")]
		public readonly HashSet<string> AreaTypes = new HashSet<string>();

		public override object Create(ActorInitializer init) { return new GivesBuildableArea(this); }
	}

	public class GivesBuildableArea : ConditionalTrait<GivesBuildableAreaInfo>
	{
		public GivesBuildableArea(GivesBuildableAreaInfo info)
			: base(info) { }

		readonly HashSet<string> noAreaTypes = new HashSet<string>();

		public HashSet<string> AreaTypes => !IsTraitDisabled ? Info.AreaTypes : noAreaTypes;
	}
}
