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
	[ScriptPropertyGroup("Experience")]
	public class GainsExperienceProperties : ScriptActorProperties, Requires<GainsExperienceInfo>
	{
		readonly GainsExperience exp;

		public GainsExperienceProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			exp = self.Trait<GainsExperience>();
		}

		[Desc("The actor's amount of experience.")]
		public int Experience => exp.Experience;

		[Desc("The actor's level.")]
		public int Level => exp.Level;

		[Desc("The actor's maximum possible level.")]
		public int MaxLevel => exp.MaxLevel;

		[Desc("Returns true if the actor can gain a level.")]
		public bool CanGainLevel => exp.CanGainLevel;

		[Desc("Gives the actor experience. If 'silent' is true, no animation or sound will be played if the actor levels up.")]
		public void GiveExperience(int amount, bool silent = false)
		{
			exp.GiveExperience(amount, silent);
		}

		[Desc("Gives the actor level(s). If 'silent' is true, no animation or sound will be played.")]
		public void GiveLevels(int numLevels, bool silent = false)
		{
			exp.GiveLevels(numLevels, silent);
		}
	}
}
