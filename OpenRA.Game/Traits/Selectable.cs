#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	public class SelectableInfo : TraitInfo<Selectable>
	{
		public readonly int Priority = 10;
		public readonly int[] Bounds = null;
		[VoiceReference]
		public readonly string Voice = null;
	}

	public class Selectable : IPostRenderSelection
	{
		// depends on the order of pips in TraitsInterfaces.cs!
		static readonly string[] pipStrings = { "pip-empty", "pip-green", "pip-yellow", "pip-red", "pip-gray" };
		static readonly string[] tagStrings = { "", "tag-fake", "tag-primary" };
		
		public void RenderAfterWorld (WorldRenderer wr, Actor self)
		{
			var bounds = self.GetBounds(true);
			Color selectionColor = Color.White;

			var xy = new float2(bounds.Left, bounds.Top);
			var Xy = new float2(bounds.Right, bounds.Top);
			var xY = new float2(bounds.Left, bounds.Bottom);
			var XY = new float2(bounds.Right, bounds.Bottom);

			var colorResults = self.TraitsImplementing<ISelectionColorModifier>().Select(t => t.GetSelectionColorModifier(self, selectionColor)).Where(
					c => c.ToArgb() != selectionColor.ToArgb());

			if (colorResults.Any())
				selectionColor = colorResults.First();

			DrawSelectionBox(self, xy, Xy, xY, XY, selectionColor);
			DrawHealthBar(self, xy, Xy);
			DrawControlGroup(wr, self, xy);
			DrawPips(wr, self, xY);
			DrawTags(wr, self, new float2(.5f * (bounds.Left + bounds.Right), bounds.Top));
			DrawUnitPath(self);
		}
		
		void DrawSelectionBox(Actor self, float2 xy, float2 Xy, float2 xY, float2 XY, Color c)
		{
			Game.Renderer.LineRenderer.DrawLine(xy, xy + new float2(4, 0), c, c);
			Game.Renderer.LineRenderer.DrawLine(xy, xy + new float2(0, 4), c, c);
			Game.Renderer.LineRenderer.DrawLine(Xy, Xy + new float2(-4, 0), c, c);
			Game.Renderer.LineRenderer.DrawLine(Xy, Xy + new float2(0, 4), c, c);

			Game.Renderer.LineRenderer.DrawLine(xY, xY + new float2(4, 0), c, c);
			Game.Renderer.LineRenderer.DrawLine(xY, xY + new float2(0, -4), c, c);
			Game.Renderer.LineRenderer.DrawLine(XY, XY + new float2(-4, 0), c, c);
			Game.Renderer.LineRenderer.DrawLine(XY, XY + new float2(0, -4), c, c);
		}
		
		void DrawHealthBar(Actor self, float2 xy, float2 Xy)
		{
			if (!self.IsInWorld) return;
			
			var health = self.TraitOrDefault<Health>();
			if (health == null || health.IsDead) return;
			
			var c = Color.Gray;
			Game.Renderer.LineRenderer.DrawLine(xy + new float2(0, -2), xy + new float2(0, -4), c, c);
			Game.Renderer.LineRenderer.DrawLine(Xy + new float2(0, -2), Xy + new float2(0, -4), c, c);

			var healthColor = (health.DamageState == DamageState.Critical) ? Color.Red :
							  (health.DamageState == DamageState.Heavy) ? Color.Yellow : Color.LimeGreen;
				
			var healthColor2 = Color.FromArgb(
				255,
				healthColor.R / 2,
				healthColor.G / 2,
				healthColor.B / 2);

			var z = float2.Lerp(xy, Xy, health.HPFraction);

			Game.Renderer.LineRenderer.DrawLine(z + new float2(0, -4), Xy + new float2(0, -4), c, c);
			Game.Renderer.LineRenderer.DrawLine(z + new float2(0, -2), Xy + new float2(0, -2), c, c);

			Game.Renderer.LineRenderer.DrawLine(xy + new float2(0, -3), z + new float2(0, -3), healthColor, healthColor);
			Game.Renderer.LineRenderer.DrawLine(xy + new float2(0, -2), z + new float2(0, -2), healthColor2, healthColor2);
			Game.Renderer.LineRenderer.DrawLine(xy + new float2(0, -4), z + new float2(0, -4), healthColor2, healthColor2);
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
			if (self.Owner != self.World.LocalPlayer) return;
			
			// If a mod wants to implement a unit with multiple pip sources, then they are placed on multiple rows
			var pipxyBase = basePosition + new float2(-12, -7); // Correct for the offset in the shp file
			var pipxyOffset = new float2(0, 0); // Correct for offset due to multiple columns/rows

			foreach (var pips in self.TraitsImplementing<IPips>())
			{
				var thisRow = pips.GetPips(self);
				if (thisRow == null)
					continue;

				foreach (var pip in thisRow)
				{
					if (pipxyOffset.X+5 > self.GetBounds(false).Width)
					{
						pipxyOffset.X = 0;
						pipxyOffset.Y -= 4;
					}
					var pipImages = new Animation("pips");
					pipImages.PlayRepeating(pipStrings[(int)pip]);
					pipImages.Image.DrawAt(wr, pipxyBase + pipxyOffset, "chrome");
					pipxyOffset += new float2(4, 0);
				}
				// Increment row
				pipxyOffset.X = 0;
				pipxyOffset.Y -= 5;
			}
		}
		
		void DrawTags(WorldRenderer wr, Actor self, float2 basePosition)
		{
			if (self.Owner != self.World.LocalPlayer) return;
			
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
		
		void DrawUnitPath(Actor self)
		{
			if (self.World.LocalPlayer == null ||!self.World.LocalPlayer.PlayerActor.Trait<DeveloperMode>().PathDebug) return;

			var activity = self.GetCurrentActivity();
			var mobile = self.TraitOrDefault<IMove>();
			if (activity != null && mobile != null)
			{
				var alt = new float2(0, -mobile.Altitude);
				var path = activity.GetCurrentPath();
				var start = self.CenterLocation + alt;

				var c = Color.Green;

				foreach (var step in path)
				{
					var stp = step + alt;
					Game.Renderer.LineRenderer.DrawLine(stp + new float2(-1, -1), stp + new float2(-1, 1), c, c);
					Game.Renderer.LineRenderer.DrawLine(stp + new float2(-1, 1), stp + new float2(1, 1), c, c);
					Game.Renderer.LineRenderer.DrawLine(stp + new float2(1, 1), stp + new float2(1, -1), c, c);
					Game.Renderer.LineRenderer.DrawLine(stp + new float2(1, -1), stp + new float2(-1, -1), c, c);
					Game.Renderer.LineRenderer.DrawLine(start, stp, c, c);
					start = stp;
				}
			}
		}
		
	}
}
