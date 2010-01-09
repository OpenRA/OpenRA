using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.FileFormats;

namespace OpenRa.Game.GameRules
{
	class NewUnitInfo
	{
		public readonly string Parent;
		public readonly Dictionary<string, MiniYaml> Traits = new Dictionary<string, MiniYaml>();

		public NewUnitInfo( MiniYaml node )
		{
			MiniYaml inherit;
			if( node.Nodes.TryGetValue( "Inherits", out inherit ) )
			{
				Parent = inherit.Value;
				node.Nodes.Remove( "Inherits" );
			}
			Traits = node.Nodes;
		}
	}
}
