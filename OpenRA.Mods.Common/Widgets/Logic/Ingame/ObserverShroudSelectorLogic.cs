#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Lint;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	[ChromeLogicArgsHotkeys("CombinedViewKey", "WorldViewKey")]
	public class ObserverShroudSelectorLogic : ChromeLogic
	{
		[TranslationReference]
		const string CameraOptionAllPlayers = "options-shroud-selector.all-players";

		[TranslationReference]
		const string CameraOptionDisableShroud = "options-shroud-selector.disable-shroud";

		[TranslationReference]
		const string CameraOptionOther = "options-shroud-selector.other";

		[TranslationReference]
		const string Players = "label-players";

		[TranslationReference("team")]
		const string TeamNumber = "label-team-name";

		[TranslationReference]
		const string NoTeam = "label-no-team";

		readonly CameraOption combined, disableShroud;
		readonly IOrderedEnumerable<IGrouping<int, CameraOption>> teams;
		readonly bool limitViews;

		readonly HotkeyReference combinedViewKey = new HotkeyReference();
		readonly HotkeyReference worldViewKey = new HotkeyReference();

		readonly World world;

		CameraOption selected;
		readonly LabelWidget shroudLabel;

		class CameraOption
		{
			public readonly Player Player;
			public readonly string Label;
			public readonly Color Color;
			public readonly string Faction;
			public readonly Func<bool> IsSelected;
			public readonly Action OnClick;

			public CameraOption(ObserverShroudSelectorLogic logic, Player p)
			{
				Player = p;
				Label = p.PlayerName;
				Color = p.Color;
				Faction = p.Faction.InternalName;
				IsSelected = () => p.World.RenderPlayer == p;
				OnClick = () =>
				{
					p.World.RenderPlayer = p;
					logic.selected = this;
					p.World.Selection.Clear();
					WidgetUtils.BindPlayerNameAndStatus(logic.shroudLabel, p);
				};
			}

			public CameraOption(ObserverShroudSelectorLogic logic, World w, string label, Player p)
			{
				Player = p;
				Label = label;
				Color = Color.White;
				Faction = null;
				IsSelected = () => w.RenderPlayer == p;
				OnClick = () => { w.RenderPlayer = p; logic.selected = this; };
			}
		}

		[ObjectCreator.UseCtor]
		public ObserverShroudSelectorLogic(Widget widget, ModData modData, World world, WorldRenderer worldRenderer, Dictionary<string, MiniYaml> logicArgs)
		{
			this.world = world;

			if (logicArgs.TryGetValue("CombinedViewKey", out var yaml))
				combinedViewKey = modData.Hotkeys[yaml.Value];

			if (logicArgs.TryGetValue("WorldViewKey", out yaml))
				worldViewKey = modData.Hotkeys[yaml.Value];

			limitViews = world.Map.Visibility.HasFlag(MapVisibility.MissionSelector);

			var groups = new Dictionary<string, IEnumerable<CameraOption>>();

			combined = new CameraOption(this, world, modData.Translation.GetString(CameraOptionAllPlayers), world.Players.First(p => p.InternalName == "Everyone"));
			disableShroud = new CameraOption(this, world, modData.Translation.GetString(CameraOptionDisableShroud), null);
			if (!limitViews)
				groups.Add(modData.Translation.GetString(CameraOptionOther), new List<CameraOption>() { combined, disableShroud });

			teams = world.Players.Where(p => !p.NonCombatant && p.Playable)
				.Select(p => new CameraOption(this, p))
				.GroupBy(p => (world.LobbyInfo.ClientWithIndex(p.Player.ClientIndex) ?? new Session.Client()).Team)
				.OrderBy(g => g.Key);

			var noTeams = teams.Count() == 1;
			var totalPlayers = 0;
			foreach (var t in teams)
			{
				totalPlayers += t.Count();
				var label = noTeams ? modData.Translation.GetString(Players) : t.Key > 0
					? modData.Translation.GetString(TeamNumber, Translation.Arguments("team", t.Key))
					: modData.Translation.GetString(NoTeam);

				groups.Add(label, t);
			}

			var shroudSelectorDisabled = limitViews && totalPlayers < 2;
			var shroudSelector = widget.Get<DropDownButtonWidget>("SHROUD_SELECTOR");
			shroudSelector.IsDisabled = () => shroudSelectorDisabled;
			shroudSelector.OnMouseDown = _ =>
			{
				ScrollItemWidget SetupItem(CameraOption option, ScrollItemWidget template)
				{
					var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
					var showFlag = option.Faction != null;

					var label = item.Get<LabelWidget>("LABEL");
					label.IsVisible = () => showFlag;
					label.GetColor = () => option.Color;

					if (showFlag)
						WidgetUtils.BindPlayerNameAndStatus(label, option.Player);
					else
						label.GetText = () => option.Label;

					var flag = item.Get<ImageWidget>("FLAG");
					flag.IsVisible = () => showFlag;
					flag.GetImageCollection = () => "flags";
					flag.GetImageName = () => option.Faction;

					var labelAlt = item.Get<LabelWidget>("NOFLAG_LABEL");
					labelAlt.IsVisible = () => !showFlag;
					labelAlt.GetText = () => option.Label;
					labelAlt.GetColor = () => option.Color;

					return item;
				}

				shroudSelector.ShowDropDown("SPECTATOR_DROPDOWN_TEMPLATE", 400, groups, SetupItem);
			};

			shroudLabel = shroudSelector.Get<LabelWidget>("LABEL");
			shroudLabel.IsVisible = () => selected.Faction != null;
			shroudLabel.GetText = () => selected.Label;
			shroudLabel.GetColor = () => selected.Color;

			var shroudFlag = shroudSelector.Get<ImageWidget>("FLAG");
			shroudFlag.IsVisible = () => selected.Faction != null;
			shroudFlag.GetImageCollection = () => "flags";
			shroudFlag.GetImageName = () => selected.Faction;

			var shroudLabelAlt = shroudSelector.Get<LabelWidget>("NOFLAG_LABEL");
			shroudLabelAlt.IsVisible = () => selected.Faction == null;
			shroudLabelAlt.GetText = () => selected.Label;
			shroudLabelAlt.GetColor = () => selected.Color;

			var keyhandler = shroudSelector.Get<LogicKeyListenerWidget>("SHROUD_KEYHANDLER");
			keyhandler.AddHandler(HandleKeyPress);

			selected = limitViews ? groups.First().Value.First() : world.WorldActor.Owner.Shroud.ExploreMapEnabled ? combined : disableShroud;
			selected.OnClick();

			// Enable zooming out to fractional zoom levels
			worldRenderer.Viewport.UnlockMinimumZoom(0.5f);
		}

		public bool HandleKeyPress(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Down && !e.IsRepeat)
			{
				if (combinedViewKey.IsActivatedBy(e) && !limitViews)
				{
					selected = combined;
					selected.OnClick();

					return true;
				}

				if (worldViewKey.IsActivatedBy(e) && !limitViews)
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

		public override void Tick()
		{
			// Fix the selector if something else has changed the render player
			if (selected != null && world.RenderPlayer != selected.Player)
			{
				if (combined.Player == world.RenderPlayer)
					combined.OnClick();
				else if (disableShroud.Player == world.RenderPlayer)
					disableShroud.OnClick();
				else
					foreach (var group in teams)
						foreach (var option in group)
							if (option.Player == world.RenderPlayer)
								option.OnClick();
			}
		}
	}
}
