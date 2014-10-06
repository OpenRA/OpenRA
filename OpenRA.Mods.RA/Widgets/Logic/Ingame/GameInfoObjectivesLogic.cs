#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using System.Drawing;
using OpenRA.Widgets;
using OpenRA.Traits;
using OpenRA.Mods.RA;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	class GameInfoObjectivesLogic
	{
		ContainerWidget template;

		[ObjectCreator.UseCtor]
		public GameInfoObjectivesLogic(Widget widget, World world)
		{
			var lp = world.LocalPlayer;

			var missionStatus = widget.Get<LabelWidget>("MISSION_STATUS");
			missionStatus.GetText = () => lp.WinState == WinState.Undefined ? "In progress" :
				lp.WinState == WinState.Won ? "Accomplished" : "Failed";
			missionStatus.GetColor = () => lp.WinState == WinState.Undefined ? Color.White :
				lp.WinState == WinState.Won ? Color.LimeGreen : Color.Red;

			var mo = lp.PlayerActor.TraitOrDefault<MissionObjectives>();
			if (mo == null)
				return;

			var objectivesPanel = widget.Get<ScrollPanelWidget>("OBJECTIVES_PANEL");
			template = objectivesPanel.Get<ContainerWidget>("OBJECTIVE_TEMPLATE");

			PopulateObjectivesList(mo, objectivesPanel, template);

			Action<Player> RedrawObjectives = player =>
			{
				if (player == lp)
					PopulateObjectivesList(mo, objectivesPanel, template);
			};
			mo.ObjectiveAdded += RedrawObjectives;
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