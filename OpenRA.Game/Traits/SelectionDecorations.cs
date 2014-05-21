#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

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

		public void RenderAfterWorld(WorldRenderer wr)
		{
			if (self.World.FogObscures(self))
				return;

			var pos = wr.ScreenPxPosition(self.CenterPosition);
			var bounds = self.Bounds.Value;
			bounds.Offset(pos.X, pos.Y);

			var xy = new int2(bounds.Left, bounds.Top);
			var xY = new int2(bounds.Left, bounds.Bottom);

			DrawControlGroup(wr, self, xy);
			DrawPips(wr, self, xY);
			DrawTags(wr, self, new int2((bounds.Left + bounds.Right) / 2, bounds.Top));
		}

		void DrawControlGroup(WorldRenderer wr, Actor self, int2 basePosition)
		{
			var group = self.World.Selection.GetControlGroupForActor(self);
			if (group == null) return;

			var pipImages = new Animation(self.World, "pips");
			var pal = wr.Palette(Info.Palette);
			pipImages.PlayFetchIndex("groups", () => (int)group);
			pipImages.Tick();

			var pos = wr.Viewport.WorldToViewPx(basePosition) - (0.5f * pipImages.Image.size).ToInt2() + new int2(9, 5);
			Game.Renderer.SpriteRenderer.DrawSprite(pipImages.Image, pos, pal);
		}

		void DrawPips(WorldRenderer wr, Actor self, int2 basePosition)
		{
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				return;

			var pipSources = self.TraitsImplementing<IPips>();
			if (!pipSources.Any())
				return;

			var pipImages = new Animation(self.World, "pips");
			pipImages.PlayRepeating(pipStrings[0]);

			var pipSize = pipImages.Image.size.ToInt2();
			var pipxyBase = wr.Viewport.WorldToViewPx(basePosition) + new int2(1 - pipSize.X / 2, - (3 + pipSize.Y / 2));
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

					Game.Renderer.SpriteRenderer.DrawSprite(pipImages.Image, pipxyBase + pipxyOffset, pal);
				}

				// Increment row
				pipxyOffset.X = 0;
				pipxyOffset.Y -= pipSize.Y + 1;
			}
		}

		void DrawTags(WorldRenderer wr, Actor self, int2 basePosition)
		{
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				return;

			var tagImages = new Animation(self.World, "pips");
			var pal = wr.Palette(Info.Palette);
			var tagxyOffset = new int2(0, 6);
			var tagBase = wr.Viewport.WorldToViewPx(basePosition);

			foreach (var tags in self.TraitsImplementing<ITags>())
			{
				foreach (var tag in tags.GetTags())
				{
					if (tag == TagType.None)
						continue;

					tagImages.PlayRepeating(tagStrings[(int)tag]);
					var pos = tagBase + tagxyOffset - (0.5f * tagImages.Image.size).ToInt2();
					Game.Renderer.SpriteRenderer.DrawSprite(tagImages.Image, pos, pal);

					// Increment row
					tagxyOffset.Y += 8;
				}
			}
		}

	}
}

