#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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

	public class ClassicSpriteSequence : DefaultSpriteSequence
	{
		readonly bool useClassicFacings;

		public ClassicSpriteSequence(ModData modData, string tileSet, SpriteCache cache, ISpriteSequenceLoader loader, string sequence, string animation, MiniYaml info)
			: base(modData, tileSet, cache, loader, sequence, animation, info)
		{
			var d = info.ToDictionary();
			useClassicFacings = LoadField(d, "UseClassicFacings", false);

			if (useClassicFacings && Facings != 32)
				throw new InvalidOperationException(
					"{0}: Sequence {1}.{2}: UseClassicFacings is only valid for 32 facings"
					.F(info.Nodes[0].Location, sequence, animation));
		}

		protected override int GetFacingFrameOffset(WAngle facing)
		{
			return useClassicFacings ? Util.ClassicIndexFacing(facing, Facings) : Common.Util.IndexFacing(facing, Facings);
		}
	}
}
