using System.Collections.Generic;

namespace OpenRa.Traits
{
	class FakeInfo : StatelessTraitInfo<Fake> { }

	class Fake : ITags
	{
		public IEnumerable<TagType> GetTags() {	yield return TagType.Fake; }
	}
}
