#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using Eluant;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("MissionData")]
	public class MissionDataProperties : ScriptPlayerProperties
	{
		public MissionDataProperties(ScriptContext context, Player player)
			: base(context, player) { }

		[ScriptActorPropertyActivity]
		[Desc("Write data to SavedMissionData.")]
		public void SaveMissionData(string category, string flag)
		{
			var data = Game.GlobalMissionData.SavedMissionData;
			if (!data.ContainsKey(category) || (data.ContainsKey(category) && data[category] != flag))
				Game.GlobalMissionData.SavedMissionData.Add(category, flag);

			Game.GlobalMissionData.Save();
		}
	}
}
