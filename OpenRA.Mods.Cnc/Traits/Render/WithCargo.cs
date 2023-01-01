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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits.Render
{
	[Desc("Renders the cargo loaded into the unit.")]
	public class WithCargoInfo : TraitInfo, Requires<CargoInfo>, Requires<BodyOrientationInfo>
	{
		[Desc("Cargo position relative to turret or body in (forward, right, up) triples. The default offset should be in the middle of the list.")]
		public readonly WVec[] LocalOffset = { WVec.Zero };

		[Desc("Passenger CargoType to display.")]
		public readonly HashSet<string> DisplayTypes = new HashSet<string>();

		public override object Create(ActorInitializer init) { return new WithCargo(init.Self, this); }
	}

	public class WithCargo : ITick, IRender, INotifyPassengerEntered, INotifyPassengerExited
	{
		readonly WithCargoInfo info;
		readonly Cargo cargo;
		readonly BodyOrientation body;
		readonly IFacing facing;
		WAngle cachedFacing;

		readonly Dictionary<Actor, IActorPreview[]> previews = new Dictionary<Actor, IActorPreview[]>();

		public WithCargo(Actor self, WithCargoInfo info)
		{
			this.info = info;

			cargo = self.Trait<Cargo>();
			body = self.Trait<BodyOrientation>();
			facing = self.TraitOrDefault<IFacing>();
		}

		void ITick.Tick(Actor self)
		{
			foreach (var actorPreviews in previews.Values)
				if (actorPreviews != null)
					foreach (var preview in actorPreviews)
						preview.Tick();

			// HACK: We don't have an efficient way to know when the preview
			// bounds change, so assume that we need to update the screen map
			// (only) when the facing changes
			if (facing.Facing != cachedFacing && previews.Count > 0)
			{
				self.World.ScreenMap.AddOrUpdate(self);
				cachedFacing = facing.Facing;
			}
		}

		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
		{
			var bodyOrientation = body.QuantizeOrientation(self.Orientation);
			var pos = self.CenterPosition;
			var i = 0;

			// Generate missing previews
			var missing = previews
				.Where(kv => kv.Value == null)
				.Select(kv => kv.Key)
				.ToList();

			foreach (var p in missing)
			{
				var passengerInits = new TypeDictionary()
				{
					new OwnerInit(p.Owner),
					new DynamicFacingInit(() => body.QuantizeFacing(facing.Facing)),
				};

				foreach (var api in p.TraitsImplementing<IActorPreviewInitModifier>())
					api.ModifyActorPreviewInit(p, passengerInits);

				var init = new ActorPreviewInitializer(p.Info, wr, passengerInits);
				previews[p] = p.Info.TraitInfos<IRenderActorPreviewInfo>()
					.SelectMany(rpi => rpi.RenderPreview(init))
					.ToArray();
			}

			foreach (var actorPreviews in previews.Values)
			{
				if (actorPreviews == null)
					continue;

				foreach (var p in actorPreviews)
				{
					var index = cargo.PassengerCount > 1 ? i++ % info.LocalOffset.Length : info.LocalOffset.Length / 2;
					var localOffset = info.LocalOffset[index];

					foreach (var pp in p.Render(wr, pos + body.LocalToWorld(localOffset.Rotate(bodyOrientation))))
						yield return pp.WithZOffset(1);
				}
			}
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
		{
			var pos = self.CenterPosition;
			foreach (var actorPreviews in previews.Values)
				if (actorPreviews != null)
					foreach (var p in actorPreviews)
						foreach (var b in p.ScreenBounds(wr, pos))
							yield return b;
		}

		void INotifyPassengerEntered.OnPassengerEntered(Actor self, Actor passenger)
		{
			if (info.DisplayTypes.Contains(passenger.Trait<Passenger>().Info.CargoType))
			{
				previews.Add(passenger, null);
				self.World.ScreenMap.AddOrUpdate(self);
			}
		}

		void INotifyPassengerExited.OnPassengerExited(Actor self, Actor passenger)
		{
			previews.Remove(passenger);
			self.World.ScreenMap.AddOrUpdate(self);
		}
	}
}
