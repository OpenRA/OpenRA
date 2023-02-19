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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public enum BlinkState { Off, On }

	public abstract class WithDecorationBaseInfo : ConditionalTraitInfo
	{
		[Desc("Position in the actor's selection box to draw the decoration.")]
		public readonly string Position = "TopLeft";

		[Desc("Player relationships who can view the decoration.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

		[Desc("Should this be visible only when selected?")]
		public readonly bool RequiresSelection = false;

		[Desc("Offset sprite center position from the selection box edge.")]
		public readonly int2 Margin = int2.Zero;

		[Desc("Screen-space offsets to apply when defined conditions are enabled.",
			"A dictionary of [condition string]: [x, y offset].")]
		public readonly Dictionary<BooleanExpression, int2> Offsets = new Dictionary<BooleanExpression, int2>();

		[Desc("The number of ticks that each step in the blink pattern in active.")]
		public readonly int BlinkInterval = 5;

		[Desc("A pattern of ticks (BlinkInterval long) where the decoration is visible or hidden.")]
		public readonly BlinkState[] BlinkPattern = Array.Empty<BlinkState>();

		[Desc("Override blink conditions to use when defined conditions are enabled.",
			"A dictionary of [condition string]: [pattern].")]
		public readonly Dictionary<BooleanExpression, BlinkState[]> BlinkPatterns = new Dictionary<BooleanExpression, BlinkState[]>();

		[ConsumedConditionReference]
		public IEnumerable<string> ConsumedConditions
		{
			get { return Offsets.Keys.Concat(BlinkPatterns.Keys).SelectMany(r => r.Variables).Distinct(); }
		}
	}

	public abstract class WithDecorationBase<InfoType> : ConditionalTrait<InfoType>, IDecoration where InfoType : WithDecorationBaseInfo
	{
		protected readonly Actor Self;
		int2 conditionalOffset;
		BlinkState[] blinkPattern;

		public WithDecorationBase(Actor self, InfoType info)
			: base(info)
		{
			Self = self;
			blinkPattern = info.BlinkPattern;
		}

		protected virtual bool ShouldRender(Actor self)
		{
			if (self.World.FogObscures(self))
				return false;

			if (blinkPattern != null && blinkPattern.Length > 0)
			{
				var i = self.World.WorldTick / Info.BlinkInterval % blinkPattern.Length;
				if (blinkPattern[i] != BlinkState.On)
					return false;
			}

			if (self.World.RenderPlayer != null)
			{
				var relationship = self.Owner.RelationshipWith(self.World.RenderPlayer);
				if (!Info.ValidRelationships.HasRelationship(relationship))
					return false;
			}

			return true;
		}

		bool IDecoration.RequiresSelection => Info.RequiresSelection;

		protected abstract IEnumerable<IRenderable> RenderDecoration(Actor self, WorldRenderer wr, int2 pos);

		IEnumerable<IRenderable> IDecoration.RenderDecoration(Actor self, WorldRenderer wr, ISelectionDecorations container)
		{
			if (IsTraitDisabled || self.IsDead || !self.IsInWorld || !ShouldRender(self))
				return Enumerable.Empty<IRenderable>();

			var screenPos = container.GetDecorationOrigin(self, wr, Info.Position, Info.Margin) + conditionalOffset;
			return RenderDecoration(self, wr, screenPos);
		}

		public override IEnumerable<VariableObserver> GetVariableObservers()
		{
			foreach (var observer in base.GetVariableObservers())
				yield return observer;

			foreach (var condition in Info.Offsets.Keys)
				yield return new VariableObserver(OffsetConditionChanged, condition.Variables);

			foreach (var condition in Info.BlinkPatterns.Keys)
				yield return new VariableObserver(BlinkConditionsChanged, condition.Variables);
		}

		void OffsetConditionChanged(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			conditionalOffset = int2.Zero;
			foreach (var kv in Info.Offsets)
			{
				if (kv.Key.Evaluate(conditions))
				{
					conditionalOffset = kv.Value;
					break;
				}
			}
		}

		void BlinkConditionsChanged(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			blinkPattern = Info.BlinkPattern;
			foreach (var kv in Info.BlinkPatterns)
			{
				if (kv.Key.Evaluate(conditions))
				{
					blinkPattern = kv.Value;
					return;
				}
			}
		}
	}
}
