using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA
{
	class TraitCreator
	{
		public static object Create( string traitName, World world, Actor actor, ITraitInfo info, TypeDictionary init )
		{
			var argsDict = new Dictionary<string, object>
				{
					{ "world", world },
					{ "self", actor },
					{ "initDict", init },
					{ "init", new ActorInitializer( actor, init ) },
					{ "info", info },
				};
			return Game.modData.ObjectCreator.CreateObject<object>( traitName, argsDict );
		}
	}
}
