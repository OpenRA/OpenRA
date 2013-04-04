#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	public class RadarBinWidget : Widget
	{
		public string WorldInteractionController = null;

		static float2 radarOpenOrigin = new float2(Game.viewport.Width - 215, 29);
		static float2 radarClosedOrigin = new float2(Game.viewport.Width - 215, -166);
		float2 radarOrigin = radarClosedOrigin;
		float radarMinimapHeight;
		const int radarSlideAnimationLength = 15;
		const int radarActivateAnimationLength = 5;
		int radarAnimationFrame = 0;
		bool radarAnimating = false;
		bool hasRadar = false;
		string radarCollection;

		float previewScale = 0;
		RectangleF mapRect = Rectangle.Empty;
		int2 previewOrigin;

		Sprite terrainSprite;
		Sprite customTerrainSprite;
		Sprite actorSprite;
		Sprite shroudSprite;

		/* hack to expose this to other broken widgets which rely on it */
		public float2 RadarOrigin { get { return radarOrigin; } }

		readonly World world;

		[ObjectCreator.UseCtor]
		public RadarBinWidget(World world)
		{
			this.world = world;
			var size = Math.Max(world.Map.Bounds.Width, world.Map.Bounds.Height);
			previewScale = Math.Min(192f / world.Map.Bounds.Width, 192f / world.Map.Bounds.Height);
			previewOrigin = new int2(9 + (int)(radarOpenOrigin.X + previewScale * (size - world.Map.Bounds.Width)/2), (int)(radarOpenOrigin.Y + previewScale * (size - world.Map.Bounds.Height)/2));
			mapRect = new RectangleF(previewOrigin.X, previewOrigin.Y, (int)(world.Map.Bounds.Width * previewScale), (int)(world.Map.Bounds.Height * previewScale));

			// Only needs to be done once
			var terrainBitmap = Minimap.TerrainBitmap(world.Map);
			var r = new Rectangle( 0, 0, world.Map.Bounds.Width, world.Map.Bounds.Height );
			var s = new Size( terrainBitmap.Width, terrainBitmap.Height );
			terrainSprite = new Sprite(new Sheet(s), r, TextureChannel.Alpha);
			terrainSprite.sheet.Texture.SetData(terrainBitmap);

			// Data is set in Tick()
			customTerrainSprite = new Sprite(new Sheet(s), r, TextureChannel.Alpha);
			actorSprite = new Sprite(new Sheet(s), r, TextureChannel.Alpha);
			shroudSprite = new Sprite(new Sheet(s), r, TextureChannel.Alpha);
		}

		public override string GetCursor(int2 pos)
		{
			if (world == null || !hasRadar)
				return null;

			var loc = MinimapPixelToCell(pos);

			var mi = new MouseInput
			{
				Location = loc.ToInt2(),
				Button = MouseButton.Right,
				Modifiers = Game.GetModifierKeys()
			};

			var cursor = world.OrderGenerator.GetCursor( world, loc, mi );
			if (cursor == null)
				return "default";

			return CursorProvider.HasCursorSequence(cursor+"-minimap") ? cursor+"-minimap" : cursor;
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (!hasRadar || radarAnimating) return false;	// we're not set up for this.

			if (!mapRect.Contains(mi.Location))
				return false;

			var loc = MinimapPixelToCell(mi.Location);
			if ((mi.Event == MouseInputEvent.Down || mi.Event == MouseInputEvent.Move) && mi.Button == MouseButton.Left)
				Game.viewport.Center(loc.ToFloat2());

			if (mi.Event == MouseInputEvent.Down && mi.Button == MouseButton.Right)
			{
				// fake a mousedown/mouseup here
				var fakemi = new MouseInput
				{
					Event = MouseInputEvent.Down,
					Button = MouseButton.Right,
					Modifiers = mi.Modifiers,
					Location = (((loc.ToPPos().ToFloat2()) - Game.viewport.Location) * Game.viewport.Zoom).ToInt2()
				};

				if (WorldInteractionController != null)
				{
					var controller = Ui.Root.Get<WorldInteractionControllerWidget>(WorldInteractionController);
					controller.HandleMouseInput(fakemi);
					fakemi.Event = MouseInputEvent.Up;
					controller.HandleMouseInput(fakemi);
				}
			}

			return true;
		}

		public override Rectangle EventBounds
		{
			get { return new Rectangle((int)mapRect.X, (int)mapRect.Y, (int)mapRect.Width, (int)mapRect.Height);}
		}

		public override void Draw()
		{
			if (world == null || world.ObserverMode) return;
			if (world.LocalPlayer.WinState != WinState.Undefined) return;

			radarCollection = "radar-" + world.LocalPlayer.Country.Race;
			var rsr = Game.Renderer.RgbaSpriteRenderer;
			rsr.DrawSprite(ChromeProvider.GetImage(radarCollection, "left"), radarOrigin);
			rsr.DrawSprite(ChromeProvider.GetImage(radarCollection, "right"), radarOrigin + new float2(201, 0));
			rsr.DrawSprite(ChromeProvider.GetImage(radarCollection, "bottom"), radarOrigin + new float2(0, 192));
			rsr.DrawSprite(ChromeProvider.GetImage(radarCollection, "bg"), radarOrigin + new float2(9, 0));

			// Don't draw the radar if the tray is moving
			if (radarAnimationFrame >= radarSlideAnimationLength)
			{
				var o = new float2(mapRect.Location.X, mapRect.Location.Y + world.Map.Bounds.Height * previewScale * (1 - radarMinimapHeight)/2);
				var s = new float2(mapRect.Size.Width, mapRect.Size.Height*radarMinimapHeight);
				rsr.DrawSprite(terrainSprite, o, s);
				rsr.DrawSprite(customTerrainSprite, o, s);
				rsr.DrawSprite(actorSprite, o, s);
				rsr.DrawSprite(shroudSprite, o, s);

				// Draw viewport rect
				if (radarAnimationFrame == radarSlideAnimationLength + radarActivateAnimationLength)
				{
					var wr = Game.viewport.WorldRect;
					var wro = new CPos(wr.X, wr.Y);
					var tl = CellToMinimapPixel(wro);
					var br = CellToMinimapPixel(wro + new CVec(wr.Width, wr.Height));

					Game.Renderer.EnableScissor((int)mapRect.Left, (int)mapRect.Top, (int)mapRect.Width, (int)mapRect.Height);
					Game.Renderer.LineRenderer.DrawRect(tl, br, Color.White);
					Game.Renderer.DisableScissor();
				}
			}
		}

		int updateTicks = 0;
		public override void Tick()
		{
			var hasRadarNew = world
				.ActorsWithTrait<ProvidesRadar>()
				.Any(a => a.Actor.Owner == world.LocalPlayer && a.Trait.IsActive);

			if (hasRadarNew != hasRadar)
			{
				radarAnimating = true;
				Sound.PlayNotification(null, "Sounds", (hasRadarNew ? "RadarUp" : "RadarDown"), null);
			}

			hasRadar = hasRadarNew;

			// Build the radar image
			if (hasRadar)
			{
				--updateTicks;
				if (updateTicks <= 0)
				{
					updateTicks = 12;
					customTerrainSprite.sheet.Texture.SetData(Minimap.CustomTerrainBitmap(world));
				}

				if (updateTicks == 8)
					actorSprite.sheet.Texture.SetData(Minimap.ActorsBitmap(world));

				if (updateTicks == 4)
					shroudSprite.sheet.Texture.SetData(Minimap.ShroudBitmap(world));
			}

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

			// Minimap height
			if (radarAnimationFrame >= radarSlideAnimationLength)
				radarMinimapHeight = float2.Lerp(0, 1, (radarAnimationFrame - radarSlideAnimationLength) * 1.0f / radarActivateAnimationLength);

			// Animation is complete
			if (radarAnimationFrame == (hasRadar ? radarSlideAnimationLength + radarActivateAnimationLength : 0))
				radarAnimating = false;
		}

		int2 CellToMinimapPixel(CPos p)
		{
			return new int2((int)(mapRect.X +previewScale*(p.X - world.Map.Bounds.Left)), (int)(mapRect.Y + previewScale*(p.Y - world.Map.Bounds.Top)));
		}

		CPos MinimapPixelToCell(int2 p)
		{
			return new CPos(world.Map.Bounds.Left + (int)((p.X - mapRect.X) / previewScale), world.Map.Bounds.Top + (int)((p.Y - mapRect.Y) / previewScale));
		}
	}
}
