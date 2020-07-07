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
using System.ComponentModel;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	public class FacingInit : ValueActorInit<WAngle>, ISingleInstanceInit
	{
		public FacingInit(WAngle value)
			: base(value) { }
	}

	public class CreationActivityDelayInit : ValueActorInit<int>, ISingleInstanceInit
	{
		public CreationActivityDelayInit(int value)
			: base(value) { }
	}

	public class DynamicFacingInit : ValueActorInit<Func<WAngle>>, ISingleInstanceInit
	{
		public DynamicFacingInit(Func<WAngle> value)
			: base(value) { }
	}

	public class SubCellInit : ValueActorInit<SubCell>, ISingleInstanceInit
	{
		public SubCellInit(SubCell value)
			: base(value) { }
	}

	public class CenterPositionInit : ValueActorInit<WPos>, ISingleInstanceInit
	{
		public CenterPositionInit(WPos value)
			: base(value) { }
	}

	// Allows maps / transformations to specify the faction variant of an actor.
	public class FactionInit : ValueActorInit<string>, ISingleInstanceInit
	{
		public FactionInit(string value)
			: base(value) { }
	}

	public class EffectiveOwnerInit : ValueActorInit<Player>
	{
		public EffectiveOwnerInit(Player value)
			: base(value) { }
	}

	internal class ActorInitLoader : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
		{
			return new ActorInitActorReference(value as string);
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
		}

		public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType == typeof(string))
			{
				var reference = value as ActorInitActorReference;
				if (reference != null)
					return reference.InternalName;
			}

			return base.ConvertTo(context, culture, value, destinationType);
		}
	}

	[TypeConverter(typeof(ActorInitLoader))]
	public class ActorInitActorReference
	{
		public readonly string InternalName;
		readonly Actor actor;

		public ActorInitActorReference(Actor actor)
		{
			this.actor = actor;
		}

		public ActorInitActorReference(string internalName)
		{
			InternalName = internalName;
		}

		Actor InnerValue(World world)
		{
			if (actor != null)
				return actor;

			var sma = world.WorldActor.Trait<SpawnMapActors>();
			return sma.Actors[InternalName];
		}

		/// <summary>
		/// The lazy value may reference other actors that have not been created
		/// yet, so must not be resolved from the actor constructor or Created method.
		/// Use a FrameEndTask or wait until it is actually needed.
		/// </summary>
		public Lazy<Actor> Actor(World world)
		{
			return new Lazy<Actor>(() => InnerValue(world));
		}

		public static implicit operator ActorInitActorReference(Actor a)
		{
			return new ActorInitActorReference(a);
		}

		public static implicit operator ActorInitActorReference(string mapName)
		{
			return new ActorInitActorReference(mapName);
		}
	}
}
