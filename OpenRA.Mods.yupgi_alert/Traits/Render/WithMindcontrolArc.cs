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

/* Works without base engine modification */

namespace OpenRA.Mods.yupgi_alert.Traits
{
	public class WithMindcontrolArcInfo : ITraitInfo
	{
		[Desc("Color of the arc")]
		public readonly Color Color = Color.PaleVioletRed;

		[Desc("Height of the highest point")]
		public readonly WDist Height = new WDist(1024);

		public virtual object Create(ActorInitializer init) { return new WithMindcontrolArc(init.Self, this); }
	}

	public class WithMindcontrolArc : IRenderAboveShroudWhenSelected, INotifySelected
	{
		readonly WithMindcontrolArcInfo info;

		public WithMindcontrolArc(Actor self, WithMindcontrolArcInfo info)
		{
			this.info = info;
		}

		void INotifySelected.Selected(Actor a) { }

		IEnumerable<IRenderable> Arc(WPos src, WPos dest)
		{
			yield return new TargetLineRenderable(new[] { src, dest }, info.Color);
		}

		IEnumerable<IRenderable> IRenderAboveShroudWhenSelected.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			// Mindcontroller has multiple arc to the slaves
			var mcr = self.TraitOrDefault<Mindcontroller>();
			if (mcr != null)
			{
				foreach (var s in mcr.Slaves)
					foreach (var x in Arc(s.CenterPosition, self.CenterPosition))
						yield return x;

				yield break;
			}

			var master = self.Trait<Mindcontrollable>().Master;
			if (master == null)
				yield break;

			// Slaves only get one arc to the master.
			foreach (var x in Arc(self.CenterPosition, master.CenterPosition))
				yield return x;
		}
	}
}