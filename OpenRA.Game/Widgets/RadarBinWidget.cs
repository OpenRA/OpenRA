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
using OpenRA.Traits;

namespace OpenRA.Widgets
{
	public class RadarBinWidget : Widget
	{
		static float2 radarOpenOrigin = new float2(Game.viewport.Width - 215, 29);
		static float2 radarClosedOrigin = new float2(Game.viewport.Width - 215, -166);
		public static float2 radarOrigin = radarClosedOrigin;
		float radarMinimapHeight;
		const int radarSlideAnimationLength = 15;
		const int radarActivateAnimationLength = 5;
		int radarAnimationFrame = 0;
		bool radarAnimating = false;
		bool hasRadar = false;

		string radarCollection;

		public override string GetCursor(int2 pos)
		{		
			var mapRect = new RectangleF(radarOrigin.X + 9, radarOrigin.Y + (192 - radarMinimapHeight) / 2,
				192, radarMinimapHeight);
			
			var loc = Game.world.Minimap.MinimapPixelToCell(mapRect, pos);

			var mi = new MouseInput
			{
				Location = loc,
				Button = MouseButton.Right,
				Modifiers = Game.controller.GetModifiers()
			};

			var cursor = Game.controller.orderGenerator.GetCursor( Game.world, loc, mi );
			if (cursor == null)
				return "default";
			
			return SequenceProvider.HasCursorSequence(cursor+"-minimap") ? cursor+"-minimap" : cursor;
		}

		public override bool HandleInputInner(MouseInput mi)
		{
			if (!hasRadar || radarAnimating) return false;	// we're not set up for this.

			var mapRect = new RectangleF(radarOrigin.X + 9, radarOrigin.Y + (192 - radarMinimapHeight) / 2,
				192, radarMinimapHeight);

			if (!mapRect.Contains(mi.Location.ToPointF()))
				return false;

			var loc = Game.world.Minimap.MinimapPixelToCell(mapRect, mi.Location);

			if ((mi.Event == MouseInputEvent.Down || mi.Event == MouseInputEvent.Move) && mi.Button == MouseButton.Left)
				Game.viewport.Center(loc);

			if (mi.Event == MouseInputEvent.Down && mi.Button == MouseButton.Right)
			{
				// fake a mousedown/mouseup here

				var fakemi = new MouseInput
				{
					Event = MouseInputEvent.Down,
					Button = MouseButton.Right,
					Modifiers = mi.Modifiers,
					Location = (loc * Game.CellSize - Game.viewport.Location).ToInt2()
				};

				Game.controller.HandleInput(Game.world, fakemi);

				fakemi.Event = MouseInputEvent.Up;
				Game.controller.HandleInput(Game.world, fakemi);
			}

			return true;
		}

		public override Rectangle EventBounds
		{
			get { return new Rectangle((int)radarOrigin.X + 9, (int)(radarOrigin.Y + (192 - radarMinimapHeight) / 2),
				192, (int)radarMinimapHeight);}
		}

		public override void DrawInner(World world)
		{
			radarCollection = "radar-" + world.LocalPlayer.Country.Race;

			var hasNewRadar = world.Queries.OwnedBy[world.LocalPlayer]
				.WithTrait<ProvidesRadar>()
				.Any(a => a.Trait.IsActive);

			if (hasNewRadar != hasRadar)
				radarAnimating = true;

			hasRadar = hasNewRadar;

			Game.Renderer.RgbaSpriteRenderer.DrawSprite(ChromeProvider.GetImage(radarCollection, "left"), radarOrigin, "chrome");
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(ChromeProvider.GetImage(radarCollection, "right"), radarOrigin + new float2(201, 0), "chrome");
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(ChromeProvider.GetImage(radarCollection, "bottom"), radarOrigin + new float2(0, 192), "chrome");

			if (radarAnimating)
				Game.Renderer.RgbaSpriteRenderer.DrawSprite(ChromeProvider.GetImage(radarCollection, "bg"), radarOrigin + new float2(9, 0), "chrome");

			Game.Renderer.RgbaSpriteRenderer.Flush();

			if (radarAnimationFrame >= radarSlideAnimationLength)
			{
				var mapRect = new RectangleF(radarOrigin.X + 9, radarOrigin.Y + (192 - radarMinimapHeight) / 2, 192, radarMinimapHeight);
				world.Minimap.Draw(mapRect);
			}
		}

		public override void Tick(World world)
		{
			if (world.LocalPlayer != null)
				world.Minimap.Update();

			if (!radarAnimating)
				return;

			// Increment frame
			if (hasRadar)
				radarAnimationFrame++;
			else
				radarAnimationFrame--;

			// Calculate radar bin position
			if (radarAnimationFrame <= radarSlideAnimationLength)
				radarOrigin = float2.Lerp(radarClosedOrigin, radarOpenOrigin, radarAnimationFrame * 1.0f / radarSlideAnimationLength);

			var eva = Rules.Info["world"].Traits.Get<EvaAlertsInfo>();

			// Play radar-on sound at the start of the activate anim (open)
			if (radarAnimationFrame == radarSlideAnimationLength && hasRadar)
				Sound.Play(eva.RadarUp);

			// Play radar-on sound at the start of the activate anim (close)
			if (radarAnimationFrame == radarSlideAnimationLength + radarActivateAnimationLength - 1 && !hasRadar)
				Sound.Play(eva.RadarDown);

			// Minimap height
			if (radarAnimationFrame >= radarSlideAnimationLength)
				radarMinimapHeight = float2.Lerp(0, 192, (radarAnimationFrame - radarSlideAnimationLength) * 1.0f / radarActivateAnimationLength);

			// Animation is complete
			if (radarAnimationFrame == (hasRadar ? radarSlideAnimationLength + radarActivateAnimationLength : 0))
				radarAnimating = false;
		}
	}
}
