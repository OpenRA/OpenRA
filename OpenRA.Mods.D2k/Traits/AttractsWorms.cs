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

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("This actor makes noise, which causes them to be targeted by actors with the Sandworm trait.")]
	public class AttractsWormsInfo : ConditionalTraitInfo
	{
		[Desc("How much noise this actor produces.")]
		public readonly int Intensity = 0;

		[Desc("Noise percentage at Range step away from the actor.")]
		public readonly int[] Falloff = { 100, 100, 25, 11, 6, 4, 3, 2, 1, 0 };

		[Desc("Range between falloff steps.")]
		public readonly WDist Spread = new WDist(3072);

		[Desc("Ranges at which each Falloff step is defined. Overrides Spread.")]
		public readonly WDist[] Range = null;

		public override object Create(ActorInitializer init) { return new AttractsWorms(init, this); }
	}

	public class AttractsWorms : ConditionalTrait<AttractsWormsInfo>
	{
		readonly Actor self;
		readonly WDist[] effectiveRange;

		public AttractsWorms(ActorInitializer init, AttractsWormsInfo info)
			: base(info)
		{
			self = init.Self;

			effectiveRange = info.Range ?? Exts.MakeArray(info.Falloff.Length, i => i * info.Spread);
		}

		int GetNoisePercentageAtDistance(int distance)
		{
			var inner = effectiveRange[0].Length;
			for (var i = 1; i < effectiveRange.Length; i++)
			{
				var outer = effectiveRange[i].Length;
				if (outer > distance)
					return int2.Lerp(Info.Falloff[i - 1], Info.Falloff[i], distance - inner, outer - inner);

				inner = outer;
			}

			return 0;
		}

		public WVec AttractionAtPosition(WPos pos)
		{
			if (IsTraitDisabled)
				return WVec.Zero;

			var distance = self.CenterPosition - pos;
			var length = distance.Length;

			// Actor is too far to hear anything.
			if (length > effectiveRange[effectiveRange.Length - 1].Length)
				return WVec.Zero;

			var direction = 1024 * distance / length;
			var percentage = GetNoisePercentageAtDistance(length);

			return direction * Info.Intensity * percentage / 100;
		}
	}
}
