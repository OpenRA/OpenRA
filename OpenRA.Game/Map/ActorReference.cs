#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections;
using System.Collections.Generic;
using OpenRA.Primitives;

namespace OpenRA
{
	public class ActorReference : IEnumerable
	{
		public string Type;
		public TypeDictionary InitDict;

		public ActorReference( string type ) : this( type, new Dictionary<string, MiniYaml>() ) { }

		public ActorReference( string type, Dictionary<string, MiniYaml> inits )
		{
			Type = type;
			InitDict = new TypeDictionary();
			foreach( var i in inits )
				InitDict.Add( LoadInit( i.Key, i.Value ) );
		}

		static IActorInit LoadInit(string traitName, MiniYaml my)
		{
			var info = Game.CreateObject<IActorInit>(traitName + "Init");
			FieldLoader.Load(info, my);
			return info;
		}

		public MiniYaml Save()
		{
			var ret = new MiniYaml( Type );
			foreach( var init in InitDict )
			{
				var initName = init.GetType().Name;
				ret.Nodes.Add( new MiniYamlNode( initName.Substring( 0, initName.Length - 4 ), FieldSaver.Save( init ) ) );
			}
			return ret;
		}

		// for initialization syntax
		public void Add( object o ) { InitDict.Add( o ); }
		public IEnumerator GetEnumerator() { return InitDict.GetEnumerator(); }
	}
}
