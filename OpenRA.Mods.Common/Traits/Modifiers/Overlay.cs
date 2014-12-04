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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Display a colored overlay when a condition's timer is active.")]
	public class OverlayInfo : ConditionalTraitInfo, ITraitInfo
	{
		[Desc("Palette to use when rendering the overlay")]
		public readonly string Palette = "invuln";

		public object Create(ActorInitializer init) { return new Overlay(this); }
	}

	public class Overlay : ConditionalTrait<OverlayInfo>, IRenderModifier
	{
		public Overlay(OverlayInfo info)
			: base (info) { }

		public IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			foreach (var a in r)
			{
				yield return a;

				if (!IsTraitDisabled && !a.IsDecoration)
					yield return a.WithPalette(wr.Palette(Info.Palette))
						.WithZOffset(a.ZOffset + 1)
						.AsDecoration();
			}
		}
	}
}