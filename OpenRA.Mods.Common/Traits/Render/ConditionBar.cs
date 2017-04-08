#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Visualizes the remaining time for a condition.")]
	class ConditionBarInfo : ITraitInfo, Requires<ConditionManagerInfo>
	{
		[ConsumedConditionReference]
		[Desc("Condition expression used as the portion filled.")]
		public readonly ConditionExpression Value = null;

		[ConsumedConditionReference]
		[Desc("Condition expression used as the max fill.")]
		public readonly ConditionExpression MaxValue = null;

		public readonly Color Color = Color.Red;

		[Desc("Which diplomatic stances allow the player to see this bar.")]
		public readonly Stance DisplayStances = Stance.Ally;

		public object Create(ActorInitializer init) { return new ConditionBar(this); }
	}

	class ConditionBar : ISelectionBar, IConditionConsumerProvider
	{
		readonly ConditionBarInfo info;
		int value;
		int max;
		float fill;

		public ConditionBar(ConditionBarInfo info) { this.info = info; }

		IEnumerable<Pair<ConditionConsumer, IEnumerable<string>>> IConditionConsumerProvider.GetConsumersWithTheirConditions()
		{
			yield return new Pair<ConditionConsumer, IEnumerable<string>>(UpdateMax, info.MaxValue.Variables);
			yield return new Pair<ConditionConsumer, IEnumerable<string>>(UpdateValue, info.Value.Variables);
		}

		void UpdateMax(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			max = info.MaxValue.Evaluate(conditions);
			UpdateFill(self);
		}

		void UpdateValue(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			value = info.Value.Evaluate(conditions);
			UpdateFill(self);
		}

		void UpdateFill(Actor self)
		{
			var viewer = self.World.RenderPlayer ?? self.World.LocalPlayer;
			var visible = viewer != null && info.DisplayStances.HasStance(self.Owner.Stances[viewer]);
			fill = visible && max > 0 ? value * 1f / max : 0;
		}

		float ISelectionBar.GetValue() { return fill; }

		Color ISelectionBar.GetColor() { return info.Color; }

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }
	}
}
