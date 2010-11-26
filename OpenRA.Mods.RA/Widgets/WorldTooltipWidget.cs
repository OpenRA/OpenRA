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
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	public class WorldTooltipWidget : Widget
	{
		public int TooltipDelay = 10;
		readonly World world;
		[ObjectCreator.UseCtor]
		public WorldTooltipWidget( [ObjectCreator.Param] World world )
		{
			this.world = world;
		}

		public override void DrawInner( WorldRenderer wr )
		{
			if (Viewport.TicksSinceLastMove < TooltipDelay || world == null || world.LocalPlayer == null)
				return;

			var cell = Game.viewport.ViewToWorld(Viewport.LastMousePos).ToInt2();
			if (!world.Map.IsInMap(cell)) return;
			
			if (!world.LocalPlayer.Shroud.IsExplored(cell))
			{
				var utext = "Unexplored Terrain";
				var usz = Game.Renderer.BoldFont.Measure(utext) + new int2(20, 24);
				
				WidgetUtils.DrawPanel("dialog4", Rectangle.FromLTRB(
					Viewport.LastMousePos.X + 20, Viewport.LastMousePos.Y + 20,
					Viewport.LastMousePos.X + usz.X + 20, Viewport.LastMousePos.Y + usz.Y + 20));
	
				Game.Renderer.BoldFont.DrawText(utext,
					new float2(Viewport.LastMousePos.X + 30, Viewport.LastMousePos.Y + 30), Color.White);
					
				return;
			}
			
			var actor = world.FindUnitsAtMouse(Viewport.LastMousePos).FirstOrDefault();
			if (actor == null)
				return;
		
			var text = actor.Info.Traits.Contains<TooltipInfo>()
				? actor.Info.Traits.Get<TooltipInfo>().Name
				: actor.Info.Name;
			var text2 = (actor.Owner.NonCombatant)
				? "" : "{0}".F(actor.Owner.PlayerName);
			var text3 = (actor.Owner == world.LocalPlayer || actor.Owner.NonCombatant)
				? "" : " ({0})".F(world.LocalPlayer.Stances[actor.Owner]);

			var sz = Game.Renderer.BoldFont.Measure(text);
			var sz2 = Game.Renderer.RegularFont.Measure(text2);
			var sz3 = Game.Renderer.RegularFont.Measure(text3);
						
			sz.X = Math.Max(sz.X, sz2.X + sz3.X + 35);

			if (text2 != "") sz.Y += sz2.Y + 2;

			sz.X += 20;
			sz.Y += 24;

			WidgetUtils.DrawPanel("dialog4", Rectangle.FromLTRB(
				Viewport.LastMousePos.X + 20, Viewport.LastMousePos.Y + 20,
				Viewport.LastMousePos.X + sz.X + 20, Viewport.LastMousePos.Y + sz.Y + 20));

			Game.Renderer.BoldFont.DrawText(text,
				new float2(Viewport.LastMousePos.X + 30, Viewport.LastMousePos.Y + 30), Color.White);
			
			if (text2 != "")
			{
				Game.Renderer.RegularFont.DrawText(text2,
					new float2(Viewport.LastMousePos.X + 65, Viewport.LastMousePos.Y + 50), actor.Owner.Color);
				
				Game.Renderer.RegularFont.DrawText(text3,
					new float2(Viewport.LastMousePos.X + 65 + sz2.X, Viewport.LastMousePos.Y + 50), Color.White);

				WidgetUtils.DrawRGBA(
					ChromeProvider.GetImage("flags", actor.Owner.Country.Race),
					new float2(Viewport.LastMousePos.X + 30, Viewport.LastMousePos.Y + 50));
			}
		}
	}
}
