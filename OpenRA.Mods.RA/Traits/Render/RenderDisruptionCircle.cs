#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	class RenderDisruptionCircleInfo : ITraitInfo, IPlaceBuildingDecoration
	{
		public IEnumerable<IRenderable> Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			if (!w.LobbyInfo.GlobalSettings.Fog)
				yield break;

			yield return new RangeCircleRenderable(
				centerPosition,
				ai.Traits.Get<CreatesDisruptionFieldInfo>().Range,
				0,
				Color.FromArgb(128, Color.Cyan),
				Color.FromArgb(96, Color.Black));

			foreach (var a in w.ActorsWithTrait<RenderDisruptionCircle>())
				if (a.Actor.Owner == a.Actor.World.LocalPlayer)
					foreach (var r in a.Trait.RenderAfterWorld(wr))
						yield return r;
		}

		public object Create(ActorInitializer init) { return new RenderDisruptionCircle(init.Self); }
	}

	class RenderDisruptionCircle : IPostRenderSelection
	{
		Actor self;

		public RenderDisruptionCircle(Actor self) { this.self = self; }

		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr)
		{
			if (self.Owner != self.World.LocalPlayer || !wr.World.LobbyInfo.GlobalSettings.Fog)
				yield break;

			yield return new RangeCircleRenderable(
				self.CenterPosition,
				self.Info.Traits.Get<CreatesDisruptionFieldInfo>().Range,
				0,
				Color.FromArgb(128, Color.Cyan),
				Color.FromArgb(96, Color.Black));
		}
	}
}