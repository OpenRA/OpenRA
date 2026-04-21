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
		[FluentReference]
		public readonly string Description;

		[Desc("Number for ordering the list.")]
		public readonly int Order;

		[Desc("Group under this heading.")]
		public readonly string Category;

		[Desc("Scale the actor preview.")]
		public readonly float Scale = 1f;

		[Desc("Sets the player color of the actor preview to a player defined in the shellmap.")]
		public readonly string PreviewOwner = null;

		[Desc("Ignore the Buildable trait when listing information.")]
		public readonly bool HideBuildable = false;

		[Desc("Specifies a production queue type if the actor can be built from multiple queues.")]
		public readonly string BuildableQueue = null;

		public override object Create(ActorInitializer init) { return Encyclopedia.Instance; }
	}

	public readonly struct Encyclopedia
	{
		public static readonly object Instance = default(Encyclopedia);
	}
}
