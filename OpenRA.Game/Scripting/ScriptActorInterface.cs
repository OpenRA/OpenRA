#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;

namespace OpenRA.Scripting
{
	public class ScriptActorInterface : ScriptObjectWrapper
	{
		readonly Actor actor;

		protected override string DuplicateKeyError(string memberName) { return "Actor '{0}' defines the command '{1}' on multiple traits".F(actor.Info.Name, memberName); }
		protected override string MemberNotFoundError(string memberName)
		{
			var actorName = actor.Info.Name;
			if (actor.IsDead)
				actorName += " (dead)";

			return "Actor '{0}' does not define a property '{1}'".F(actorName, memberName);
		}

		public ScriptActorInterface(ScriptContext context, Actor actor)
			: base(context)
		{
			this.actor = actor;

			InitializeBindings();
		}

		void InitializeBindings()
		{
			var commandClasses = Context.ActorCommands[actor.Info].AsEnumerable();

			// Destroyed actors cannot have their traits queried
			if (actor.Disposed)
				commandClasses = commandClasses.Where(c => c.HasAttribute<ExposedForDestroyedActors>());

			var args = new object[] { Context, actor };
			var objects = commandClasses.Select(cg =>
			{
				var groupCtor = cg.GetConstructor(new Type[] { typeof(ScriptContext), typeof(Actor) });
				return groupCtor.Invoke(args);
			});

			Bind(objects);
		}

		public void OnActorDestroyed()
		{
			// Regenerate bindings to remove access to bogus trait state
			InitializeBindings();
		}
	}
}
