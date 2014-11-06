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

namespace OpenRA.Mods.Common
{
	[Desc("Display a colored overlay when a timed upgrade is active.")]
	public class UpgradeOverlayInfo : ITraitInfo
	{
		[Desc("Upgrade that is required before this overlay is rendered")]
		public readonly string RequiresUpgrade = null;

		[Desc("Palette to use when rendering the overlay")]
		public readonly string Palette = "invuln";

		public object Create(ActorInitializer init) { return new UpgradeOverlay(this); }
	}

	public class UpgradeOverlay : IRenderModifier, IUpgradable
	{
		readonly UpgradeOverlayInfo info;
		bool enabled;

		public UpgradeOverlay(UpgradeOverlayInfo info)
		{
			this.info = info;
		}

		public IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			foreach (var a in r)
			{
				yield return a;

				if (enabled && !a.IsDecoration)
					yield return a.WithPalette(wr.Palette(info.Palette))
						.WithZOffset(a.ZOffset + 1)
						.AsDecoration();
			}
		}

		public bool AcceptsUpgrade(string type)
		{
			return type == info.RequiresUpgrade;
		}

		public void UpgradeAvailable(Actor self, string type, bool available)
		{
			if (type == info.RequiresUpgrade)
				enabled = available;
		}
	}
}