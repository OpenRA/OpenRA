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
	[Desc("Store the parent actor that spawn this actor.")]
	public sealed class HasParentInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new HasParent(init); }
	}

	public sealed class HasParent : ITransformActorInitModifier
	{
		public Actor Parent { get; private set; }
		public HasParent(ActorInitializer init)
		{
			var pa = init.GetOrDefault<ParentActorInit>()?.Value;
			if (pa != null)
				init.World.AddFrameEndTask(_ => Parent = pa.Actor(init.World).Value);
		}

		void ITransformActorInitModifier.ModifyTransformActorInit(Actor self, TypeDictionary init)
		{
			init.Add(new ParentActorInit(Parent));
		}
	}
}
