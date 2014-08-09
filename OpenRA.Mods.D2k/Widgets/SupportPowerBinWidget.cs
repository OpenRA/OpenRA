#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.D2k.Widgets
{
	class SupportPowerBinWidget : Widget
	{
		[Translate] public string ReadyText = "";
		[Translate] public string HoldText = "";

		public int IconWidth = 64;
		public int IconHeight = 48;

		Animation icon;
		Animation clock;
		readonly List<Pair<Rectangle, Action<MouseInput>>> buttons = new List<Pair<Rectangle,Action<MouseInput>>>();

		readonly World world;
		readonly WorldRenderer worldRenderer;

		[ObjectCreator.UseCtor]
		public SupportPowerBinWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
		}

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			icon = new Animation(world, "icon");
			clock = new Animation(world, "clock");
		}

		public override Rectangle EventBounds
		{
			get { return buttons.Any() ? buttons.Select(b => b.First).Aggregate(Rectangle.Union) : Bounds; }
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Down)
			{
				var action = buttons.Where(a => a.First.Contains(mi.Location))
				.Select(a => a.Second).FirstOrDefault();
				if (action == null)
					return false;

				action(mi);
				return true;
			}

			return false;
		}

		public override void Draw()
		{
			buttons.Clear();

			if( world.LocalPlayer == null ) return;

			var powers = this.world.ActorsWithTrait<SupportPower>().Where(p => p.Trait.HasPrerequisites);
			var numPowers = powers.Count();
			if (numPowers == 0) return;

			var rectBounds = RenderBounds;
			WidgetUtils.DrawRGBA(WidgetUtils.GetChromeImage(world, "specialbin-top"),new float2(rectBounds.X,rectBounds.Y));
			for (var i = 1; i < numPowers; i++)
				WidgetUtils.DrawRGBA(WidgetUtils.GetChromeImage(world, "specialbin-middle"), new float2(rectBounds.X, rectBounds.Y + i * 51));
			WidgetUtils.DrawRGBA(WidgetUtils.GetChromeImage(world, "specialbin-bottom"), new float2(rectBounds.X, rectBounds.Y + numPowers * 51));

			// HACK: Hack Hack Hack
			rectBounds.Width = IconWidth + 5;
			rectBounds.Height = 31 + numPowers * (IconHeight + 3);

			var y = rectBounds.Y + 10;
			var iconSize = new float2(IconWidth, IconHeight);
			foreach (var kv in powers)
			{
				var sp = kv.Trait;
				icon.Play(sp.Info.Icon);

				var drawPos = new float2(rectBounds.X + 5, y);
				var rect = new Rectangle(rectBounds.X + 5, y, 64, 48);

				if (rect.Contains(Viewport.LastMousePos))
				{
					var pos = drawPos.ToInt2();
					var tl = new int2(pos.X-3,pos.Y-3);
					var m = new int2(pos.X+64+3,pos.Y+48+3);
					var br = tl + new int2(64+3+20,40);

					if (sp.TotalTime > 0)
						br += new int2(0,20);

					if (sp.Info.LongDesc != null)
						br += Game.Renderer.Fonts["Regular"].Measure(sp.Info.LongDesc.Replace("\\n", "\n"));
					else
						br += new int2(300,0);

					var border = WidgetUtils.GetBorderSizes("dialog4");

					WidgetUtils.DrawPanelPartial("dialog4", Rectangle.FromLTRB(tl.X, tl.Y, m.X + border[3], m.Y),
						PanelSides.Left | PanelSides.Top | PanelSides.Bottom | PanelSides.Center);
					WidgetUtils.DrawPanelPartial("dialog4", Rectangle.FromLTRB(m.X - border[2], tl.Y, br.X, m.Y + border[1]),
						PanelSides.Top | PanelSides.Right | PanelSides.Center);
					WidgetUtils.DrawPanelPartial("dialog4", Rectangle.FromLTRB(m.X, m.Y - border[1], br.X, br.Y),
						PanelSides.Left | PanelSides.Right | PanelSides.Bottom | PanelSides.Center);

					pos += new int2(77, 5);
					Game.Renderer.Fonts["Bold"].DrawText(sp.Info.Description, pos, Color.White);

					if (sp.TotalTime > 0)
					{
						pos += new int2(0,20);
						Game.Renderer.Fonts["Bold"].DrawText(WidgetUtils.FormatTime(sp.RemainingTime), pos, Color.White);
						Game.Renderer.Fonts["Bold"].DrawText("/ {0}".F(WidgetUtils.FormatTime(sp.TotalTime)), pos + new int2(45,0), Color.White);
					}

					if (sp.Info.LongDesc != null)
					{
						pos += new int2(0, 20);
						Game.Renderer.Fonts["Regular"].DrawText(sp.Info.LongDesc.Replace("\\n", "\n"), pos, Color.White);
					}
				}

				WidgetUtils.DrawSHPCentered(icon.Image, drawPos + 0.5f * iconSize, worldRenderer);

				clock.PlayFetchIndex("idle",
					() => sp.TotalTime == 0 ? clock.CurrentSequence.Length - 1 : (sp.TotalTime - sp.RemainingTime)
						* (clock.CurrentSequence.Length - 1) / sp.TotalTime);
				clock.Tick();

				WidgetUtils.DrawSHPCentered(clock.Image, drawPos + 0.5f * iconSize, worldRenderer);

				var overlay = sp.Ready ? ReadyText : !sp.Disabled ? null : HoldText;
				var font = Game.Renderer.Fonts["TinyBold"];
				if (overlay != null)
				{
					var size = font.Measure(overlay);
					var overlayPos = drawPos + new float2(32, 16);
					font.DrawTextWithContrast(overlay, overlayPos - new float2(size.X / 2, 0), Color.White, Color.Black, 1);
				}

				buttons.Add(Pair.New(rect,HandleSupportPower(sp)));

				y += 51;
			}
		}

		static Action<MouseInput> HandleSupportPower(SupportPower sp)
		{
			return mi =>
			{
				if (mi.Button == MouseButton.Left)
				{
					if (sp.Disabled)
						Sound.PlayToPlayer(sp.Self.Owner, sp.Info.InsufficientPowerSound);
					sp.TargetLocation();
				}
			};
		}
	}
}