#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;
using OpenRA.Mods.RA.Activities;

namespace OpenRA.Mods.RA.Render
{
	public class RenderBuildingInfo : RenderSimpleInfo, Requires<BuildingInfo>, IPlaceBuildingDecoration
	{
		public readonly bool HasMakeAnimation = true;
		public readonly float2 Origin = float2.Zero;
		public override object Create(ActorInitializer init) { return new RenderBuilding(init, this);}

		public override IEnumerable<Renderable> RenderPreview(ActorInfo building, PaletteReference pr)
		{
			var origin = building.Traits.Get<RenderBuildingInfo>().Origin;
			return base.RenderPreview(building, pr).Select(a => a.WithPxOffset(origin));
		}

		public void Render(WorldRenderer wr, World w, ActorInfo ai, PPos centerLocation)
		{
			if (!ai.Traits.Get<BuildingInfo>().RequiresBaseProvider)
				return;

			foreach (var a in w.ActorsWithTrait<BaseProvider>())
				a.Trait.RenderBeforeWorld(wr, a.Actor);
		}
	}

	public class RenderBuilding : RenderSimple, INotifyDamageStateChanged, IRenderModifier
	{
		readonly RenderBuildingInfo Info;

		public RenderBuilding( ActorInitializer init, RenderBuildingInfo info )
			: this(init, info, () => 0) { }

		public RenderBuilding( ActorInitializer init, RenderBuildingInfo info, Func<int> baseFacing )
			: base(init.self, baseFacing)
		{
			Info = info;
			var self = init.self;
			// Work around a bogus crash
			anim.PlayRepeating( NormalizeSequence(self, "idle") );

			// Can't call Complete() directly from ctor because other traits haven't been inited yet
			if (self.Info.Traits.Get<RenderBuildingInfo>().HasMakeAnimation && !init.Contains<SkipMakeAnimsInit>())
				self.QueueActivity(new MakeAnimation(self, () => Complete(self)));
			else
				self.QueueActivity(new CallFunc(() => Complete(self)));
		}

		public IEnumerable<Renderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<Renderable> r)
		{
			var disabled = self.IsDisabled();
			foreach (var a in r)
			{
				var ret = a.WithPxOffset(-Info.Origin);
				yield return ret;
				if (disabled)
					yield return ret.WithPalette(wr.Palette("disabled")).WithZOffset(1);
			}
		}

		void Complete( Actor self )
		{
			anim.PlayRepeating( NormalizeSequence(self, "idle") );
			foreach( var x in self.TraitsImplementing<INotifyBuildComplete>() )
				x.BuildingComplete( self );
		}

		public void PlayCustomAnimThen(Actor self, string name, Action a)
		{
			anim.PlayThen(NormalizeSequence(self, name),
				() => { anim.PlayRepeating(NormalizeSequence(self, "idle")); a(); });
		}

		public void PlayCustomAnimRepeating(Actor self, string name)
		{
			anim.PlayThen(NormalizeSequence(self, name),
				() => { PlayCustomAnimRepeating(self, name); });
		}

		public void PlayCustomAnimBackwards(Actor self, string name, Action a)
		{
			anim.PlayBackwardsThen(NormalizeSequence(self, name),
				() => { anim.PlayRepeating(NormalizeSequence(self, "idle")); a(); });
		}

		public void CancelCustomAnim(Actor self)
		{
			anim.PlayRepeating( NormalizeSequence(self, "idle") );
		}

		public virtual void DamageStateChanged(Actor self, AttackInfo e)
		{
			if (e.DamageState >= DamageState.Heavy && e.PreviousDamageState < DamageState.Heavy)
				anim.ReplaceAnim("damaged-idle");
			else if (e.DamageState < DamageState.Heavy)
				anim.ReplaceAnim("idle");
		}
	}
}
