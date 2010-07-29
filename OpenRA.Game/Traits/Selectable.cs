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
using OpenRA.Graphics;
using System.Linq;

namespace OpenRA.Traits
{
	public class SelectableInfo : TraitInfo<Selectable>
	{
		public readonly int Priority = 10;
		public readonly int[] Bounds = null;
		[VoiceReference]
		public readonly string Voice = null;
		public readonly float Radius = 10;
	}

	public class Selectable : IPostRenderSelection
	{
		// depends on the order of pips in TraitsInterfaces.cs!
		static readonly string[] pipStrings = { "pip-empty", "pip-green", "pip-yellow", "pip-red", "pip-gray" };
		static readonly string[] tagStrings = { "", "tag-fake", "tag-primary" };
		
		public void RenderAfterWorld (Actor self)
		{
			var bounds = self.GetBounds(true);

			var xy = new float2(bounds.Left, bounds.Top);
			var Xy = new float2(bounds.Right, bounds.Top);
			var xY = new float2(bounds.Left, bounds.Bottom);
			var XY = new float2(bounds.Right, bounds.Bottom);

			DrawSelectionBox(self, xy, Xy, xY, XY, Color.White);
			DrawHealthBar(self, xy, Xy);
			DrawControlGroup(self, xy);
			DrawPips(self, xY);
			DrawTags(self, new float2(.5f * (bounds.Left + bounds.Right), bounds.Top));
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
			var health = self.traits.GetOrDefault<Health>();
			if (self.IsDead() || health == null)
				return;
			
			var c = Color.Gray;
			Game.Renderer.LineRenderer.DrawLine(xy + new float2(0, -2), xy + new float2(0, -4), c, c);
			Game.Renderer.LineRenderer.DrawLine(Xy + new float2(0, -2), Xy + new float2(0, -4), c, c);

			var healthColor = (health.ExtendedDamageState == ExtendedDamageState.Quarter) ? Color.Red :
							  (health.ExtendedDamageState == ExtendedDamageState.Half) ? Color.Yellow : Color.LimeGreen;
				
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

		void DrawControlGroup(Actor self, float2 basePosition)
		{
			var group = self.World.Selection.GetControlGroupForActor(self);
			if (group == null) return;

			var pipImages = new Animation("pips");
			pipImages.PlayFetchIndex("groups", () => (int)group);
			pipImages.Tick();
			Game.Renderer.SpriteRenderer.DrawSprite(pipImages.Image, basePosition + new float2(-8, 1), "chrome");
		}

		void DrawPips(Actor self, float2 basePosition)
		{
			if (self.Owner != self.World.LocalPlayer) return;
			
			// If a mod wants to implement a unit with multiple pip sources, then they are placed on multiple rows
			var pipxyBase = basePosition + new float2(-12, -7); // Correct for the offset in the shp file
			var pipxyOffset = new float2(0, 0); // Correct for offset due to multiple columns/rows

			foreach (var pips in self.traits.WithInterface<IPips>())
			{
				foreach (var pip in pips.GetPips(self))
				{
					if (pipxyOffset.X+5 > self.GetBounds(false).Width)
					{
						pipxyOffset.X = 0;
						pipxyOffset.Y -= 4;
					}
					var pipImages = new Animation("pips");
					pipImages.PlayRepeating(pipStrings[(int)pip]);
					Game.Renderer.SpriteRenderer.DrawSprite(pipImages.Image, pipxyBase + pipxyOffset, "chrome");
					pipxyOffset += new float2(4, 0);
				}
				// Increment row
				pipxyOffset.X = 0;
				pipxyOffset.Y -= 5;
			}
		}
		
		void DrawTags(Actor self, float2 basePosition)
		{
			if (self.Owner != self.World.LocalPlayer) return;
			
			// If a mod wants to implement a unit with multiple tags, then they are placed on multiple rows
			var tagxyBase = basePosition + new float2(-16, 2); // Correct for the offset in the shp file
			var tagxyOffset = new float2(0, 0); // Correct for offset due to multiple rows

			foreach (var tags in self.traits.WithInterface<ITags>())
			{
				foreach (var tag in tags.GetTags())
				{
					if (tag == TagType.None)
						continue;
						
					var tagImages = new Animation("pips");
					tagImages.PlayRepeating(tagStrings[(int)tag]);
					Game.Renderer.SpriteRenderer.DrawSprite(tagImages.Image, tagxyBase + tagxyOffset, "chrome");
					
					// Increment row
					tagxyOffset.Y += 8;
				}
			}
		}
		
		void DrawUnitPath(Actor self)
		{
			if (!Game.world.LocalPlayer.PlayerActor.traits.Get<DeveloperMode>().PathDebug) return;
			
			var mobile = self.traits.WithInterface<IMove>().FirstOrDefault();
			if (mobile != null)
			{
				var unit = self.traits.Get<Unit>();
				var alt = (unit != null)? new float2(0, -unit.Altitude) : float2.Zero;
				var path = mobile.GetCurrentPath(self);
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
