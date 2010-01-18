
namespace OpenRa.Traits
{
	class IronCurtainInfo : StatelessTraitInfo<IronCurtain> { }

	class IronCurtain : IResolveOrder
	{
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "PlayAnimation")
			{
				var rb = self.traits.Get<RenderBuilding>();
				if (rb != null)
					rb.PlayCustomAnim(self, order.TargetString);
			}
		}
	}
}
