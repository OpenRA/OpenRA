using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IjwFramework.Types;
using IjwFramework.Collections;

namespace OpenRa.GameRules
{
	class VoiceInfo
	{
		public readonly string[] SovietVariants = { ".aud" };
		public readonly string[] AlliedVariants = { ".aud" };
		public readonly string[] Select = { };
		public readonly string[] Move = { };
		public readonly string[] Attack = null;
		public readonly string[] Die = { };

		public readonly Lazy<Dictionary<string, VoicePool>> Pools;

		public VoiceInfo()
		{
			Pools = Lazy.New(() =>
				new Dictionary<string, VoicePool>
				{
					{ "Select", new VoicePool(Select) },
					{ "Move", new VoicePool(Move) },
					{ "Attack", new VoicePool( Attack ?? Move ) },
					{ "Die", new VoicePool(Die) },
				});
		}
	}
}
