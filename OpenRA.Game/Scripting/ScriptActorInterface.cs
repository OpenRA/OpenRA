#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Eluant;
using Eluant.ObjectBinding;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Scripting
{
	public class ScriptActorInterface : ScriptObjectWrapper
	{
		readonly Actor actor;

		protected override string DuplicateKeyError(string memberName) { return "Actor '{0}' defines the command '{1}' on multiple traits".F(actor.Info.Name, memberName); }
		protected override string MemberNotFoundError(string memberName) { return "Actor '{0}' does not define a property '{1}'".F(actor.Info.Name, memberName); }

		public ScriptActorInterface(ScriptContext context, Actor actor)
			: base(context)
		{
			this.actor = actor;

			var args = new [] { actor };
			var objects = context.ActorCommands[actor.Info].Select(cg =>
			{
				var groupCtor = cg.GetConstructor(new Type[] { typeof(Actor) });
				return groupCtor.Invoke(args);
			});

			Bind(objects);
		}
	}
}
