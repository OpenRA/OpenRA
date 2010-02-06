
namespace OpenRa.Traits
{
	class RenderBuildingChargeInfo : RenderBuildingInfo
	{
		public readonly string ChargeAudio = "tslachg2.aud";
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
			Sound.Play(self.Info.Traits.Get<RenderBuildingChargeInfo>().ChargeAudio);
			anim.PlayThen(GetPrefix(self) + "active", 
				() => anim.PlayRepeating(GetPrefix(self) + "idle"));
		}
	}
}
