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

using System;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;

namespace OpenRA.Mods.Cnc.Graphics
{
	public class ClassicSpriteSequenceLoader : DefaultSpriteSequenceLoader
	{
		public ClassicSpriteSequenceLoader(ModData modData)
			: base(modData) { }

		public override ISpriteSequence CreateSequence(ModData modData, string tileSet, SpriteCache cache, string sequence, string animation, MiniYaml info)
		{
			return new ClassicSpriteSequence(modData, tileSet, cache, this, sequence, animation, info);
		}
	}

	[Desc("A sprite sequence that has the oddities that come with first-generation Westwood titles.")]
	public class ClassicSpriteSequence : DefaultSpriteSequence
	{
		// This needs to be a public property for the documentation generation to work.
		[Desc("Incorporate a compensation factor due to the distortion caused by 3D-Studio " +
		      "when it tried to render 45% angles which was used by Westwood Studios at that time.")]
		public bool UseClassicFacings { get; }

		public ClassicSpriteSequence(ModData modData, string tileSet, SpriteCache cache, ISpriteSequenceLoader loader, string sequence, string animation, MiniYaml info)
			: base(modData, tileSet, cache, loader, sequence, animation, info)
		{
			var d = info.ToDictionary();
			UseClassicFacings = LoadField(d, nameof(UseClassicFacings), UseClassicFacings);

			if (UseClassicFacings && Facings != 32)
				throw new InvalidOperationException(
					$"{info.Nodes[0].Location}: Sequence {sequence}.{animation}: UseClassicFacings is only valid for 32 facings");
		}

		protected override int GetFacingFrameOffset(WAngle facing)
		{
			return UseClassicFacings ? Util.ClassicIndexFacing(facing, Facings) : Common.Util.IndexFacing(facing, Facings);
		}
	}
}
