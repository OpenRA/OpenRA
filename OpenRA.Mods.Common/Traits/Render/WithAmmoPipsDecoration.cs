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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public class WithAmmoPipsDecorationInfo : WithDecorationBaseInfo, Requires<AmmoPoolInfo>
	{
		[Desc("Number of pips to display. Defaults to the sum of the enabled AmmoPool.Ammo.")]
		public readonly int PipCount = -1;

		[Desc("If non-zero, override the spacing between adjacent pips.")]
		public readonly int2 PipStride = int2.Zero;

		[Desc("Image that defines the pip sequences.")]
		public readonly string Image = "pips";

		[SequenceReference(nameof(Image))]
		[Desc("Sequence used for empty pips.")]
		public readonly string EmptySequence = "pip-empty";

		[SequenceReference(nameof(Image))]
		[Desc("Sequence used for full pips.")]
		public readonly string FullSequence = "pip-green";

		[PaletteReference]
		public readonly string Palette = "chrome";

		[Desc("Name(s) of AmmoPool(s) that use this decoration. Leave empty to include all pools.")]
		public readonly string[] AmmoPools = Array.Empty<string>();

		public override object Create(ActorInitializer init) { return new WithAmmoPipsDecoration(init.Self, this); }
	}

	public class WithAmmoPipsDecoration : WithDecorationBase<WithAmmoPipsDecorationInfo>
	{
		readonly AmmoPool[] ammo;
		readonly Animation pips;

		public WithAmmoPipsDecoration(Actor self, WithAmmoPipsDecorationInfo info)
			: base(self, info)
		{
			if (info.AmmoPools.Length > 0)
				ammo = self.TraitsImplementing<AmmoPool>()
					.Where(ap => info.AmmoPools.Contains(ap.Info.Name))
					.ToArray();
			else
				ammo = self.TraitsImplementing<AmmoPool>().ToArray();

			pips = new Animation(self.World, info.Image);
		}

		protected override IEnumerable<IRenderable> RenderDecoration(Actor self, WorldRenderer wr, int2 screenPos)
		{
			pips.PlayRepeating(Info.EmptySequence);

			var palette = wr.Palette(Info.Palette);
			var pipSize = pips.Image.Size.XY.ToInt2();
			var pipStride = Info.PipStride != int2.Zero ? Info.PipStride : new int2(pipSize.X, 0);
			screenPos -= pipSize / 2;

			var currentAmmo = 0;
			var totalAmmo = 0;
			foreach (var a in ammo)
			{
				currentAmmo += a.CurrentAmmoCount;
				totalAmmo += a.Info.Ammo;
			}

			var pipCount = Info.PipCount > 0 ? Info.PipCount : totalAmmo;
			for (var i = 0; i < pipCount; i++)
			{
				pips.PlayRepeating(currentAmmo * pipCount > i * totalAmmo ? Info.FullSequence : Info.EmptySequence);
				yield return new UISpriteRenderable(pips.Image, self.CenterPosition, screenPos, 0, palette);

				screenPos += pipStride;
			}
		}
	}
}
