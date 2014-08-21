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

namespace OpenRA.Mods.RA.Render
{
	class RenderFlareInfo : RenderSimpleInfo
	{
		public override object Create(ActorInitializer init) { return new RenderFlare(init.self); }
	}

	class RenderFlare : RenderSimple
	{
		public RenderFlare(Actor self)
			: base(self, () => 0)
		{
			DefaultAnimation.PlayThen("open", () => DefaultAnimation.PlayRepeating("idle"));
		}
	}
}
