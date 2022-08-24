#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	class GameInfoObjectivesLogic : ChromeLogic
	{
		readonly ContainerWidget template;

		[TranslationReference]
		static readonly string InProgress = "in-progress";

		[TranslationReference]
		static readonly string Accomplished = "accomplished";

		[TranslationReference]
		static readonly string Failed = "failed";

		[ObjectCreator.UseCtor]
		public GameInfoObjectivesLogic(Widget widget, World world, ModData modData)
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
			var inProgress = modData.Translation.GetString(InProgress);
			var accomplished = modData.Translation.GetString(Accomplished);
			var failed = modData.Translation.GetString(Failed);
			missionStatus.GetText = () => player.WinState == WinState.Undefined ? inProgress :
				player.WinState == WinState.Won ? accomplished : failed;
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

		static void PopulateObjectivesList(MissionObjectives mo, ScrollPanelWidget parent, ContainerWidget template)
		{
			parent.RemoveChildren();

			foreach (var objective in mo.Objectives.OrderBy(o => o.Type))
			{
				var widget = template.Clone();
				var label = widget.Get<LabelWidget>("OBJECTIVE_TYPE");
				label.GetText = () => objective.Type;

				var checkbox = widget.Get<CheckboxWidget>("OBJECTIVE_STATUS");
				checkbox.IsChecked = () => objective.State != ObjectiveState.Incomplete;
				checkbox.GetCheckmark = () => objective.State == ObjectiveState.Completed ? "tick" : "cross";
				checkbox.GetText = () => objective.Description;

				parent.AddChild(widget);
			}
		}
	}
}
