#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Mods.Common.Traits
{
	class RenderFlareInfo : RenderSimpleInfo
	{
		public readonly string OpenSequence = "open";

		public override object Create(ActorInitializer init) { return new RenderFlare(init, this); }
	}

	class RenderFlare : RenderSimple
	{
		public RenderFlare(ActorInitializer init, RenderFlareInfo info)
			: base(init, info, () => 0)
		{
			DefaultAnimation.PlayThen(info.OpenSequence, () => DefaultAnimation.PlayRepeating(info.Sequence));
		}
	}
}
