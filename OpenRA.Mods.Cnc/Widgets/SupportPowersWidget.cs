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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Widgets;
using OpenRA.Mods.RA;

namespace OpenRA.Mods.Cnc.Widgets
{
	class SupportPowersWidget : Widget
	{
		public int Spacing = 10;

		Dictionary<string, Sprite> iconSprites;
		Animation clock;
		Dictionary<Rectangle, string> Icons	= new Dictionary<Rectangle, string>();

		Rectangle eventBounds;
		public override Rectangle EventBounds { get { return eventBounds; } }
		readonly WorldRenderer worldRenderer;
		readonly SupportPowerManager spm;

		[ObjectCreator.UseCtor]
		public SupportPowersWidget([ObjectCreator.Param] World world,
		                          [ObjectCreator.Param] WorldRenderer worldRenderer)
		{
			this.worldRenderer = worldRenderer;
			spm = world.LocalPlayer.PlayerActor.Trait<SupportPowerManager>();

			iconSprites = Rules.Info.Values.SelectMany( u => u.Traits.WithInterface<SupportPowerInfo>() )
				.Select(u => u.Image).Distinct()
				.ToDictionary(
					u => u,
					u => Game.modData.SpriteLoader.LoadAllSprites(u)[0]);

			clock = new Animation("clock");
		}

		public void RefreshIcons()
		{
			Icons = new Dictionary<Rectangle, string>();
			var powers = spm.Powers.Where(p => !p.Value.Disabled).Select(p => p.Key);

			var i = 0;
			var rb = RenderBounds;
			foreach (var item in powers)
			{
				var rect = new Rectangle(rb.X + 1, rb.Y + i * (48 + Spacing) + 1, 64, 48);
				Icons.Add(rect, item);
				i++;
			}

			eventBounds = (Icons.Count == 0) ? Rectangle.Empty : Icons.Keys.Aggregate(Rectangle.Union);
		}

		public override void DrawInner()
		{
			var overlayFont = Game.Renderer.Fonts["TinyBold"];
			var holdOffset = new float2(32,24) - overlayFont.Measure("On Hold") / 2;
			var readyOffset = new float2(32,24) - overlayFont.Measure("Ready") / 2;

			foreach (var kv in Icons)
				WidgetUtils.DrawPanel("panel-black", kv.Key.InflateBy(1,1,1,1));

			foreach (var kv in Icons)
			{
				var rect = kv.Key;
				var power = spm.Powers[kv.Value];
				var drawPos = new float2(rect.Location);
				WidgetUtils.DrawSHP(iconSprites[power.Info.Image], drawPos, worldRenderer);

				// Charge progress
				clock.PlayFetchIndex("idle",
					() => (power.TotalTime - power.RemainingTime)
						* (clock.CurrentSequence.Length - 1) / power.TotalTime);
				clock.Tick();
				WidgetUtils.DrawSHP(clock.Image, drawPos, worldRenderer);

				if (power.Ready)
					overlayFont.DrawTextWithContrast("Ready",
					                                 drawPos + readyOffset,
					                                 Color.White, Color.Black, 1);
				else if (!power.Active)
					overlayFont.DrawTextWithContrast("On Hold",
					                                 drawPos + holdOffset,
					                                 Color.White, Color.Black, 1);
			}
		}

		public override void Tick ()
		{
			base.Tick();
			RefreshIcons();
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event != MouseInputEvent.Down)
				return false;

			var clicked = Icons.Where(i => i.Key.Contains(mi.Location))
				.Select(i => i.Value).FirstOrDefault();

			if (clicked != null)
				spm.Target(clicked);

			return true;
		}
	}
}