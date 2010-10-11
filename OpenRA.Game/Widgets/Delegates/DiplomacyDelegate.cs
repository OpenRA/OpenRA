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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Widgets.Delegates
{
	public class DiplomacyDelegate : IWidgetDelegate
	{
		static List<Widget> controls = new List<Widget>();

		int validPlayers = 0;
		readonly World world;
		[ObjectCreator.UseCtor]
		public DiplomacyDelegate( [ObjectCreator.Param] World world )
		{
			this.world = world;
			var root = Widget.RootWidget.GetWidget("INGAME_ROOT");
			var diplomacyBG = root.GetWidget("DIPLOMACY_BG");
			var diplomacy = root.GetWidget("INGAME_DIPLOMACY_BUTTON");
			diplomacy.OnMouseUp = mi =>
			{
				diplomacyBG.Visible = !diplomacyBG.Visible;
				if (diplomacyBG.IsVisible())
					LayoutDialog(diplomacyBG);
				return true;
			};
			
			Game.AfterGameStart += _ => validPlayers = world.players.Values.Where(a => a != world.LocalPlayer && !a.NonCombatant).Count();
			diplomacy.IsVisible = () => (validPlayers > 0);
		}

		void LayoutDialog(Widget bg)
		{
			bg.Children.RemoveAll(w => controls.Contains(w));
			controls.Clear();

			var y = 50;
			var margin = 20;
			var labelWidth = (bg.Bounds.Width - 3 * margin) / 3;

			var ts = new LabelWidget
			{
				Bold = true,
				Bounds = new Rectangle(margin + labelWidth + 10, y, labelWidth, 25),
				Text = "Their Stance",
				Align = LabelWidget.TextAlign.Left,
			};

			bg.AddChild(ts);
			controls.Add(ts);

			var ms = new LabelWidget
			{
				Bold = true,
				Bounds = new Rectangle(margin + 2 * labelWidth + 20, y, labelWidth, 25),
				Text = "My Stance",
				Align = LabelWidget.TextAlign.Left,
			};

			bg.AddChild(ms);
			controls.Add(ms);

			y += 35;

			foreach (var p in world.players.Values.Where(a => a != world.LocalPlayer && !a.NonCombatant))
			{
				var pp = p;
				var label = new LabelWidget
				{
					Bounds = new Rectangle(margin, y, labelWidth, 25),
					Id = "DIPLOMACY_PLAYER_LABEL_{0}".F(p.Index),
					Text = p.PlayerName,
					Align = LabelWidget.TextAlign.Left,
					Bold = true,
				};

				bg.AddChild(label);
				controls.Add(label);

				var theirStance = new LabelWidget
				{
					Bounds = new Rectangle( margin + labelWidth + 10, y, labelWidth, 25),
					Id = "DIPLOMACY_PLAYER_LABEL_THEIR_{0}".F(p.Index),
					Text = p.PlayerName,
					Align = LabelWidget.TextAlign.Left,
					Bold = false,

					GetText = () => pp.Stances[ world.LocalPlayer ].ToString(),
				};

				bg.AddChild(theirStance);
				controls.Add(theirStance);

				var myStance = new ButtonWidget
				{
					Bounds = new Rectangle( margin + 2 * labelWidth + 20,  y, labelWidth, 25),
					Id = "DIPLOMACY_PLAYER_LABEL_MY_{0}".F(p.Index),
					Text = world.LocalPlayer.Stances[ pp ].ToString(),
				};

				myStance.OnMouseUp = mi => { CycleStance(pp, myStance); return true; };

				bg.AddChild(myStance);
				controls.Add(myStance);
				
				y += 35;
			}
		}

		Stance GetNextStance(Stance s)
		{
			switch (s)
			{
				case Stance.Ally: return Stance.Enemy;
				case Stance.Enemy: return Stance.Neutral;
				case Stance.Neutral: return Stance.Ally;
				default:
					throw new ArgumentException();
			}
		}

		void CycleStance(Player p, ButtonWidget bw)
		{
			if (p.World.LobbyInfo.GlobalSettings.LockTeams)
				return;	// team changes are banned

			var nextStance = GetNextStance((Stance)Enum.Parse(typeof(Stance), bw.Text));

			Game.IssueOrder(new Order("SetStance", world.LocalPlayer.PlayerActor,
				new int2(p.Index, (int)nextStance)));

			bw.Text = nextStance.ToString();
		}
	}
}
