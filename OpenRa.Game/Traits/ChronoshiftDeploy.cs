using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.Orders;
using System.Drawing;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class ChronoshiftDeploy : IOrder, ISpeedModifier, ITick, IPips, IPaletteModifier
	{
		// Recharge logic
		int chargeTick = 0; // How long until we can chronoshift again?
		int chargeLength = (int)(Rules.Aftermath.ChronoTankDuration * 60 * 25); // How long between shifts?
		
		// Screen fade logic
		int animationTick = 0;
		int animationLength = 10;
		bool animationStarted = false;

		public ChronoshiftDeploy(Actor self) { }
		
		public void Tick(Actor self)
		{
			if (chargeTick > 0)
				chargeTick--;

			if (animationStarted)
			{
				if (animationTick < animationLength)
					animationTick++;
				else
					animationStarted = false;
			}
			if (!animationStarted)
			{
				if (animationTick > 0)
					animationTick--;
			}
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Right && xy == self.Location && chargeTick <= 0)
				return new Order("Deploy", self, null, int2.Zero, null);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Deploy")
			{
				Game.controller.orderGenerator = new ChronoshiftDestinationOrderGenerator(self);
				return;
			}

			var movement = self.traits.WithInterface<IMovement>().FirstOrDefault();
			if (order.OrderString == "Chronoshift" && movement.CanEnterCell(order.TargetLocation))
			{
				Game.controller.CancelInputMode();
				self.CancelActivity();
				self.QueueActivity(new Activities.Teleport(order.TargetLocation));
				Sound.Play("chrotnk1.aud");
				chargeTick = chargeLength;
				animationStarted = true;
			}
		}
		
		public float GetSpeedModifier()
		{
			// ARGH! You must not do this, it will desync!
			return (Game.controller.orderGenerator is ChronoshiftDestinationOrderGenerator) ? 0f : 1f;
		}
		
		// Display 5 pips indicating the current charge status
		public IEnumerable<PipType> GetPips()
		{
			const int numPips = 5;
			for (int i = 0; i < numPips; i++)
			{
				if ((1 - chargeTick * 1.0f / chargeLength) * numPips < i + 1)
				{
					yield return PipType.Transparent;
					continue;
				}
					
				switch (i)
				{
					case 0:
					case 1:
						yield return PipType.Red;
						break;
					case 2:
					case 3:
						yield return PipType.Yellow;
						break;
					case 4:
						yield return PipType.Green;
						break;
				}
			}
		}

		public void AdjustPalette(Bitmap bmp)
		{
			if (!animationStarted && animationTick == 0)
				return;

			// saturation modifier
			var f = 1 - (animationTick * 1.0f / animationLength);

			using (var bitmapCopy = new Bitmap(bmp))
				for (int j = 0; j < (int)PaletteType.Chrome; j++)
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
