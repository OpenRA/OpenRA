using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Traits.Render
{
	class RenderFlareInfo : RenderSimpleInfo
	{
		public override object Create(Actor self) { return new RenderFlare(self); }
	}

	class RenderFlare : RenderSimple
	{
		public RenderFlare(Actor self)
			: base(self, () => 0)
		{
			anim.PlayThen("open", () => anim.PlayRepeating("idle"));
		}
	}
}
