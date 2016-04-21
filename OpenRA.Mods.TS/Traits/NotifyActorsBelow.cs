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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.TS.Traits
{
	[Desc("Actor notifies other (ground) actors when positions overlap.")]
	public interface INotifyActorAbove
	{
		string NotifyActorType { get; }

		void NotifyActorAbove(Actor self, Actor above);
	}

	[Desc("Actor notifies other (ground) actors when positions overlap.")]
	public class NotifyActorsBelowInfo : ITraitInfo
	{
		[Desc("Which actor types to notify, e.g. building")]
		public readonly HashSet<string> NotifyActorTypes = new HashSet<string>();

		public virtual object Create(ActorInitializer init) { return new NotifyActorsBelow(init, this); }
	}

	public class NotifyActorsBelow : ITick
	{
		readonly NotifyActorsBelowInfo info;

		public NotifyActorsBelow(ActorInitializer init, NotifyActorsBelowInfo info)
		{
			this.info = info;
		}

		public void Tick(Actor self)
		{
			self.World.ActorMap.GetActorsAt(self.Location)
				.Where(a => a != self)
				.Do(a => a.TraitsImplementing<INotifyActorAbove>()
					.Where(t => Exts.IsTraitEnabled(t) && info.NotifyActorTypes.Contains(t.NotifyActorType))
					.Do(t => t.NotifyActorAbove(a, self)));
		}
	}
}
