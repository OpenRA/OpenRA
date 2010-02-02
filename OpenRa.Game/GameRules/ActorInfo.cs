using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.FileFormats;
using OpenRa.Traits;
using System.Reflection;
using IjwFramework.Types;
using System.IO;

namespace OpenRa.GameRules
{
	public class ActorInfo
	{
		public readonly string Name;
		public readonly string Category;
		public readonly TypeDictionary Traits = new TypeDictionary();

		public ActorInfo( string name, MiniYaml node, Dictionary<string, MiniYaml> allUnits )
		{
			var mergedNode = MergeWithParent( node, allUnits ).Nodes;

			Name = name;
			MiniYaml categoryNode;
			if( mergedNode.TryGetValue( "Category", out categoryNode ) )
				Category = categoryNode.Value;

			foreach( var t in mergedNode )
				if( t.Key != "Inherits" && t.Key != "Category" )
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

		static Pair<Assembly, string>[] ModAssemblies;
		public static void LoadModAssemblies(Manifest m)
		{
			var asms = new List<Pair<Assembly, string>>();

			// all the core stuff is in this assembly
			asms.Add(Pair.New(typeof(ITraitInfo).Assembly, typeof(ITraitInfo).Namespace));

			// add the mods
			foreach (var a in m.Assemblies)
				asms.Add(Pair.New(Assembly.LoadFile(Path.GetFullPath(a)), Path.GetFileNameWithoutExtension(a)));
			ModAssemblies = asms.ToArray();
		}

		static ITraitInfo LoadTraitInfo(string traitName, MiniYaml my)
		{
			if (traitName.Contains('@'))
				traitName = traitName.Substring(0, traitName.IndexOf('@'));

			foreach (var mod in ModAssemblies)
			{
				var fullTypeName = mod.Second + "." + traitName + "Info";
				var info = (ITraitInfo)mod.First.CreateInstance(fullTypeName);
				if (info == null) continue;
				FieldLoader.Load(info, my);
				return info;
			}

			throw new InvalidOperationException("Cannot locate trait: {0}".F(traitName));
		}

		public IEnumerable<ITraitInfo> TraitsInConstructOrder()
		{
			var ret = new List<ITraitInfo>();
			var t = Traits.WithInterface<ITraitInfo>().ToList();
			int index = 0;
			while( t.Count != 0 )
			{
				if( index >= t.Count )
					throw new InvalidOperationException( "Trait prerequisites not satisfied (or prerequisite loop)" );

				var prereqs = PrerequisitesOf( t[ index ] );
				if( prereqs.Count == 0 || prereqs.All( n => ret.Any( x => x.GetType().IsSubclassOf( n ) ) ) )
				{
					ret.Add( t[ index ] );
					t.RemoveAt( index );
					index = 0;
				}
				else
					++index;
			}

			return ret;
		}

		static List<Type> PrerequisitesOf( ITraitInfo info )
		{
			return info
				.GetType()
				.GetInterfaces()
				.Where( t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof( ITraitPrerequisite<> ) )
				.Select( t => t.GetGenericArguments()[ 0 ] )
				.ToList();
		}
	}
}
