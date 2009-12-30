using System.Collections.Generic;

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
