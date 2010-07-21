#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Widgets
{
	public class WorldTooltipWidget : Widget
	{
		const int worldTooltipDelay = 10;		/* ticks */

		public WorldTooltipWidget() : base() { }
		
		public override void DrawInner(World world)
		{
			if (Widget.TicksSinceLastMove < worldTooltipDelay || world == null || world.LocalPlayer == null)
				return;

			var actor = world.FindUnitsAtMouse(Widget.LastMousePos).FirstOrDefault();
			if (actor == null) return;

			var text = actor.Info.Traits.Contains<ValuedInfo>()
				? actor.Info.Traits.Get<ValuedInfo>().Description
				: actor.Info.Name;
			var text2 = (actor.Owner.NonCombatant)
				? "" : "{0}".F(actor.Owner.PlayerName);
			var text3 = (actor.Owner == world.LocalPlayer || actor.Owner.NonCombatant)
				? "" : " ({0})".F(world.LocalPlayer.Stances[actor.Owner]);
			var renderer = Game.Renderer;

			var sz = renderer.BoldFont.Measure(text);
			var sz2 = renderer.RegularFont.Measure(text2);
			var sz3 = renderer.RegularFont.Measure(text3);
						
			sz.X = Math.Max(sz.X, sz2.X + sz3.X + 35);

			if (text2 != "") sz.Y += sz2.Y + 2;

			sz.X += 20;
			sz.Y += 24;

			WidgetUtils.DrawPanel("dialog4", Rectangle.FromLTRB(
				Widget.LastMousePos.X + 20, Widget.LastMousePos.Y + 20,
				Widget.LastMousePos.X + sz.X + 20, Widget.LastMousePos.Y + sz.Y + 20));

			renderer.BoldFont.DrawText(text,
				new float2(Widget.LastMousePos.X + 30, Widget.LastMousePos.Y + 30), Color.White);
			
			if (text2 != "")
			{
				renderer.RegularFont.DrawText(text2,
					new float2(Widget.LastMousePos.X + 65, Widget.LastMousePos.Y + 50), actor.Owner.Color);
				
				renderer.RegularFont.DrawText(text3,
					new float2(Widget.LastMousePos.X + 65 + sz2.X, Widget.LastMousePos.Y + 50), Color.White);

				WidgetUtils.DrawRGBA(
					ChromeProvider.GetImage(Game.Renderer, "flags", actor.Owner.Country.Race),
					new float2(Widget.LastMousePos.X + 30, Widget.LastMousePos.Y + 50));
			}
			
			renderer.RgbaSpriteRenderer.Flush();
		}
	}
}
