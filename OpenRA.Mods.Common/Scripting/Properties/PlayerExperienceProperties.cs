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

using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Player")]
	public class PlayerExperienceProperties : ScriptPlayerProperties, Requires<PlayerExperienceInfo>
	{
		readonly PlayerExperience exp;

		public PlayerExperienceProperties(ScriptContext context, Player player)
			: base(context, player)
		{
			exp = player.PlayerActor.Trait<PlayerExperience>();
		}

		public int Experience
		{
			get => exp.Experience;

			set => exp.GiveExperience(value - exp.Experience);
		}
	}
}
