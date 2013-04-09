#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	public class SelectionDecorationsInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new SelectionDecorations(init.self); }
	}

	public class SelectionDecorations : IPostRenderSelection
	{
		// depends on the order of pips in TraitsInterfaces.cs!
		static readonly string[] pipStrings = { "pip-empty", "pip-green", "pip-yellow", "pip-red", "pip-gray", "pip-blue" };
		static readonly string[] tagStrings = { "", "tag-fake", "tag-primary" };

		Actor self;

		public SelectionDecorations(Actor self) { this.self = self; }

		public void RenderAfterWorld(WorldRenderer wr)
		{
			var bounds = self.Bounds.Value;

			var xy = new float2(bounds.Left, bounds.Top);
			var xY = new float2(bounds.Left, bounds.Bottom);

			DrawControlGroup(wr, self, xy);
			DrawPips(wr, self, xY);
			DrawTags(wr, self, new float2(.5f * (bounds.Left + bounds.Right), bounds.Top));
		}

		void DrawControlGroup(WorldRenderer wr, Actor self, float2 basePosition)
		{
			var group = self.World.Selection.GetControlGroupForActor(self);
			if (group == null) return;

			var pipImages = new Animation("pips");
			pipImages.PlayFetchIndex("groups", () => (int)group);
			pipImages.Tick();
			pipImages.Image.DrawAt(wr, basePosition + new float2(-8, 1), "chrome");
		}

		void DrawPips(WorldRenderer wr, Actor self, float2 basePosition)
		{
			if (self.Owner != self.World.RenderPlayer)
				return;

			var pipSources = self.TraitsImplementing<IPips>();
			if (pipSources.Count() == 0)
				return;

			var pipImages = new Animation("pips");
			pipImages.PlayRepeating(pipStrings[0]);

			var pipSize = pipImages.Image.size;
			var pipxyBase = basePosition + new float2(1, -pipSize.Y);
			var pipxyOffset = new float2(0, 0); // Correct for offset due to multiple columns/rows

			foreach (var pips in pipSources)
			{
				var thisRow = pips.GetPips(self);
				if (thisRow == null)
					continue;

				var width = self.Bounds.Value.Width;

				foreach (var pip in thisRow)
				{
					if (pipxyOffset.X + pipSize.X >= width)
					{
						pipxyOffset.X = 0;
						pipxyOffset.Y -= pipSize.Y;
					}
					pipImages.PlayRepeating(pipStrings[(int)pip]);
					pipImages.Image.DrawAt(wr, pipxyBase + pipxyOffset, "chrome");
					pipxyOffset += new float2(pipSize.X, 0);
				}

				// Increment row
				pipxyOffset.X = 0;
				pipxyOffset.Y -= pipSize.Y + 1;
			}
		}

		void DrawTags(WorldRenderer wr, Actor self, float2 basePosition)
		{
			if (self.Owner != self.World.RenderPlayer)
			    return;

			// If a mod wants to implement a unit with multiple tags, then they are placed on multiple rows
			var tagxyBase = basePosition + new float2(-16, 2); // Correct for the offset in the shp file
			var tagxyOffset = new float2(0, 0); // Correct for offset due to multiple rows

			foreach (var tags in self.TraitsImplementing<ITags>())
			{
				foreach (var tag in tags.GetTags())
				{
					if (tag == TagType.None)
						continue;

					var tagImages = new Animation("pips");
					tagImages.PlayRepeating(tagStrings[(int)tag]);
					tagImages.Image.DrawAt(wr, tagxyBase + tagxyOffset, "chrome");

					// Increment row
					tagxyOffset.Y += 8;
				}
			}
		}

	}
}

