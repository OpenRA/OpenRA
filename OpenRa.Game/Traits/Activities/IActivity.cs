
namespace OpenRa.Traits.Activities
{
	public interface IActivity
	{
		IActivity NextActivity { get; set; }
		IActivity Tick( Actor self );
		void Cancel( Actor self );
	}
}
