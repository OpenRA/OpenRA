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
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class DiplomacyLogic
	{
		static List<Widget> controls = new List<Widget>();

		readonly World world;

		[ObjectCreator.UseCtor]
		public DiplomacyLogic(Widget widget, Action onExit, World world)
		{
			this.world = world;
			var root = Ui.Root.Get("DIPLOMACY");
			var diplomacyBG = root.Get("DIPLOMACY_BG");
			LayoutDialog(diplomacyBG);

			var close = widget.GetOrNull<ButtonWidget>("CLOSE_DIPLOMACY");
			if (close != null)
				close.OnClick = () => { Ui.CloseWindow(); onExit(); };
		}

		// This is shit
		void LayoutDialog(Widget bg)
		{
			foreach (var c in controls)
				bg.RemoveChild(c);
			controls.Clear();

			var y = 50;
			var margin = 20;
			var labelWidth = (bg.Bounds.Width - 3 * margin) / 3;

			var ts = new LabelWidget
			{
				Font = "Bold",
				Bounds = new Rectangle(margin + labelWidth + 10, y, labelWidth, 25),
				Text = "Their Stance",
				Align = TextAlign.Left,
			};

			bg.AddChild(ts);
			controls.Add(ts);

			var ms = new LabelWidget
			{
				Font = "Bold",
				Bounds = new Rectangle(margin + 2 * labelWidth + 20, y, labelWidth, 25),
				Text = "My Stance",
				Align = TextAlign.Left,
			};

			bg.AddChild(ms);
			controls.Add(ms);

			y += 35;

			foreach (var p in world.Players.Where(a => a != world.LocalPlayer && !a.NonCombatant))
			{
				var pp = p;
				var label = new LabelWidget
				{
					Bounds = new Rectangle(margin, y, labelWidth, 25),
					Text = p.PlayerName,
					Align = TextAlign.Left,
					Font = "Bold",
					Color = p.Color.RGB,
				};

				bg.AddChild(label);
				controls.Add(label);

				var theirStance = new LabelWidget
				{
					Bounds = new Rectangle( margin + labelWidth + 10, y, labelWidth, 25),
					Text = p.PlayerName,
					Align = TextAlign.Left,

					GetText = () => pp.Stances[ world.LocalPlayer ].ToString(),
				};

				bg.AddChild(theirStance);
				controls.Add(theirStance);

				var myStance = new DropDownButtonWidget
				{
					Bounds = new Rectangle( margin + 2 * labelWidth + 20,  y, labelWidth, 25),
					GetText = () => world.LocalPlayer.Stances[ pp ].ToString(),
				};

				if (!p.World.LobbyInfo.GlobalSettings.FragileAlliances)
					myStance.Disabled = true;

				myStance.OnMouseDown = mi => ShowDropDown(pp, myStance);

				bg.AddChild(myStance);
				controls.Add(myStance);

				y += 35;
			}
		}

		void ShowDropDown(Player p, DropDownButtonWidget dropdown)
		{
			var stances = Enum<Stance>.GetValues();
			Func<Stance, ScrollItemWidget, ScrollItemWidget> setupItem = (s, template) =>
			{
				var item = ScrollItemWidget.Setup(template,
					() => s == world.LocalPlayer.Stances[ p ],
					() => SetStance(dropdown, p, s));

				item.Get<LabelWidget>("LABEL").GetText = () => s.ToString();
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 150, stances, setupItem);
		}

		void SetStance(ButtonWidget bw, Player p, Stance ss)
		{
			if (!p.World.LobbyInfo.GlobalSettings.FragileAlliances)
				return;	// team changes are banned

			// NOTE(jsd): Abuse of the type system here with `CPos`
			world.IssueOrder(new Order("SetStance", world.LocalPlayer.PlayerActor, false)
				{ TargetLocation = new CPos((int)ss, 0), TargetString = p.InternalName });

			bw.Text = ss.ToString();
		}
	}
}
