using System.Collections.Generic;

namespace OpenRa.Game.Traits
{
	class FakeInfo : ITraitInfo
	{
		public object Create(Actor self) { return new Fake(self); }
	}

	class Fake : ITags
	{
		public Fake(Actor self){}
		
		public IEnumerable<TagType> GetTags()
		{
			yield return TagType.Fake;
		}
	}
}
