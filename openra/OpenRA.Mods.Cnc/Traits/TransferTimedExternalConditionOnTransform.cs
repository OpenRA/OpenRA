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

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("A special case trait that re-grants a timed external condition when this actor transforms.",
		"This trait does not work with permanently granted external conditions.",
		"This trait changes the external condition source, so cannot be used for conditions that may later be revoked")]
	public class TransferTimedExternalConditionOnTransformInfo : TraitInfo, Requires<TransformsInfo>
	{
		[FieldLoader.Require]
		[Desc("External condition to transfer")]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new TransferTimedExternalConditionOnTransform(this); }
	}

	public class TransferTimedExternalConditionOnTransform : IConditionTimerWatcher, INotifyTransform
	{
		readonly TransferTimedExternalConditionOnTransformInfo info;
		int duration = 0;
		int remaining = 0;

		public TransferTimedExternalConditionOnTransform(TransferTimedExternalConditionOnTransformInfo info)
		{
			this.info = info;
		}

		void INotifyTransform.BeforeTransform(Actor self) { }
		void INotifyTransform.OnTransform(Actor self) { }

		void INotifyTransform.AfterTransform(Actor toActor)
		{
			if (remaining <= 0)
				return;

			var external = toActor.TraitsImplementing<ExternalCondition>()
				.FirstOrDefault(t => t.Info.Condition == info.Condition && t.CanGrantCondition(this));

			external?.GrantCondition(toActor, this, duration, remaining);
		}

		void IConditionTimerWatcher.Update(int duration, int remaining)
		{
			this.duration = duration;
			this.remaining = remaining;
		}

		string IConditionTimerWatcher.Condition => info.Condition;
	}
}
