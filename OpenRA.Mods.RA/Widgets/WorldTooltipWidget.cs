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
	public class WorldTooltipWidget : Widget
	{
		public int TooltipDelay = 10;
		readonly World world;
		[ObjectCreator.UseCtor]
		public WorldTooltipWidget( [ObjectCreator.Param] World world )
		{
			this.world = world;
		}

		public override void DrawInner()
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
		
			var name = actor.Info.Traits.Contains<TooltipInfo>()
				? actor.Info.Traits.Get<TooltipInfo>().Name
				: actor.Info.Name;
			var owner = (actor.Owner.NonCombatant)
				? "" : "{0}".F(actor.Owner.PlayerName);
			var stance = (actor.Owner == world.LocalPlayer || actor.Owner.NonCombatant)
				? "" : " ({0})".F(world.LocalPlayer.Stances[actor.Owner]);

			var nameSize = Game.Renderer.BoldFont.Measure(name);
			var ownerSize = Game.Renderer.RegularFont.Measure(owner);
			var stanceSize = Game.Renderer.RegularFont.Measure(stance);
						
			var panelSize = new int2(Math.Max(nameSize.X, ownerSize.X + stanceSize.X + 35) + 20, nameSize.Y + 24);

			if (owner != "") panelSize.Y += ownerSize.Y + 2;

			WidgetUtils.DrawPanel("dialog4", Rectangle.FromLTRB(
				Viewport.LastMousePos.X + 20, Viewport.LastMousePos.Y + 20,
				Viewport.LastMousePos.X + panelSize.X + 20, Viewport.LastMousePos.Y + panelSize.Y + 20));

			Game.Renderer.BoldFont.DrawText(name,
				new float2(Viewport.LastMousePos.X + 30, Viewport.LastMousePos.Y + 30), Color.White);
			
			if (owner != "")
			{
				Game.Renderer.RegularFont.DrawText(owner,
					new float2(Viewport.LastMousePos.X + 65, Viewport.LastMousePos.Y + 50), actor.Owner.ColorRamp.GetColor(0));
				
				Game.Renderer.RegularFont.DrawText(stance,
					new float2(Viewport.LastMousePos.X + 65 + ownerSize.X, Viewport.LastMousePos.Y + 50), Color.White);

				WidgetUtils.DrawRGBA(
					ChromeProvider.GetImage("flags", actor.Owner.Country.Race),
					new float2(Viewport.LastMousePos.X + 30, Viewport.LastMousePos.Y + 50));
			}
		}
	}
}
