#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using Eluant;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Ability")]
	public class DeliversCashProperties : ScriptActorProperties, Requires<IMoveInfo>, Requires<DeliversCashInfo>
	{
		readonly DeliversCashInfo info;

		public DeliversCashProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			info = Self.Info.TraitInfo<DeliversCashInfo>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Deliver cash to the target actor.")]
		public void DeliverCash(Actor target)
		{
			var t = Target.FromActor(target);

			// NB: Scripted actions get no visible targetlines.
			Self.QueueActivity(new DonateCash(Self, t, info.Payload, info.PlayerExperience, null));
		}
	}

	[ScriptPropertyGroup("Ability")]
	public class DeliversExperienceProperties : ScriptActorProperties, Requires<IMoveInfo>, Requires<DeliversExperienceInfo>
	{
		readonly DeliversExperienceInfo deliversExperience;
		readonly GainsExperience gainsExperience;

		public DeliversExperienceProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			deliversExperience = Self.Info.TraitInfo<DeliversExperienceInfo>();
			gainsExperience = Self.Trait<GainsExperience>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Deliver experience to the target actor.")]
		public void DeliverExperience(Actor target)
		{
			var targetGainsExperience = target.TraitOrDefault<GainsExperience>();
			if (targetGainsExperience == null)
				throw new LuaException("Actor '{0}' cannot gain experience!".F(target));

			if (targetGainsExperience.Level == targetGainsExperience.MaxLevel)
				return;

			var level = gainsExperience.Level;
			var t = Target.FromActor(target);

			// NB: Scripted actions get no visible targetlines.
			Self.QueueActivity(new DonateExperience(Self, t, level, deliversExperience.PlayerExperience, null));
		}
	}
}
