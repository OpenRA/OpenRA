#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	public class SelectableInfo : ITraitInfo
	{
		public readonly bool Selectable = true;
		public readonly int Priority = 10;
		public readonly int[] Bounds = null;
		[VoiceReference] public readonly string Voice = null;

		public object Create(ActorInitializer init) { return new Selectable(init.self, this); }
	}

	public class Selectable : IPostRenderSelection
	{
		public SelectableInfo Info;
		Actor self;

		public Selectable(Actor self, SelectableInfo info)
		{
			this.self = self;
			Info = info;
		}

		IEnumerable<WPos> ActivityTargetPath()
		{
			if (!self.IsInWorld || self.IsDead())
				yield break;

			var activity = self.GetCurrentActivity();
			if (activity != null)
			{
				var targets = activity.GetTargets(self);
				yield return self.CenterPosition;

				foreach (var t in targets.Where(t => t.Type != TargetType.Invalid))
					yield return t.CenterPosition;
			}
		}

		public void RenderAfterWorld(WorldRenderer wr)
		{
			if (!Info.Selectable)
				return;

			new SelectionBoxRenderable(self, Color.White).Render(wr);
			new SelectionBarsRenderable(self).Render(wr);

			if (self.World.LocalPlayer != null && self.World.LocalPlayer.PlayerActor.Trait<DeveloperMode>().PathDebug)
				new TargetLineRenderable(ActivityTargetPath(), Color.Green).Render(wr);
		}
	}
}
