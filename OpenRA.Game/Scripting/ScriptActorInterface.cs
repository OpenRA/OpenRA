#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;

namespace OpenRA.Scripting
{
	public class ScriptActorInterface : ScriptObjectWrapper
	{
		readonly Actor actor;

		protected override string DuplicateKeyError(string memberName) { return $"Actor '{actor.Info.Name}' defines the command '{memberName}' on multiple traits"; }
		protected override string MemberNotFoundError(string memberName)
		{
			var actorName = actor.Info.Name;
			if (actor.IsDead)
				actorName += " (dead)";

			return $"Actor '{actorName}' does not define a property '{memberName}'";
		}

		public ScriptActorInterface(ScriptContext context, Actor actor)
			: base(context)
		{
			this.actor = actor;

			InitializeBindings();
		}

		void InitializeBindings()
		{
			var commandClasses = Context.ActorCommands[actor.Info];

			// Destroyed actors cannot have their traits queried. In rare cases the actor may have already been destroyed.
			if (actor.Disposed)
				commandClasses = commandClasses.Where(c => c.HasAttribute<ExposedForDestroyedActors>()).ToArray();

			Bind(CreateObjects(commandClasses, new object[] { Context, actor }));
		}

		public void OnActorDestroyed()
		{
			// Remove bindings not available to destroyed actors.
			foreach (var commandClass in Context.ActorCommands[actor.Info])
				if (!commandClass.HasAttribute<ExposedForDestroyedActors>())
					Unbind(commandClass);
		}
	}
}
