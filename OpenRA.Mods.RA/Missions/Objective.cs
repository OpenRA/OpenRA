#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Missions
{
	public class Objective
	{
		public ObjectiveType Type;
		public string Text;
		public ObjectiveStatus Status;

		public Objective(ObjectiveType type, string text, ObjectiveStatus status)
		{
			Type = type;
			Text = text;
			Status = status;
		}
	}

	public enum ObjectiveType { Primary, Secondary }
	public enum ObjectiveStatus { Inactive, InProgress, Completed, Failed }

	public interface IHasObjectives
	{
		event Action<bool> OnObjectivesUpdated;
		IEnumerable<Objective> Objectives { get; }
	}

	public class MissionObjectivesPanelInfo : ITraitInfo
	{
		public string ObjectivesPanel = null;
		public object Create(ActorInitializer init) { return new MissionObjectivesPanel(this); }
	}

	public class MissionObjectivesPanel : IObjectivesPanel
	{
		MissionObjectivesPanelInfo info;
		public MissionObjectivesPanel(MissionObjectivesPanelInfo info) { this.info = info; }
		public string ObjectivesPanel { get { return info.ObjectivesPanel; } }
	}
}
