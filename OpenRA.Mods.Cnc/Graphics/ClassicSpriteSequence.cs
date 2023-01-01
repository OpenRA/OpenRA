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
		[Desc("Incorporate a compensation factor for the rotational distortion present in the first-generation Westwood games.")]
		static readonly SpriteSequenceField<bool> UseClassicFacings = new SpriteSequenceField<bool>(nameof(UseClassicFacings), false);
		readonly bool useClassicFacings;

		public ClassicSpriteSequence(ModData modData, string tileSet, SpriteCache cache, ISpriteSequenceLoader loader, string sequence, string animation, MiniYaml info)
			: base(modData, tileSet, cache, loader, sequence, animation, info)
		{
			var d = info.ToDictionary();
			useClassicFacings = LoadField(d, UseClassicFacings);

			if (useClassicFacings && facings != 32)
				throw new InvalidOperationException(
					$"{info.Nodes[0].Location}: Sequence {sequence}.{animation}: UseClassicFacings is only valid for 32 facings");
		}

		protected override int GetFacingFrameOffset(WAngle facing)
		{
			return useClassicFacings ? Util.ClassicIndexFacing(facing, facings) : Common.Util.IndexFacing(facing, facings);
		}
	}
}
