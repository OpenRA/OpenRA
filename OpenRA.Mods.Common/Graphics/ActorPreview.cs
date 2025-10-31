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
		public World World => WorldRenderer.World;

		readonly ActorReference reference;

		public ActorPreviewInitializer(ActorInfo actor, WorldRenderer worldRenderer, TypeDictionary dict)
		{
			Actor = actor;
			WorldRenderer = worldRenderer;
			reference = new ActorReference(actor.Name.ToLowerInvariant(), dict);
		}

		public ActorPreviewInitializer(ActorReference actor, WorldRenderer worldRenderer)
		{
			Actor = worldRenderer.World.Map.Rules.Actors[actor.Type.ToLowerInvariant()];
			reference = actor;
			WorldRenderer = worldRenderer;
		}

		// Forward IActorInitializer queries to the actor reference
		// ActorReference can't reference a World instance, which prevents it from implementing this directly.
		public T GetOrDefault<T>(TraitInfo info) where T : ActorInit { return reference.GetOrDefault<T>(info); }
		public T Get<T>(TraitInfo info) where T : ActorInit { return reference.Get<T>(info); }
		public U GetValue<T, U>(TraitInfo info) where T : ValueActorInit<U> { return reference.GetValue<T, U>(info); }
		public U GetValue<T, U>(TraitInfo info, U fallback) where T : ValueActorInit<U> { return reference.GetValue<T, U>(info, fallback); }
		public bool Contains<T>(TraitInfo info) where T : ActorInit { return reference.Contains<T>(info); }
		public T GetOrDefault<T>() where T : ActorInit, ISingleInstanceInit { return reference.GetOrDefault<T>(); }
		public T Get<T>() where T : ActorInit, ISingleInstanceInit { return reference.Get<T>(); }
		public U GetValue<T, U>() where T : ValueActorInit<U>, ISingleInstanceInit { return reference.GetValue<T, U>(); }
		public U GetValue<T, U>(U fallback) where T : ValueActorInit<U>, ISingleInstanceInit { return reference.GetValue<T, U>(fallback); }
		public bool Contains<T>() where T : ActorInit, ISingleInstanceInit { return reference.Contains<T>(); }

		public Func<WRot> GetOrientation()
		{
			var facingInfo = Actor.TraitInfoOrDefault<IFacingInfo>();
			if (facingInfo == null)
				return () => WRot.None;

			WAngle facing;

			var mobileInfo = Actor.TraitInfoOrDefault<MobileInfo>();
			var location = reference.GetOrDefault<LocationInit>();
			if (location == null || mobileInfo == null || mobileInfo.TerrainOrientationAdjustmentMargin.Length < 0)
			{
				// Dynamic facing takes priority.
				var dynamicInit = reference.GetOrDefault<DynamicFacingInit>();
				if (dynamicInit != null)
				{
					var getFacing = dynamicInit.Value;
					return () => WRot.FromYaw(getFacing());
				}
				else
				{
					// Fall back to initial actor facing if an Init isn't available.
					var facingInit = reference.GetOrDefault<FacingInit>();
					facing = facingInit != null ? facingInit.Value : facingInfo.GetInitialFacing();
					return () => WRot.FromYaw(facing);
				}
			}

			var orientationInit = reference.GetOrDefault<TerrainOrientationInit>();
			var terrainOrientation = orientationInit != null ? orientationInit.Value : World.Map.TerrainOrientation(location.Value);
			var normalVector = new WVec(0, 0, 1024).Rotate(terrainOrientation);

			// Duplicated as to make more efficient delegates.
			{
				// Dynamic facing takes priority.
				var dynamicInit = reference.GetOrDefault<DynamicFacingInit>();
				if (dynamicInit != null)
				{
					var getFacing = dynamicInit.Value;
					return () => terrainOrientation + new WRot(normalVector, getFacing());
				}
				else
				{
					// Fall back to initial actor facing if an Init isn't available.
					var facingInit = reference.GetOrDefault<FacingInit>();
					facing = facingInit != null ? facingInit.Value : facingInfo.GetInitialFacing();
					return () => terrainOrientation + new WRot(normalVector, facing);
				}
			}
		}

		public Func<WAngle> GetFacing()
		{
			var facingInfo = Actor.TraitInfoOrDefault<IFacingInfo>();
			if (facingInfo == null)
				return () => WAngle.Zero;

			// Dynamic facing takes priority
			var dynamicInit = reference.GetOrDefault<DynamicFacingInit>();
			if (dynamicInit != null)
				return dynamicInit.Value;

			// Fall back to initial actor facing if an Init isn't available
			var facingInit = reference.GetOrDefault<FacingInit>();
			var facing = facingInit != null ? facingInit.Value : facingInfo.GetInitialFacing();
			return () => facing;
		}

		public DamageState GetDamageState()
		{
			var health = reference.GetOrDefault<HealthInit>();

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
