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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Graphics
{
	public interface IActorPreview
	{
		void Tick();
		IEnumerable<IRenderable> Render(WorldRenderer wr, WPos pos);
	}

	public class ActorPreviewInitializer : IActorInitializer
	{
		public readonly ActorInfo Actor;
		public readonly WorldRenderer WorldRenderer;
		public World World { get { return WorldRenderer.World; } }

		readonly TypeDictionary dict;

		public ActorPreviewInitializer(ActorInfo actor, WorldRenderer worldRenderer, TypeDictionary dict)
		{
			Actor = actor;
			WorldRenderer = worldRenderer;
			this.dict = dict;
		}

		public T Get<T>() where T : IActorInit { return dict.Get<T>(); }
		public U Get<T, U>() where T : IActorInit<U> { return dict.Get<T>().Value(World); }
		public bool Contains<T>() where T : IActorInit { return dict.Contains<T>(); }

		public DamageState GetDamageState()
		{
			var health = dict.GetOrDefault<HealthInit>();

			if (health == null)
				return DamageState.Undamaged;

			var hf = health.Value(null);

			if (hf <= 0)
				return DamageState.Dead;

			if (hf < 0.25f)
				return DamageState.Critical;

			if (hf < 0.5f)
				return DamageState.Heavy;

			if (hf < 0.75f)
				return DamageState.Medium;

			if (hf < 1.0f)
				return DamageState.Light;

			return DamageState.Undamaged;
		}
	}
}
