#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Render the actor with a given color when cloaked.")]
	public class RenderCloakWithPaletteInfo : RenderCloakAsBaseInfo
	{
		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("The palette to use when cloaked when using Palette CloakStyle.")]
		public readonly string Palette = "cloak";

		[Desc("Indicates that CloakedPalette is a player palette when using Palette CloakStyle.")]
		public readonly bool IsPlayerPalette = false;

		public override object Create(ActorInitializer init) { return new RenderCloakWithPalette(init, this); }
	}

	public class RenderCloakWithPalette : RenderCloakAsBase<RenderCloakWithPaletteInfo>, INotifyOwnerChanged
	{
		PaletteReference palette = null;
		public RenderCloakWithPalette(ActorInitializer init, RenderCloakWithPaletteInfo info)
			: base(info) { }

		protected override IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			palette ??= wr.Palette(Info.IsPlayerPalette ? Info.Palette + self.Owner.InternalName : Info.Palette);
			return r.Select(a => !a.IsDecoration && a is IPalettedRenderable pr ? pr.WithPalette(palette) : a);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			palette = null;
		}
	}
}
