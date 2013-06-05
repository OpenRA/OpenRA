#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Missions;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class MissionObjectivesLogic
	{
		IHasObjectives objectives;
		Widget primaryPanel;
		Widget secondaryPanel;
		Widget primaryTemplate;
		Widget secondaryTemplate;
		ButtonWidget objectivesButton;

		[ObjectCreator.UseCtor]
		public MissionObjectivesLogic(World world, Widget widget)
		{
			var gameRoot = Ui.Root.Get("INGAME_ROOT");
			primaryPanel = widget.Get("PRIMARY_OBJECTIVES");
			secondaryPanel = widget.Get("SECONDARY_OBJECTIVES");
			primaryTemplate = primaryPanel.Get("PRIMARY_OBJECTIVE_TEMPLATE");
			secondaryTemplate = secondaryPanel.Get("SECONDARY_OBJECTIVE_TEMPLATE");

			objectives = world.WorldActor.TraitsImplementing<IHasObjectives>().First();

			objectivesButton = gameRoot.Get<ButtonWidget>("OBJECTIVES_BUTTON");
			objectivesButton.IsHighlighted = () => Game.LocalTick % 50 < 25 && objectivesButton.Highlighted;
			objectivesButton.OnClick += () => objectivesButton.Highlighted = false;

			objectives.OnObjectivesUpdated += UpdateObjectives;
			UpdateObjectives(true);
			Game.ConnectionStateChanged += RemoveHandlers;
		}

		public void RemoveHandlers(OrderManager orderManager)
		{
			if (!orderManager.GameStarted)
			{
				Game.ConnectionStateChanged -= RemoveHandlers;
				objectives.OnObjectivesUpdated -= UpdateObjectives;
			}
		}

		public void UpdateObjectives(bool notify)
		{
			if (notify)
				objectivesButton.Highlighted = true;

			primaryPanel.RemoveChildren();
			secondaryPanel.RemoveChildren();

			foreach (var o in objectives.Objectives.Where(o => o.Status != ObjectiveStatus.Inactive))
			{
				var objective = o;

				Widget widget;
				LabelWidget objectiveText;
				LabelWidget objectiveStatus;

				if (objective.Type == ObjectiveType.Primary)
				{
					widget = primaryTemplate.Clone();
					objectiveText = widget.Get<LabelWidget>("PRIMARY_OBJECTIVE");
					objectiveStatus = widget.Get<LabelWidget>("PRIMARY_STATUS");
					SetupWidget(widget, objectiveText, objectiveStatus, objective);
					primaryPanel.AddChild(widget);
				}
				else
				{
					widget = secondaryTemplate.Clone();
					objectiveText = widget.Get<LabelWidget>("SECONDARY_OBJECTIVE");
					objectiveStatus = widget.Get<LabelWidget>("SECONDARY_STATUS");
					SetupWidget(widget, objectiveText, objectiveStatus, objective);
					secondaryPanel.AddChild(widget);
				}
			}
		}

		void SetupWidget(Widget widget, LabelWidget objectiveText, LabelWidget objectiveStatus, Objective objective)
		{
			var font = Game.Renderer.Fonts[objectiveText.Font];
			var text = WidgetUtils.WrapText(objective.Text, objectiveText.Bounds.Width, font);
			widget.Bounds.Height = objectiveText.Bounds.Height = objectiveStatus.Bounds.Height = font.Measure(text).Y;
			objectiveText.GetText = () => text;
			objectiveStatus.GetText = () => GetObjectiveStatusText(objective.Status);
		}

		static string GetObjectiveStatusText(ObjectiveStatus status)
		{
			switch (status)
			{
				case ObjectiveStatus.InProgress: return "In Progress";
				case ObjectiveStatus.Completed: return "Completed";
				case ObjectiveStatus.Failed: return "Failed";
				default: return "";
			}
		}
	}
}
