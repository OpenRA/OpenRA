
namespace OpenRa.Traits
{
	class RenderBuildingChargeInfo : RenderBuildingInfo
	{
		public override object Create(Actor self) { return new RenderBuildingCharge(self); }
	}

	/* used for tesla */
	class RenderBuildingCharge : RenderBuilding, INotifyAttack
	{
		public RenderBuildingCharge(Actor self)
			: base(self)
		{
		}

		public void Attacking(Actor self)
		{
			Sound.Play("tslachg2.aud");
			anim.PlayThen(GetPrefix(self) + "active", 
				() => anim.PlayRepeating(GetPrefix(self) + "idle"));
		}
	}
}
