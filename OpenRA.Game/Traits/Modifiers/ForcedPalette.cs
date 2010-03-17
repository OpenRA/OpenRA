using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Traits
{
	class ForcedPaletteInfo : ITraitInfo
	{
		public readonly string Palette = null;
		public object Create(Actor self) { return new ForcedPalette(this); }
	}

	class ForcedPalette : IRenderModifier
	{
		string palette;

		public ForcedPalette(ForcedPaletteInfo info) { palette = info.Palette; }

		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			return r.Select(rr => rr.WithPalette(palette));
		}
	}
}
