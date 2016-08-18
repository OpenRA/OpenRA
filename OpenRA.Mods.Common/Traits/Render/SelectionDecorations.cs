#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public class SelectionDecorationsInfo : ITraitInfo, ISelectionDecorationsInfo
	{
		[PaletteReference] public readonly string Palette = "chrome";

		[Desc("Visual bounds for selection box. If null, it uses AutoSelectionSize.",
		"The first two values define the bounds' size, the optional third and fourth",
		"values specify the position relative to the actors' center. Defaults to selectable bounds.")]
		public readonly int[] VisualBounds = null;

		[Desc("Health bar, production progress bar etc.")]
		public readonly bool RenderSelectionBars = true;

		public readonly bool RenderSelectionBox = true;

		public readonly Color SelectionBoxColor = Color.White;

		public readonly string Image = "pips";

		public object Create(ActorInitializer init) { return new SelectionDecorations(init.Self, this); }

		public int[] SelectionBoxBounds { get { return VisualBounds; } }
	}

	public class SelectionDecorations : IPostRenderSelection, ITick
	{
		// depends on the order of pips in TraitsInterfaces.cs!
		static readonly string[] PipStrings = { "pip-empty", "pip-green", "pip-yellow", "pip-red", "pip-gray", "pip-blue", "pip-ammo", "pip-ammoempty" };

		public readonly SelectionDecorationsInfo Info;

		readonly Actor self;
		readonly Animation pipImages;

		public SelectionDecorations(Actor self, SelectionDecorationsInfo info)
		{
			this.self = self;
			Info = info;

			pipImages = new Animation(self.World, Info.Image);
		}

		IEnumerable<WPos> ActivityTargetPath()
		{
			if (!self.IsInWorld || self.IsDead)
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

		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr)
		{
			if (self.World.FogObscures(self))
				yield break;

			if (Info.RenderSelectionBox)
				yield return new SelectionBoxRenderable(self, Info.SelectionBoxColor);

			if (Info.RenderSelectionBars)
				yield return new SelectionBarsRenderable(self, true, true);

			if (!self.Owner.IsAlliedWith(wr.World.RenderPlayer))
				yield break;

			if (self.World.LocalPlayer != null && self.World.LocalPlayer.PlayerActor.Trait<DeveloperMode>().PathDebug)
				yield return new TargetLineRenderable(ActivityTargetPath(), Color.Green);

			var b = self.VisualBounds;
			var pos = wr.ScreenPxPosition(self.CenterPosition);
			var bl = wr.Viewport.WorldToViewPx(pos + new int2(b.Left, b.Bottom));
			var pal = wr.Palette(Info.Palette);

			foreach (var r in DrawPips(wr, self, bl, pal))
				yield return r;
		}

		IEnumerable<IRenderable> DrawPips(WorldRenderer wr, Actor self, int2 basePosition, PaletteReference palette)
		{
			var pipSources = self.TraitsImplementing<IPips>();
			if (!pipSources.Any())
				yield break;

			pipImages.PlayRepeating(PipStrings[0]);

			var pipSize = pipImages.Image.Size.XY.ToInt2();
			var pipxyBase = basePosition + new int2(1 - pipSize.X / 2, -(3 + pipSize.Y / 2));
			var pipxyOffset = new int2(0, 0);
			var width = self.VisualBounds.Width;

			foreach (var pips in pipSources)
			{
				var thisRow = pips.GetPips(self);
				if (thisRow == null)
					continue;

				foreach (var pip in thisRow)
				{
					if (pipxyOffset.X + pipSize.X >= width)
						pipxyOffset = new int2(0, pipxyOffset.Y - pipSize.Y);

					pipImages.PlayRepeating(PipStrings[(int)pip]);
					pipxyOffset += new int2(pipSize.X, 0);

					yield return new UISpriteRenderable(pipImages.Image, self.CenterPosition, pipxyBase + pipxyOffset, 0, palette, 1f);
				}

				// Increment row
				pipxyOffset = new int2(0, pipxyOffset.Y - (pipSize.Y + 1));
			}
		}

		void ITick.Tick(Actor self)
		{
			pipImages.Tick();
		}
	}
}
