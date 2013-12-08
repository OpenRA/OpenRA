﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	public class RenderUnitInfo : RenderSimpleInfo, Requires<IFacingInfo>
	{
		public override object Create(ActorInitializer init) { return new RenderUnit(init.self); }
	}

	public class RenderUnit : RenderSimple
	{
		public RenderUnit(Actor self)
			: base(self) { }

		public void PlayCustomAnimation(Actor self, string newAnim, Action after)
		{
			Anim.PlayThen(newAnim, () => { Anim.Play("idle"); if (after != null) after(); });
		}

		public void PlayCustomAnimRepeating(Actor self, string name)
		{
			Anim.PlayThen(name,
				() => { PlayCustomAnimRepeating(self, name); });
		}

		public void PlayCustomAnimBackwards(Actor self, string name, Action a)
		{
			Anim.PlayBackwardsThen(name,
				() => { Anim.PlayRepeating("idle"); a(); });
		}
	}
}
