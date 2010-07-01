using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Traits
{
	/* attributes used by RALint to understand the rules */

	[AttributeUsage(AttributeTargets.Field)]
	public class ActorReferenceAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Field)]
	public class WeaponReferenceAttribute : Attribute { }
}
