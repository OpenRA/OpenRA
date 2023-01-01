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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Applies a condition to connected line build segments.")]
	public class LineBuildSegmentExternalConditionInfo : ConditionalTraitInfo, Requires<LineBuildInfo>
	{
		[FieldLoader.Require]
		[Desc("The condition to apply. Must be included in the target actor's ExternalConditions list.")]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new LineBuildSegmentExternalCondition(this); }
	}

	public class LineBuildSegmentExternalCondition : ConditionalTrait<LineBuildSegmentExternalConditionInfo>, INotifyLineBuildSegmentsChanged
	{
		readonly HashSet<Actor> segments = new HashSet<Actor>();
		readonly Dictionary<Actor, int> tokens = new Dictionary<Actor, int>();

		public LineBuildSegmentExternalCondition(LineBuildSegmentExternalConditionInfo info)
			: base(info) { }

		void GrantCondition(Actor self, Actor segment)
		{
			if (tokens.ContainsKey(segment))
				return;

			var external = segment.TraitsImplementing<ExternalCondition>()
				.FirstOrDefault(t => t.Info.Condition == Info.Condition && t.CanGrantCondition(segment));

			if (external != null)
				tokens[segment] = external.GrantCondition(segment, self);
		}

		void RevokeCondition(Actor self, Actor segment)
		{
			if (!tokens.TryGetValue(segment, out var token))
				return;

			tokens.Remove(segment);
			if (segment.Disposed)
				return;

			foreach (var external in segment.TraitsImplementing<ExternalCondition>())
				if (external.TryRevokeCondition(segment, self, token))
					break;
		}

		void INotifyLineBuildSegmentsChanged.SegmentAdded(Actor self, Actor segment)
		{
			segments.Add(segment);
			if (!IsTraitDisabled)
				GrantCondition(self, segment);
		}

		void INotifyLineBuildSegmentsChanged.SegmentRemoved(Actor self, Actor segment)
		{
			if (!IsTraitDisabled)
				RevokeCondition(self, segment);
			segments.Remove(segment);
		}

		protected override void TraitEnabled(Actor self)
		{
			foreach (var s in segments)
				GrantCondition(self, s);
		}

		protected override void TraitDisabled(Actor self)
		{
			foreach (var s in segments)
				RevokeCondition(self, s);
		}
	}
}
