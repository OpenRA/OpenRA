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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class EncyclopediaInfo : TraitInfo
	{
		[Desc("Explains the purpose in the in-game encyclopedia.")]
		public readonly string Description = null;

		[Desc("Number for ordering the list.")]
		public readonly int Order;

		[Desc("Group under this heading.")]
		public readonly string Category;

		public override object Create(ActorInitializer init) { return Encyclopedia.Instance; }
	}

	public class Encyclopedia
	{
		public static readonly Encyclopedia Instance = new Encyclopedia();
		Encyclopedia() { }
	}
}
