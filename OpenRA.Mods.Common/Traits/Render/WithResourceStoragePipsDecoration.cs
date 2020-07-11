#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
	public class WithResourceStoragePipsDecorationInfo : WithDecorationBaseInfo
	{
		[FieldLoader.Require]
		[Desc("Number of pips to display how filled unit is.")]
		public readonly int PipCount = 0;

		[Desc("If non-zero, override the spacing between adjacing pips.")]
		public readonly int2 PipStride = int2.Zero;

		[Desc("Image that defines the pip sequences.")]
		public readonly string Image = "pips";

		[SequenceReference("Image")]
		[Desc("Sequence used for empty pips.")]
		public readonly string EmptySequence = "pip-empty";

		[SequenceReference("Image")]
		[Desc("Sequence used for full pips.")]
		public readonly string FullSequence = "pip-green";

		[PaletteReference]
		public readonly string Palette = "chrome";

		public override object Create(ActorInitializer init) { return new WithResourceStoragePipsDecoration(init.Self, this); }
	}

	public class WithResourceStoragePipsDecoration : WithDecorationBase<WithResourceStoragePipsDecorationInfo>, INotifyOwnerChanged
	{
		readonly Animation pips;
		PlayerResources player;

		public WithResourceStoragePipsDecoration(Actor self, WithResourceStoragePipsDecorationInfo info)
			: base(self, info)
		{
			player = self.Owner.PlayerActor.Trait<PlayerResources>();
			pips = new Animation(self.World, info.Image);
		}

		protected override IEnumerable<IRenderable> RenderDecoration(Actor self, WorldRenderer wr, int2 screenPos)
		{
			pips.PlayRepeating(Info.EmptySequence);

			var palette = wr.Palette(Info.Palette);
			var pipSize = pips.Image.Size.XY.ToInt2();
			var pipStride = Info.PipStride != int2.Zero ? Info.PipStride : new int2(pipSize.X, 0);

			screenPos -= pipSize / 2;
			for (var i = 0; i < Info.PipCount; i++)
			{
				pips.PlayRepeating(player.Resources * Info.PipCount > i * player.ResourceCapacity ? Info.FullSequence : Info.EmptySequence);
				yield return new UISpriteRenderable(pips.Image, self.CenterPosition, screenPos, 0, palette, 1f);

				screenPos += pipStride;
			}
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			player = newOwner.PlayerActor.Trait<PlayerResources>();
		}
	}
}
