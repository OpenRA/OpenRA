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
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	public class SelectionDecorationsInfo : ITraitInfo
	{
		public readonly string Palette = "chrome";

		public object Create(ActorInitializer init) { return new SelectionDecorations(init.self, this); }
	}

	public class SelectionDecorations : IPostRenderSelection
	{
		// depends on the order of pips in TraitsInterfaces.cs!
		static readonly string[] pipStrings = { "pip-empty", "pip-green", "pip-yellow", "pip-red", "pip-gray", "pip-blue", "pip-ammo", "pip-ammoempty" };
		static readonly string[] tagStrings = { "", "tag-fake", "tag-primary" };

		public SelectionDecorationsInfo Info;
		Actor self;

		public SelectionDecorations(Actor self, SelectionDecorationsInfo info)
		{
			this.self = self;
			Info = info;
		}

		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr)
		{
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer) && self.World.FogObscures(self))
				yield break;

			var b = self.Bounds.Value;
			var pos = wr.ScreenPxPosition(self.CenterPosition);
			var tl = wr.Viewport.WorldToViewPx(pos + new int2(b.Left, b.Top));
			var bl = wr.Viewport.WorldToViewPx(pos + new int2(b.Left, b.Bottom));
			var tm = wr.Viewport.WorldToViewPx(pos + new int2((b.Left + b.Right) / 2, b.Top));

			foreach (var r in DrawControlGroup(wr, self, tl))
				yield return r;

			foreach (var r in DrawPips(wr, self, bl))
				yield return r;

			foreach (var r in DrawTags(wr, self, tm))
				yield return r;
		}

		IEnumerable<IRenderable> DrawControlGroup(WorldRenderer wr, Actor self, int2 basePosition)
		{
			var group = self.World.Selection.GetControlGroupForActor(self);
			if (group == null)
				yield break;

			var pipImages = new Animation(self.World, "pips");
			var pal = wr.Palette(Info.Palette);
			pipImages.PlayFetchIndex("groups", () => (int)group);
			pipImages.Tick();

			var pos = basePosition - (0.5f * pipImages.Image.size).ToInt2() + new int2(9, 5);
			yield return new UISpriteRenderable(pipImages.Image, pos, 0, pal, 1f);
		}

		IEnumerable<IRenderable> DrawPips(WorldRenderer wr, Actor self, int2 basePosition)
		{
			var pipSources = self.TraitsImplementing<IPips>();
			if (!pipSources.Any())
				yield break;

			var pipImages = new Animation(self.World, "pips");
			pipImages.PlayRepeating(pipStrings[0]);

			var pipSize = pipImages.Image.size.ToInt2();
			var pipxyBase = basePosition + new int2(1 - pipSize.X / 2, - (3 + pipSize.Y / 2));
			var pipxyOffset = new int2(0, 0);
			var pal = wr.Palette(Info.Palette);
			var width = self.Bounds.Value.Width;

			foreach (var pips in pipSources)
			{
				var thisRow = pips.GetPips(self);
				if (thisRow == null)
					continue;

				foreach (var pip in thisRow)
				{
					if (pipxyOffset.X + pipSize.X >= width)
					{
						pipxyOffset.X = 0;
						pipxyOffset.Y -= pipSize.Y;
					}

					pipImages.PlayRepeating(pipStrings[(int)pip]);
					pipxyOffset += new int2(pipSize.X, 0);

					yield return new UISpriteRenderable(pipImages.Image, pipxyBase + pipxyOffset, 0, pal, 1f);
				}

				// Increment row
				pipxyOffset.X = 0;
				pipxyOffset.Y -= pipSize.Y + 1;
			}
		}

		IEnumerable<IRenderable> DrawTags(WorldRenderer wr, Actor self, int2 basePosition)
		{
			var tagImages = new Animation(self.World, "pips");
			var pal = wr.Palette(Info.Palette);
			var tagxyOffset = new int2(0, 6);

			foreach (var tags in self.TraitsImplementing<ITags>())
			{
				foreach (var tag in tags.GetTags())
				{
					if (tag == TagType.None)
						continue;

					tagImages.PlayRepeating(tagStrings[(int)tag]);
					var pos = basePosition + tagxyOffset - (0.5f * tagImages.Image.size).ToInt2();
					yield return new UISpriteRenderable(tagImages.Image, pos, 0, pal, 1f);

					// Increment row
					tagxyOffset.Y += 8;
				}
			}
		}

	}
}

