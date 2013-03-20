﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class RenderDetectionCircleInfo : TraitInfo<RenderDetectionCircle> { }
	class RenderDetectionCircle : IPreRenderSelection
	{
		public void RenderBeforeWorld(WorldRenderer wr, Actor self)
		{
			if (self.Owner != self.World.LocalPlayer)
				return;

			wr.DrawRangeCircleWithContrast(
				Color.FromArgb(128, Color.LimeGreen),
				self.CenterLocation.ToFloat2(), self.Info.Traits.Get<DetectCloakedInfo>().Range,
				Color.FromArgb(96, Color.Black),
				1);
		}
	}
}
