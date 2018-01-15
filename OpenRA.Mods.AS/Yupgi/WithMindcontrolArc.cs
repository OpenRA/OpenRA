#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Yupgi_alert.Graphics;

/* Works without base engine modification */

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	public interface IRenderAboveShroudWhenSelected { IEnumerable<IRenderable> RenderAboveShroud(Actor self, WorldRenderer wr); }

	public class WithMindcontrolArcInfo : ITraitInfo
	{
		[Desc("Color of the arc")]
		public readonly Color Color = Color.FromArgb(128, Color.PaleVioletRed);

		[Desc("Height of the highest point")]
		public readonly WDist Height = new WDist(1024);

		[Desc("Drawing from self.CenterPosition draws the curve from the foot. Add this much for better looks.")]
		public readonly WVec Offset = new WVec(0, 0, 512);

		[Desc("Angle of the ballistic arc, in WAngle")]
		public readonly WAngle Angle = new WAngle(64);

		[Desc("Draw with this many piecewise-linear lines")]
		public readonly int Segments = 16;

		public virtual object Create(ActorInitializer init) { return new WithMindcontrolArc(init.Self, this); }
	}

	public class WithMindcontrolArc : IRenderAboveShroudWhenSelected, INotifySelected, INotifyCreated
	{
		readonly WithMindcontrolArcInfo info;
		Mindcontroller mindController;
		Mindcontrollable mindControllable;

		public WithMindcontrolArc(Actor self, WithMindcontrolArcInfo info)
		{
			this.info = info;
		}

		public void Created(Actor self)
		{
			mindController = self.TraitOrDefault<Mindcontroller>();
			mindControllable = self.TraitOrDefault<Mindcontrollable>();
		}

		void INotifySelected.Selected(Actor a) { }

		IEnumerable<IRenderable> IRenderAboveShroudWhenSelected.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			// Mindcontroller has multiple arc to the slaves
			if (mindController != null)
			{
				foreach (var s in mindController.Slaves)
					yield return new ArcRenderable(
						self.CenterPosition + info.Offset,
						s.CenterPosition + info.Offset,
						info.Angle, info.Color, info.Segments);
				yield break;
			}

			if (mindControllable == null || mindControllable.Master == null)
				yield break;

			// Slaves only get one arc to the master.
			yield return new ArcRenderable(
				mindControllable.Master.CenterPosition + info.Offset,
				self.CenterPosition + info.Offset,
				info.Angle, info.Color, info.Segments);
		}
	}
}