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
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ObserverShroudSelectorLogic
	{
		CameraOption selected;
		CameraOption combined, disableShroud;
		IOrderedEnumerable<IGrouping<int, CameraOption>> teams;

		class CameraOption
		{
			public readonly Player Player;
			public readonly string Label;
			public readonly Color Color;
			public readonly string Race;
			public readonly Func<bool> IsSelected;
			public readonly Action OnClick;

			public CameraOption(ObserverShroudSelectorLogic logic, Player p)
			{
				Player = p;
				Label = p.PlayerName;
				Color = p.Color.RGB;
				Race = p.Country.Race;
				IsSelected = () => p.World.RenderPlayer == p;
				OnClick = () => { p.World.RenderPlayer = p; logic.selected = this; };
			}

			public CameraOption(ObserverShroudSelectorLogic logic, World w, string label, Player p)
			{
				Player = p;
				Label = label;
				Color = Color.White;
				Race = null;
				IsSelected = () => w.RenderPlayer == p;
				OnClick = () => { w.RenderPlayer = p; logic.selected = this; };
			}
		}

		[ObjectCreator.UseCtor]
		public ObserverShroudSelectorLogic(Widget widget, World world)
		{
			var groups = new Dictionary<string, IEnumerable<CameraOption>>();

			teams = world.Players.Where(p => !p.NonCombatant)
				.Select(p => new CameraOption(this, p))
				.GroupBy(p => (world.LobbyInfo.ClientWithIndex(p.Player.ClientIndex) ?? new Session.Client()).Team)
				.OrderBy(g => g.Key);

			var noTeams = teams.Count() == 1;
			foreach (var t in teams)
			{
				var label = noTeams ? "Players" : t.Key == 0 ? "No Team" : "Team {0}".F(t.Key);
				groups.Add(label, t);
			}

			combined = new CameraOption(this, world, "All Players", world.Players.First(p => p.InternalName == "Everyone"));
			disableShroud = new CameraOption(this, world, "Disable Shroud", null);
			groups.Add("Other", new List<CameraOption>() { combined, disableShroud });

			var shroudSelector = widget.Get<DropDownButtonWidget>("SHROUD_SELECTOR");
			shroudSelector.OnMouseDown = _ =>
			{
				Func<CameraOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
				{
					var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
					var showFlag = option.Race != null;

					var label = item.Get<LabelWidget>("LABEL");
					label.IsVisible = () => showFlag;
					label.GetText = () => option.Label;
					label.GetColor = () => option.Color;

					var flag = item.Get<ImageWidget>("FLAG");
					flag.IsVisible = () => showFlag;
					flag.GetImageCollection = () => "flags";
					flag.GetImageName = () => option.Race;

					var labelAlt = item.Get<LabelWidget>("NOFLAG_LABEL");
					labelAlt.IsVisible = () => !showFlag;
					labelAlt.GetText = () => option.Label;
					labelAlt.GetColor = () => option.Color;

					return item;
				};

				shroudSelector.ShowDropDown("SPECTATOR_DROPDOWN_TEMPLATE", 400, groups, setupItem);
			};

			var shroudLabel = shroudSelector.Get<LabelWidget>("LABEL");
			shroudLabel.IsVisible = () => selected.Race != null;
			shroudLabel.GetText = () => selected.Label;
			shroudLabel.GetColor = () => selected.Color;

			var shroudFlag = shroudSelector.Get<ImageWidget>("FLAG");
			shroudFlag.IsVisible = () => selected.Race != null;
			shroudFlag.GetImageCollection = () => "flags";
			shroudFlag.GetImageName = () => selected.Race;

			var shroudLabelAlt = shroudSelector.Get<LabelWidget>("NOFLAG_LABEL");
			shroudLabelAlt.IsVisible = () => selected.Race == null;
			shroudLabelAlt.GetText = () => selected.Label;
			shroudLabelAlt.GetColor = () => selected.Color;

			var keyhandler = shroudSelector.Get<LogicKeyListenerWidget>("SHROUD_KEYHANDLER");
			keyhandler.OnKeyPress = HandleKeyPress;

			selected = disableShroud;
		}

		public bool HandleKeyPress(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Down)
			{
				var h = Hotkey.FromKeyInput(e);
				if (h == Game.Settings.Keys.ObserverCombinedView)
				{
					selected = combined;
					selected.OnClick();

					return true;
				}

				if (h == Game.Settings.Keys.ObserverWorldView)
				{
					selected = disableShroud;
					selected.OnClick();

					return true;
				}

				if (e.Key >= Keycode.NUMBER_0 && e.Key <= Keycode.NUMBER_9)
				{
					var key = (int)e.Key - (int)Keycode.NUMBER_0;
					var team = teams.Where(t => t.Key == key).SelectMany(s => s);
					if (!team.Any())
						return false;

					if (e.Modifiers == Modifiers.Shift)
						team = team.Reverse();

					selected = team.SkipWhile(t => t.Player != selected.Player).Skip(1).FirstOrDefault() ?? team.FirstOrDefault();
					selected.OnClick();

					return true;
				}
			}

			return false;
		}
	}
}
