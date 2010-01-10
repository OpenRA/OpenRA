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
		public readonly TypeDictionary Traits = new TypeDictionary();
		public readonly string Name;

		public NewUnitInfo( string name, MiniYaml node )
		{
			Name = name;

			// todo: make inheritance actually work
			MiniYaml inherit;
			if( node.Nodes.TryGetValue( "Inherits", out inherit ) )
			{
				Parent = inherit.Value;
				node.Nodes.Remove( "Inherits" );
			}

			foreach (var t in node.Nodes)
				Traits.Add(LoadTraitInfo(t.Key, t.Value));
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
