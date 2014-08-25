#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	class LeaveMapLogic
	{
		[ObjectCreator.UseCtor]
		public LeaveMapLogic(Widget widget, World world)
		{
			widget.Get<LabelWidget>("VERSION_LABEL").Text = Game.modData.Manifest.Mod.Version;

			var panelName = "LEAVE_RESTART_SIMPLE";

			var iop = world.WorldActor.TraitsImplementing<IObjectivesPanel>().FirstOrDefault();
			var showObjectives = iop != null && iop.PanelName != null && world.LocalPlayer != null;

			if (showObjectives)
				panelName = "LEAVE_RESTART_FULL";

			var dialog = widget.Get<ContainerWidget>(panelName);
			dialog.IsVisible = () => true;
			widget.IsVisible = () => Ui.CurrentWindow() == null;

			var leaveButton = dialog.Get<ButtonWidget>("LEAVE_BUTTON");
			leaveButton.OnClick = () =>
			{
				leaveButton.Disabled = true;
				var mpe = world.WorldActor.TraitOrDefault<MenuPaletteEffect>();

				Sound.PlayNotification(world.Map.Rules, null, "Speech", "Leave",
					world.LocalPlayer == null ? null : world.LocalPlayer.Country.Race);

				var exitDelay = 1200;
				if (mpe != null)
				{
					Game.RunAfterDelay(exitDelay, () => mpe.Fade(MenuPaletteEffect.EffectType.Black));
					exitDelay += 40 * mpe.Info.FadeLength;
				}

				Game.RunAfterDelay(exitDelay, () =>
				{
					Game.Disconnect();
					Ui.ResetAll();
					Game.LoadShellMap();
				});
			};

			if (showObjectives)
			{
				var objectivesContainer = dialog.Get<ContainerWidget>("OBJECTIVES");
				Game.LoadWidget(world, iop.PanelName, objectivesContainer, new WidgetArgs());
			}
		}
	}
}