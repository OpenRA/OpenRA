using System;

namespace OpenRA.Traits
{
	/* attributes used by RALint to understand the rules */

	[AttributeUsage(AttributeTargets.Field)]
	public class ActorReferenceAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Field)]
	public class WeaponReferenceAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Field)]
	public class VoiceReferenceAttribute : Attribute { }
}
