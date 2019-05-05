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
using OpenRA.Primitives;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Ability")]
	public class DeliversCashProperties : ScriptActorProperties, Requires<IMoveInfo>, Requires<DeliversCashInfo>
	{
		readonly DeliversCash deliversCash;

		public DeliversCashProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			deliversCash = Self.Trait<DeliversCash>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Deliver cash to the target actor.")]
		public void DeliverCash(Actor target)
		{
			var acceptsDeliveredCash = target.TraitOrDefault<AcceptsDeliveredCash>();
			if (acceptsDeliveredCash == null || acceptsDeliveredCash.IsTraitDisabled)
				throw new LuaException("Actor '{0}' does not accept delivered cash!".F(target));

			if (deliversCash.IsTraitDisabled)
				throw new LuaException("Actor '{0}' does not deliver cash!".F(Self));

			var t = Target.FromActor(target);
			Self.QueueActivity(new DonateCash(Self, t, deliversCash));
		}
	}

	[ScriptPropertyGroup("Ability")]
	public class DeliversExperienceProperties : ScriptActorProperties, Requires<IMoveInfo>, Requires<DeliversExperienceInfo>
	{
		readonly DeliversExperience deliversExperience;
		readonly GainsExperience gainsExperience;

		public DeliversExperienceProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			deliversExperience = Self.Trait<DeliversExperience>();
			gainsExperience = Self.Trait<GainsExperience>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Deliver experience to the target actor.")]
		public void DeliverExperience(Actor target)
		{
			var targetGainsExperience = target.TraitOrDefault<GainsExperience>();
			var acceptsDeliveredExperience = target.TraitOrDefault<AcceptsDeliveredExperience>();
			if (targetGainsExperience == null)
				throw new LuaException("Actor '{0}' cannot gain experience!".F(target));

			if (acceptsDeliveredExperience == null || acceptsDeliveredExperience.IsTraitDisabled)
				throw new LuaException("Actor '{0}' does not accept delivered experience!".F(target));

			if (deliversExperience.IsTraitDisabled)
				throw new LuaException("Actor '{0}' does not deliver experience!".F(Self));

			if (targetGainsExperience.Level == targetGainsExperience.MaxLevel)
				return;

			var level = gainsExperience.Level;

			var t = Target.FromActor(target);
			Self.QueueActivity(new DonateExperience(Self, t, level, deliversExperience));
		}
	}
}
