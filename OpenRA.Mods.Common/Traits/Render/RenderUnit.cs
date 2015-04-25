#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class RenderUnitInfo : RenderSimpleInfo, Requires<IFacingInfo>
	{
		public override object Create(ActorInitializer init) { return new RenderUnit(init, this); }
	}

	public class RenderUnit : RenderSimple
	{
		readonly RenderUnitInfo info;

		public RenderUnit(ActorInitializer init, RenderUnitInfo info)
			: base(init, info)
		{
			this.info = info;
		}

		public void PlayCustomAnimation(Actor self, string newAnim, Action after)
		{
			DefaultAnimation.PlayThen(newAnim, () => { DefaultAnimation.Play(info.Sequence); if (after != null) after(); });
		}

		public void PlayCustomAnimRepeating(Actor self, string name)
		{
			DefaultAnimation.PlayThen(name,
				() => PlayCustomAnimRepeating(self, name));
		}

		public void PlayCustomAnimBackwards(Actor self, string name, Action after)
		{
			DefaultAnimation.PlayBackwardsThen(name,
				() => { DefaultAnimation.PlayRepeating(info.Sequence); if (after != null) after(); });
		}
	}
}
