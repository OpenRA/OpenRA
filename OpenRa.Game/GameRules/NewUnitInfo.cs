using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.FileFormats;
using OpenRa.Game.Traits;

namespace OpenRa.Game.GameRules
{
	class NewUnitInfo
	{
		public readonly TypeDictionary Traits = new TypeDictionary();
		public readonly string Name;

		public NewUnitInfo( string name, MiniYaml node, Dictionary<string, MiniYaml> allUnits )
		{
			Name = name;

			foreach( var t in MergeWithParent( node, allUnits ).Nodes )
				if( t.Key != "Inherits" )
					Traits.Add( LoadTraitInfo( t.Key, t.Value ) );
		}

		static MiniYaml GetParent( MiniYaml node, Dictionary<string, MiniYaml> allUnits )
		{
			MiniYaml inherits;
			node.Nodes.TryGetValue( "Inherits", out inherits );
			if( inherits == null || string.IsNullOrEmpty( inherits.Value ) )
				return null;

			MiniYaml parent;
			allUnits.TryGetValue( inherits.Value, out parent );
			if( parent == null )
				return null;

			return parent;
		}

		static MiniYaml MergeWithParent( MiniYaml node, Dictionary<string, MiniYaml> allUnits )
		{
			var parent = GetParent( node, allUnits );
			if( parent != null )
				return MiniYaml.Merge( node, MergeWithParent( parent, allUnits ) );
			return node;
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
