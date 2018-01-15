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

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public class SelectionDecorationsInfo : ITraitInfo, Requires<IDecorationBoundsInfo>
	{
		[PaletteReference] public readonly string Palette = "chrome";

		[Desc("Health bar, production progress bar etc.")]
		public readonly bool RenderSelectionBars = true;

		public readonly bool RenderSelectionBox = true;

		public readonly Color SelectionBoxColor = Color.White;

		public readonly string Image = "pips";

		public object Create(ActorInitializer init) { return new SelectionDecorations(init.Self, this); }
	}

	public class SelectionDecorations : IRenderAboveShroud, INotifyCreated, ITick
	{
		// depends on the order of pips in TraitsInterfaces.cs!
		static readonly string[] PipStrings = { "pip-empty", "pip-green", "pip-yellow", "pip-red", "pip-gray", "pip-blue", "pip-ammo", "pip-ammoempty" };

		public readonly SelectionDecorationsInfo Info;

		readonly IDecorationBounds[] decorationBounds;
		readonly Animation pipImages;
		IPips[] pipSources;

		public SelectionDecorations(Actor self, SelectionDecorationsInfo info)
		{
			Info = info;

			decorationBounds = self.TraitsImplementing<IDecorationBounds>().ToArray();
			pipImages = new Animation(self.World, Info.Image);
		}

		void INotifyCreated.Created(Actor self)
		{
			pipSources = self.TraitsImplementing<IPips>().ToArray();
		}

		IEnumerable<WPos> ActivityTargetPath(Actor self)
		{
			if (!self.IsInWorld || self.IsDead)
				yield break;

			var activity = self.CurrentActivity;
			if (activity != null)
			{
				var targets = activity.GetTargets(self);
				yield return self.CenterPosition;

				foreach (var t in targets.Where(t => t.Type != TargetType.Invalid))
					yield return t.CenterPosition;
			}
		}

		IEnumerable<IRenderable> IRenderAboveShroud.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			if (self.World.FogObscures(self))
				return Enumerable.Empty<IRenderable>();

			return DrawDecorations(self, wr);
		}

		bool IRenderAboveShroud.SpatiallyPartitionable { get { return true; } }

		IEnumerable<IRenderable> DrawDecorations(Actor self, WorldRenderer wr)
		{
			var selected = self.World.Selection.Contains(self);
			var regularWorld = self.World.Type == WorldType.Regular;
			var statusBars = Game.Settings.Game.StatusBars;
			var bounds = decorationBounds.Select(b => b.DecorationBounds(self, wr)).FirstOrDefault(b => !b.IsEmpty);

			// Health bars are shown when:
			//  * actor is selected
			//  * status bar preference is set to "always show"
			//  * status bar preference is set to "when damaged" and actor is damaged
			var displayHealth = selected || (regularWorld && statusBars == StatusBarsType.AlwaysShow)
				|| (regularWorld && statusBars == StatusBarsType.DamageShow && self.GetDamageState() != DamageState.Undamaged);

			// Extra bars are shown when:
			//  * actor is selected
			//  * status bar preference is set to "always show"
			//  * status bar preference is set to "when damaged"
			var displayExtra = selected || (regularWorld && statusBars != StatusBarsType.Standard);

			if (Info.RenderSelectionBox && selected)
				yield return new SelectionBoxRenderable(self, bounds, Info.SelectionBoxColor);

			if (Info.RenderSelectionBars && (displayHealth || displayExtra))
				yield return new SelectionBarsRenderable(self, bounds, displayHealth, displayExtra);

			// Target lines and pips are always only displayed for selected allied actors
			if (!selected || !self.Owner.IsAlliedWith(wr.World.RenderPlayer))
				yield break;

			if (self.World.LocalPlayer != null && self.World.LocalPlayer.PlayerActor.Trait<DeveloperMode>().PathDebug)
				yield return new TargetLineRenderable(ActivityTargetPath(self), Color.Green);

			foreach (var r in DrawPips(self, bounds, wr))
				yield return r;
		}

		IEnumerable<IRenderable> DrawPips(Actor self, Rectangle bounds, WorldRenderer wr)
		{
			if (pipSources.Length == 0)
				return Enumerable.Empty<IRenderable>();

			return DrawPipsInner(self, bounds, wr);
		}

		IEnumerable<IRenderable> DrawPipsInner(Actor self, Rectangle bounds, WorldRenderer wr)
		{
			pipImages.PlayRepeating(PipStrings[0]);

			var palette = wr.Palette(Info.Palette);
			var basePosition = wr.Viewport.WorldToViewPx(new int2(bounds.Left, bounds.Bottom));
			var pipSize = pipImages.Image.Size.XY.ToInt2();
			var pipxyBase = basePosition + new int2(1 - pipSize.X / 2, -(3 + pipSize.Y / 2));
			var pipxyOffset = new int2(0, 0);
			var width = bounds.Width;

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
