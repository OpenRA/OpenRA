using OpenRa.Game.Traits;
using OpenRa.Game.Orders;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace OpenRa.Game.Traits
{
	class Chronoshiftable : IOrder, ISpeedModifier, ITick, IPaletteModifier
	{
		// Return-to-sender logic
		int2 chronoshiftOrigin;
		int chronoshiftReturnTicks = 0;

		// Screen fade logic
		int animationTick = 0;
		int animationLength = 20;
		bool animationStarted = false;
		
		public Chronoshiftable(Actor self) { }

		public void Tick(Actor self)
		{
			if (animationStarted)
			{
				if (animationTick < animationLength)
					animationTick++;
				else
					animationStarted = false;
			}
			else if (animationTick > 0)
					animationTick--;
			
			if (chronoshiftReturnTicks <= 0)
				return;

			if (chronoshiftReturnTicks > 0)
				chronoshiftReturnTicks--;

			// Return to original location
			if (chronoshiftReturnTicks == 0)
			{
				self.CancelActivity();
				// Todo: need a new Teleport method that will move to the closest available cell
				self.QueueActivity(new Activities.Teleport(chronoshiftOrigin));
			}
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			return null; // Chronoshift order is issued through Chrome.
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "ChronosphereSelect")
			{
				Game.controller.orderGenerator = new ChronoshiftDestinationOrderGenerator(self);
			}

			var movement = self.traits.WithInterface<IMovement>().FirstOrDefault();
			if (order.OrderString == "Chronoshift" && movement.CanEnterCell(order.TargetLocation))
			{
				
				// Set up return-to-sender info
				chronoshiftOrigin = self.Location;
				chronoshiftReturnTicks = (int)(Rules.General.ChronoDuration * 60 * 25);
				
				// TODO: Kill cargo if Rules.General.ChronoKillCargo says so
				
				// Set up the teleport
				Game.controller.CancelInputMode();
				self.CancelActivity();
				self.QueueActivity(new Activities.Teleport(order.TargetLocation));
				Sound.Play("chrono2.aud");
				
				// Start the screen-fade animation
				animationStarted = true;
				
				// Play chronosphere active anim
				var chronosphere = Game.world.Actors.Where(a => a.Owner == order.Subject.Owner && a.traits.Contains<Chronosphere>()).FirstOrDefault();
				if (chronosphere != null)
					chronosphere.traits.Get<RenderBuilding>().PlayCustomAnim(chronosphere, "active");
			}
		}

		public float GetSpeedModifier()
		{
			// ARGH! You must not do this, it will desync!
			return (Game.controller.orderGenerator is ChronoshiftDestinationOrderGenerator) ? 0f : 1f;
		}

		public void AdjustPalette(Bitmap bmp)
		{
			if (!animationStarted && animationTick == 0)
				return;

			// saturation modifier
			var f = 1 - (animationTick * 1.0f / animationLength);

			using (var bitmapCopy = new Bitmap(bmp))
				for (int j = 0; j < 8; j++)
					for (int i = 0; i < bmp.Width; i++)
					{
						var h = bitmapCopy.GetPixel(i, j).GetHue(); // 0-360
						var s = f * bitmapCopy.GetPixel(i, j).GetSaturation(); // 0-1.0
						var l = bitmapCopy.GetPixel(i, j).GetBrightness(); // 0-1.0
						var alpha = bitmapCopy.GetPixel(i, j).A;

						// Convert from HSL to RGB
						// Refactor me!
						var q = (l < 0.5f) ? l * (1 + s) : l + s - (l * s);
						var p = 2 * l - q;
						var hk = h / 360.0f;

						double[] trgb = { hk + 1 / 3.0f,
										  hk,
										  hk - 1/3.0f };
						double[] rgb = { 0, 0, 0 };

						for (int k = 0; k < 3; k++)
						{
							// mod doesn't seem to work right... do it manually
							while (trgb[k] < 0) trgb[k] += 1.0f;
							while (trgb[k] > 1) trgb[k] -= 1.0f;
						}

						for (int k = 0; k < 3; k++)
						{
							if (trgb[k] < 1 / 6.0f) { rgb[k] = (p + ((q - p) * 6 * trgb[k])); }
							else if (trgb[k] >= 1 / 6.0f && trgb[k] < 0.5) { rgb[k] = q; }
							else if (trgb[k] >= 0.5f && trgb[k] < 2.0f / 3) { rgb[k] = (p + ((q - p) * 6 * (2.0f / 3 - trgb[k]))); }
							else { rgb[k] = p; }
						}
						bmp.SetPixel(i, j, Color.FromArgb(alpha, (int)(rgb[0] * 255), (int)(rgb[1] * 255), (int)(rgb[2] * 255)));
					}
		}
	}
}
