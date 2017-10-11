#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("When created, this actor kills all actors with this trait owned by it's owner.")]
	public class DoctrineInfo : ITraitInfo
	{
		[Desc("Type of the doctrine. If empty, it falls back to the actor's type.")]
		public readonly string Type = null;

		public object Create(ActorInitializer init) { return new Doctrine(init.Self, this); }
	}

	public class Doctrine : INotifyCreated
	{
		public readonly string Type;

		public Doctrine(Actor self, DoctrineInfo info)
		{
			Type = string.IsNullOrEmpty(info.Type) ? self.Info.Name : info.Type;
		}

		void INotifyCreated.Created(Actor self)
		{
			var actors = self.World.ActorsWithTrait<Doctrine>().Where(x => x.Trait.Type == Type && x.Actor.Owner == self.Owner && x.Actor != self);

			foreach (var a in actors)
			{
				a.Actor.Kill(a.Actor);
			}
		}
	}
}
