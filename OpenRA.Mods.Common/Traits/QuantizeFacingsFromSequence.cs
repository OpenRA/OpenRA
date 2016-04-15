#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Derive facings from sprite body sequence.")]
	public class QuantizeFacingsFromSequenceInfo : UpgradableTraitInfo, IQuantizeBodyOrientationInfo, Requires<RenderSpritesInfo>
	{
		[Desc("Defines sequence to derive facings from."), SequenceReference]
		public readonly string Sequence = "idle";

		public int QuantizedBodyFacings(ActorInfo ai, SequenceProvider sequenceProvider, string race)
		{
			if (string.IsNullOrEmpty(Sequence))
				throw new InvalidOperationException("Actor " + ai.Name + " is missing sequence to quantize facings from.");

			var rsi = ai.TraitInfo<RenderSpritesInfo>();
			return sequenceProvider.GetSequence(rsi.GetImage(ai, sequenceProvider, race), Sequence).Facings;
		}

		public override object Create(ActorInitializer init) { return new QuantizeFacingsFromSequence(this); }
	}

	public class QuantizeFacingsFromSequence : UpgradableTrait<QuantizeFacingsFromSequenceInfo>
	{
		public QuantizeFacingsFromSequence(QuantizeFacingsFromSequenceInfo info)
			: base(info) { }
	}
}
