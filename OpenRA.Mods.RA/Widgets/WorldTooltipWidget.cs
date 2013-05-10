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
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Widgets
{
	public class WorldTooltipWidget : Widget
	{
		public int TooltipDelay = 10;
		readonly World world;

		[ObjectCreator.UseCtor]
		public WorldTooltipWidget(World world) { this.world = world; }

		public override void Draw()
		{
			if (Viewport.TicksSinceLastMove < TooltipDelay || world == null)
				return;

			var cell = Game.viewport.ViewToWorld(Viewport.LastMousePos);
			if (!world.Map.IsInMap(cell))
				return;

			if (world.ShroudObscures(cell))
			{
				var utext = "Unexplored Terrain";
				var usz = Game.Renderer.Fonts["Bold"].Measure(utext) + new int2(20, 24);

				WidgetUtils.DrawPanel("dialog4", Rectangle.FromLTRB(
					Viewport.LastMousePos.X + 20, Viewport.LastMousePos.Y + 20,
					Viewport.LastMousePos.X + usz.X + 20, Viewport.LastMousePos.Y + usz.Y + 20));

				Game.Renderer.Fonts["Bold"].DrawText(utext,
					new float2(Viewport.LastMousePos.X + 30, Viewport.LastMousePos.Y + 30), Color.White);

				return;
			}

			var actor = world.FindUnitsAtMouse(Viewport.LastMousePos).FirstOrDefault();
			if (actor == null)
				return;

			var itt = actor.TraitsImplementing<IToolTip>().FirstOrDefault();
			if (itt == null)
				return;

			var owner = itt.Owner();
			var nameText = itt.Name();
			var ownerText = !owner.NonCombatant ? owner.PlayerName : "";
			var stanceText = (world.LocalPlayer != null && owner != actor.World.LocalPlayer
							  && !owner.NonCombatant) ? " ({0})".F(itt.Stance()) : "";

			var nameSize = Game.Renderer.Fonts["Bold"].Measure(nameText);
			var ownerSize = Game.Renderer.Fonts["Regular"].Measure(ownerText);
			var stanceSize = Game.Renderer.Fonts["Regular"].Measure(stanceText);
			var panelSize = new int2(Math.Max(nameSize.X, ownerSize.X + stanceSize.X + 35) + 20, nameSize.Y + 24);

			if (ownerText != "") panelSize.Y += ownerSize.Y + 2;

			WidgetUtils.DrawPanel("dialog4", Rectangle.FromLTRB(
				Viewport.LastMousePos.X + 20, Viewport.LastMousePos.Y + 20,
				Viewport.LastMousePos.X + panelSize.X + 20, Viewport.LastMousePos.Y + panelSize.Y + 20));

			Game.Renderer.Fonts["Bold"].DrawText(nameText,
				new float2(Viewport.LastMousePos.X + 30, Viewport.LastMousePos.Y + 30), Color.White);

			if (ownerText != "")
			{
				Game.Renderer.Fonts["Regular"].DrawText(ownerText,
					new float2(Viewport.LastMousePos.X + 65, Viewport.LastMousePos.Y + 50), actor.Owner.Color.RGB);

				Game.Renderer.Fonts["Regular"].DrawText(stanceText,
					new float2(Viewport.LastMousePos.X + 65 + ownerSize.X, Viewport.LastMousePos.Y + 50), Color.White);

				WidgetUtils.DrawRGBA(
					ChromeProvider.GetImage("flags", actor.Owner.Country.Race),
					new float2(Viewport.LastMousePos.X + 30, Viewport.LastMousePos.Y + 50));
			}
		}
	}
}
