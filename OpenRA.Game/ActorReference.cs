#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Collections;
using System.IO;

namespace OpenRA.FileFormats
{
	public class ActorReference : IEnumerable
	{
		public readonly string Type;
		public readonly TypeDictionary InitDict;

		public ActorReference( string type ) : this( type, new Dictionary<string, MiniYaml>() ) { }

		public ActorReference( string type, Dictionary<string, MiniYaml> inits )
		{
			if (!Rules.Info.ContainsKey(type))
				throw new InvalidDataException("Unknown actor: `{0}'".F(type));
			
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
				ret.Nodes.Add( initName.Substring( 0, initName.Length - 4 ), FieldSaver.Save( init ) );
			}
			return ret;
		}

		// for initialization syntax
		public void Add( object o ) { InitDict.Add( o ); }
		public IEnumerator GetEnumerator() { return InitDict.GetEnumerator(); }
	}
}
