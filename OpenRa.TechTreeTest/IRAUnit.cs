using System;
using System.Collections.Generic;
using System.Drawing;
namespace OpenRa.TechTreeTest
{
	interface IRAUnit
	{
		bool Buildable { get; }
		void CheckPrerequisites(IEnumerable<string> units, Race currentRace);
		string FriendlyName { get; }
		Bitmap Icon { get; }
		Race Owner { get; set; }
		string[] Prerequisites { get; set; }
		bool ShouldMakeBuildable(IEnumerable<string> units);
		bool ShouldMakeUnbuildable(IEnumerable<string> units);
		string Tag { get; }
		int TechLevel { get; set; }
	}
}
