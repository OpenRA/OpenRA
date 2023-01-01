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
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actor can reveal Cloak actors in a specified range.")]
	public class DetectCloakedInfo : ConditionalTraitInfo
	{
		[Desc("Specific cloak classifications I can reveal.")]
		public readonly BitSet<DetectionType> DetectionTypes = new BitSet<DetectionType>("Cloak");

		public readonly WDist Range = WDist.FromCells(5);

		public override object Create(ActorInitializer init) { return new DetectCloaked(this); }
	}

	public class DetectCloaked : ConditionalTrait<DetectCloakedInfo>
	{
		IDetectCloakedModifier[] rangeModifiers;

		public DetectCloaked(DetectCloakedInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			rangeModifiers = self.TraitsImplementing<IDetectCloakedModifier>().ToArray();
			base.Created(self);
		}

		public WDist Range
		{
			get
			{
				if (IsTraitDisabled)
					return WDist.Zero;

				var detectCloakedModifier = rangeModifiers.Select(x => x.GetDetectCloakedModifier());
				var range = Util.ApplyPercentageModifiers(Info.Range.Length, detectCloakedModifier);
				return new WDist(range);
			}
		}
	}
}
