#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	class GameInfoObjectivesLogic : ChromeLogic
	{
		readonly ContainerWidget template;

		[ObjectCreator.UseCtor]
		public GameInfoObjectivesLogic(Widget widget, World world)
		{
			var player = world.RenderPlayer ?? world.LocalPlayer;

			var objectivesPanel = widget.Get<ScrollPanelWidget>("OBJECTIVES_PANEL");
			template = objectivesPanel.Get<ContainerWidget>("OBJECTIVE_TEMPLATE");

			if (player == null)
			{
				objectivesPanel.RemoveChildren();
				return;
			}

			var mo = player.PlayerActor.TraitOrDefault<MissionObjectives>();
			if (mo == null)
			{
				objectivesPanel.RemoveChildren();
				return;
			}

			var missionStatus = widget.Get<LabelWidget>("MISSION_STATUS");
			missionStatus.GetText = () => player.WinState == WinState.Undefined ? "In progress" :
				player.WinState == WinState.Won ? "Accomplished" : "Failed";
			missionStatus.GetColor = () => player.WinState == WinState.Undefined ? Color.White :
				player.WinState == WinState.Won ? Color.LimeGreen : Color.Red;

			PopulateObjectivesList(mo, objectivesPanel, template);

			Action<Player, bool> redrawObjectives = (p, _) =>
			{
				if (p == player)
					PopulateObjectivesList(mo, objectivesPanel, template);
			};
			mo.ObjectiveAdded += redrawObjectives;
		}

		void PopulateObjectivesList(MissionObjectives mo, ScrollPanelWidget parent, ContainerWidget template)
		{
			parent.RemoveChildren();

			foreach (var o in mo.Objectives.OrderBy(o => o.Type))
			{
				var objective = o; // Work around the loop closure issue in older versions of C#
				var widget = template.Clone();

				var label = widget.Get<LabelWidget>("OBJECTIVE_TYPE");
				label.GetText = () => objective.Type == ObjectiveType.Primary ? "Primary" : "Secondary";

				var checkbox = widget.Get<CheckboxWidget>("OBJECTIVE_STATUS");
				checkbox.IsChecked = () => objective.State != ObjectiveState.Incomplete;
				checkbox.GetCheckType = () => objective.State == ObjectiveState.Completed ? "checked" : "crossed";
				checkbox.GetText = () => objective.Description;

				parent.AddChild(widget);
			}
		}
	}
}