using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRa.Game.Traits
{
	class Fake : ITags
	{
		public Fake(Actor self){}
		
		public IEnumerable<TagType> GetTags()
		{
			yield return TagType.Fake;
		}
	}
}
