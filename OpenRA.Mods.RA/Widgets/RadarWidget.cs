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
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	public class RadarWidget : Widget
	{
		public string WorldInteractionController = null;
		public int AnimationLength = 5;
		public string RadarOnlineSound = null;
		public string RadarOfflineSound = null;
		public Func<bool> IsEnabled = () => true;
		public Action AfterOpen = () => {};
		public Action AfterClose = () => {};

		float radarMinimapHeight;
		int frame;
		bool hasRadar;
		bool cachedEnabled;
		int updateTicks;

		float previewScale = 0;
		int2 previewOrigin = int2.Zero;
		Rectangle mapRect = Rectangle.Empty;

		Sprite terrainSprite;
		Sprite customTerrainSprite;
		Sprite actorSprite;
		Sprite shroudSprite;

		readonly World world;

		[ObjectCreator.UseCtor]
		public RadarWidget(World world)
		{
			this.world = world;
		}

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			var width = world.Map.Bounds.Width;
			var height = world.Map.Bounds.Height;
			var size = Math.Max(width, height);
			var rb = RenderBounds;

			previewScale = Math.Min(rb.Width * 1f / width, rb.Height * 1f / height);
			previewOrigin = new int2((int)(previewScale*(size - width)/2), (int)(previewScale*(size - height)/2));
			mapRect = new Rectangle(previewOrigin.X, previewOrigin.Y, (int)(previewScale*width), (int)(previewScale*height));

			// Only needs to be done once
			var terrainBitmap = Minimap.TerrainBitmap(world.Map);
			var r = new Rectangle(0, 0, width, height);
			var s = new Size(terrainBitmap.Width, terrainBitmap.Height);
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

			var cursor = world.OrderGenerator.GetCursor(world, loc, mi);
			if (cursor == null)
				return "default";

			return CursorProvider.HasCursorSequence(cursor+"-minimap") ? cursor+"-minimap" : cursor;
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (!mapRect.Contains(mi.Location))
				return false;

			if (!hasRadar)
				return true;

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
					Location = (loc.ToPPos().ToFloat2() - Game.viewport.Location).ToInt2()
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

		public override void Draw()
		{
			if (world == null)
				return;

			var o = new float2(mapRect.Location.X, mapRect.Location.Y + world.Map.Bounds.Height * previewScale * (1 - radarMinimapHeight)/2);
			var s = new float2(mapRect.Size.Width, mapRect.Size.Height*radarMinimapHeight);

			var rsr = Game.Renderer.RgbaSpriteRenderer;
			rsr.DrawSprite(terrainSprite, o, s);
			rsr.DrawSprite(customTerrainSprite, o, s);
			rsr.DrawSprite(actorSprite, o, s);
			rsr.DrawSprite(shroudSprite, o, s);

			// Draw viewport rect
			if (hasRadar)
			{
				var wr = Game.viewport.WorldRect;
				var wro = new CPos(wr.X, wr.Y);
				var tl = CellToMinimapPixel(wro);
				var br = CellToMinimapPixel(wro + new CVec(wr.Width, wr.Height));

				Game.Renderer.EnableScissor(mapRect.Left, mapRect.Top, mapRect.Width, mapRect.Height);
				Game.Renderer.LineRenderer.DrawRect(tl, br, Color.White);
				Game.Renderer.DisableScissor();
			}
		}

		public override void Tick()
		{
			// Update the radar animation even when its closed
			// This avoids obviously stale data from being shown when first opened.
			// TODO: This delayed updating is a giant hack
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

			// Enable/Disable the radar
			var enabled = IsEnabled();
			if (enabled != cachedEnabled)
				Sound.Play(enabled ? RadarOnlineSound : RadarOfflineSound);
			cachedEnabled = enabled;

			var targetFrame = enabled ? AnimationLength : 0;
			hasRadar = enabled && frame == AnimationLength;
			if (frame == targetFrame)
				return;

			frame += enabled ? 1 : -1;
			radarMinimapHeight = float2.Lerp(0, 1, (float)frame / AnimationLength);

			// Update map rectangle for event handling
			var ro = RenderOrigin;
			mapRect = new Rectangle(previewOrigin.X + ro.X, previewOrigin.Y + ro.Y, mapRect.Width, mapRect.Height);

			// Animation is complete
			if (frame == targetFrame)
			{
				if (enabled)
					AfterOpen();
				else
					AfterClose();
			}
		}

		int2 CellToMinimapPixel(CPos p)
		{
			var viewOrigin = new float2(mapRect.X, mapRect.Y);
			var mapOrigin = new CPos(world.Map.Bounds.Left, world.Map.Bounds.Top);

			return (viewOrigin + previewScale * (p - mapOrigin).ToFloat2()).ToInt2();
		}

		CPos MinimapPixelToCell(int2 p)
		{
			var viewOrigin = new float2(mapRect.X, mapRect.Y);
			var mapOrigin = new CPos(world.Map.Bounds.Left, world.Map.Bounds.Top);

			return (CPos)(mapOrigin.ToFloat2() + (1f / previewScale) * (p - viewOrigin)).ToInt2();
		}
	}
}
