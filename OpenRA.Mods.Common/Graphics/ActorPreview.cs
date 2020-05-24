#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
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
		IEnumerable<IRenderable> RenderUI(WorldRenderer wr, int2 pos, float scale);
		IEnumerable<Rectangle> ScreenBounds(WorldRenderer wr, WPos pos);
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

		public T GetOrDefault<T>(TraitInfo info) where T : IActorInit
		{
			return dict.GetOrDefault<T>();
		}

		public T Get<T>(TraitInfo info) where T : IActorInit
		{
			var init = GetOrDefault<T>(info);
			if (init == null)
				throw new InvalidOperationException("TypeDictionary does not contain instance of type `{0}`".F(typeof(T)));

			return init;
		}

		public U GetValue<T, U>(TraitInfo info) where T : IActorInit<U>
		{
			return Get<T>(info).Value;
		}

		public U GetValue<T, U>(TraitInfo info, U fallback) where T : IActorInit<U>
		{
			var init = GetOrDefault<T>(info);
			return init != null ? init.Value : fallback;
		}

		public bool Contains<T>(TraitInfo info) where T : IActorInit { return GetOrDefault<T>(info) != null; }

		public Func<WRot> GetOrientation()
		{
			var facingInfo = Actor.TraitInfoOrDefault<IFacingInfo>();
			if (facingInfo == null)
				return () => WRot.Zero;

			// Dynamic facing takes priority
			var dynamicInit = dict.GetOrDefault<DynamicFacingInit>();
			if (dynamicInit != null)
			{
				// TODO: Account for terrain slope
				var getFacing = dynamicInit.Value;
				return () => WRot.FromFacing(getFacing());
			}

			// Fall back to initial actor facing if an Init isn't available
			var facingInit = dict.GetOrDefault<FacingInit>();
			var facing = facingInit != null ? facingInit.Value : facingInfo.GetInitialFacing();
			var orientation = WRot.FromFacing(facing);
			return () => orientation;
		}

		public Func<WAngle> GetFacing()
		{
			var facingInfo = Actor.TraitInfoOrDefault<IFacingInfo>();
			if (facingInfo == null)
				return () => WAngle.Zero;

			// Dynamic facing takes priority
			var dynamicInit = dict.GetOrDefault<DynamicFacingInit>();
			if (dynamicInit != null)
			{
				var getFacing = dynamicInit.Value;
				return () => WAngle.FromFacing(getFacing());
			}

			// Fall back to initial actor facing if an Init isn't available
			var facingInit = dict.GetOrDefault<FacingInit>();
			var facing = WAngle.FromFacing(facingInit != null ? facingInit.Value : facingInfo.GetInitialFacing());
			return () => facing;
		}

		public DamageState GetDamageState()
		{
			var health = dict.GetOrDefault<HealthInit>();

			if (health == null)
				return DamageState.Undamaged;

			var hf = health.Value;

			if (hf <= 0)
				return DamageState.Dead;

			if (hf < 25)
				return DamageState.Critical;

			if (hf < 50)
				return DamageState.Heavy;

			if (hf < 75)
				return DamageState.Medium;

			if (hf < 100)
				return DamageState.Light;

			return DamageState.Undamaged;
		}
	}
}
