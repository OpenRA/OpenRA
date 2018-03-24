#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	class InfiltrateForProxyActorInfo : ConditionalTraitInfo
	{
		[ActorReference, FieldLoader.Require]
		public readonly string ProxyActor = null;

		public readonly HashSet<string> Types = new HashSet<string>();

		public override object Create(ActorInitializer init) { return new InfiltrateForProxyActor(this); }
	}

	class InfiltrateForProxyActor : ConditionalTrait<InfiltrateForProxyActorInfo>, INotifyInfiltrated
	{
		readonly InfiltrateForProxyActorInfo info;

		public InfiltrateForProxyActor(InfiltrateForProxyActorInfo info)
			: base(info)
		{
			this.info = info;
		}

		void INotifyInfiltrated.Infiltrated(Actor self, Actor infiltrator, BitSet<TargetableType> types)
		{
			if (IsTraitDisabled || !info.Types.Overlaps(types))
				return;

			infiltrator.World.AddFrameEndTask(w => w.CreateActor(info.ProxyActor, new TypeDictionary
			{
				new OwnerInit(infiltrator.Owner)
			}));
		}
	}
}
