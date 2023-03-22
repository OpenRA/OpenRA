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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public class WithStoresResourcesPipsDecorationInfo : WithDecorationBaseInfo, Requires<IStoresResourcesInfo>
	{
		[FieldLoader.Require]
		[Desc("Number of pips to display how filled unit is.")]
		public readonly int PipCount = 0;

		[Desc("If non-zero, override the spacing between adjacent pips.")]
		public readonly int2 PipStride = int2.Zero;

		[Desc("Image that defines the pip sequences.")]
		public readonly string Image = "pips";

		[SequenceReference(nameof(Image))]
		[Desc("Sequence used for empty pips.")]
		public readonly string EmptySequence = "pip-empty";

		[SequenceReference(nameof(Image))]
		[Desc("Sequence used for full pips that aren't defined in ResourceSequences.")]
		public readonly string FullSequence = "pip-green";

		[SequenceReference(nameof(Image), dictionaryReference: LintDictionaryReference.Values)]
		[Desc("Pip sequence to use for specific resource types.")]
		public readonly Dictionary<string, string> ResourceSequences = new();

		[PaletteReference]
		public readonly string Palette = "chrome";

		public override object Create(ActorInitializer init) { return new WithStoresResourcesPipsDecoration(init.Self, this); }
	}

	public class WithStoresResourcesPipsDecoration : WithDecorationBase<WithStoresResourcesPipsDecorationInfo>
	{
		readonly IStoresResources storesResources;
		readonly Animation pips;

		public WithStoresResourcesPipsDecoration(Actor self, WithStoresResourcesPipsDecorationInfo info)
			: base(self, info)
		{
			// TODO: allow to choose which stores resources trait to target.
			storesResources = self.TraitsImplementing<IStoresResources>().First();
			pips = new Animation(self.World, info.Image);
		}

		string GetPipSequence(int i)
		{
			var n = i * storesResources.Capacity / Info.PipCount;

			foreach (var rt in storesResources.Contents)
			{
				if (n < rt.Value)
				{
					if (!Info.ResourceSequences.TryGetValue(rt.Key, out var sequence))
						sequence = Info.FullSequence;

					return sequence;
				}

				n -= rt.Value;
			}

			return Info.EmptySequence;
		}

		protected override IEnumerable<IRenderable> RenderDecoration(Actor self, WorldRenderer wr, int2 screenPos)
		{
			pips.PlayRepeating(Info.EmptySequence);

			var palette = wr.Palette(Info.Palette);
			var pipSize = pips.Image.Size.XY.ToInt2();
			var pipStride = Info.PipStride != int2.Zero ? Info.PipStride : new int2(pipSize.X, 0);

			screenPos -= pipSize / 2;
			for (var i = 0; i < Info.PipCount; i++)
			{
				pips.PlayRepeating(GetPipSequence(i));
				yield return new UISpriteRenderable(pips.Image, self.CenterPosition, screenPos, 0, palette);

				screenPos += pipStride;
			}
		}
	}
}
