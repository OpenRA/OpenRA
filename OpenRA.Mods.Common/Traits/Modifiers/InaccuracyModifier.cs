#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Modifies the inaccuracy of weapons fired by this actor by a flat amount.")]
	public class InaccuracyModifierInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Amount to increase to inaccuracy (negative to decrease).")]
		public readonly WDist Modifier = WDist.Zero;

		[Desc("Higher priority modifiers are applied first.")]
		public readonly int Priority = 0;

		public override object Create(ActorInitializer init) { return new InaccuracyModifier(this); }
	}

	public class InaccuracyModifier : ConditionalTrait<InaccuracyModifierInfo>, IInaccuracyModifier
	{
		public InaccuracyModifier(InaccuracyModifierInfo info)
			: base(info) { }

		IModifier IInaccuracyModifier.GetInaccuracyModifier()
		{
			var modifier = new Modifier
			{
				Type = ModifierType.Absolute,
				Priority = Info.Priority,
				Value = IsTraitDisabled ? 0 : Info.Modifier.Length
			};

			return modifier;
		}
	}
}
