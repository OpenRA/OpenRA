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
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.RA.Graphics
{
	public interface IActorPreview
	{
		void Tick();
		IEnumerable<IRenderable> Render(WorldRenderer wr, WPos pos);
	}

	public class ActorPreviewInitializer
	{
		public readonly ActorInfo Actor;
		public readonly Player Owner;
		public readonly WorldRenderer WorldRenderer;
		public World World { get { return WorldRenderer.world; } }

		readonly TypeDictionary dict;

		public ActorPreviewInitializer(ActorInfo actor, Player owner, WorldRenderer worldRenderer, TypeDictionary dict)
		{
			Actor = actor;
			Owner = owner;
			WorldRenderer = worldRenderer;
			this.dict = dict;
		}

		public T Get<T>() where T : IActorInit { return dict.Get<T>(); }
		public U Get<T, U>() where T : IActorInit<U> { return dict.Get<T>().Value(World); }
		public bool Contains<T>() where T : IActorInit { return dict.Contains<T>(); }
	}
}
