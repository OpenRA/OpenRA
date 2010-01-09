using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.FileFormats;
using OpenRa.Game.Traits;

namespace OpenRa.Game.GameRules
{
	class NewUnitInfo
	{
		public readonly string Parent;
		public readonly Dictionary<string, ITraitInfo> Traits;

		public NewUnitInfo( MiniYaml node )
		{
			MiniYaml inherit;
			if( node.Nodes.TryGetValue( "Inherits", out inherit ) )
			{
				Parent = inherit.Value;
				node.Nodes.Remove( "Inherits" );
			}

			Traits = node.Nodes.ToDictionary( 
				a => a.Key, 
				a => LoadTraitInfo( a.Key, a.Value ));
		}

		static ITraitInfo LoadTraitInfo(string traitName, MiniYaml my)
		{
			var fullTypeName = typeof(ITraitInfo).Namespace + "." + traitName + "Info";
			var info = (ITraitInfo)typeof(ITraitInfo).Assembly.CreateInstance(fullTypeName);

			if (info == null)
				throw new NotImplementedException("Missing traitinfo type `{0}`".F(fullTypeName));

			FieldLoader.Load(info, my);
			return info;
		}
	}
}
