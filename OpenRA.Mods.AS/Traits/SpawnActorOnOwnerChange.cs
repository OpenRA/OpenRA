#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Spawns a proxy actor when this actor changes ownership.")]
	public class SpawnProxyActorOnOwnerChangeInfo : ConditionalTraitInfo
	{
		[ActorReference, FieldLoader.Require]
		public readonly string ProxyActor = null;

		public override object Create(ActorInitializer init) { return new SpawnProxyActorOnOwnerChange(this); }
	}

	public class SpawnProxyActorOnOwnerChange : ConditionalTrait<SpawnProxyActorOnOwnerChangeInfo>, INotifyOwnerChanged
	{
		readonly SpawnProxyActorOnOwnerChangeInfo info;

		public SpawnProxyActorOnOwnerChange(SpawnProxyActorOnOwnerChangeInfo info)
			: base(info)
		{
			this.info = info;
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (IsTraitDisabled)
				return;

			self.World.AddFrameEndTask(w => w.CreateActor(info.ProxyActor, new TypeDictionary
			{
				new OwnerInit(newOwner)
			}));
		}
	}
}
