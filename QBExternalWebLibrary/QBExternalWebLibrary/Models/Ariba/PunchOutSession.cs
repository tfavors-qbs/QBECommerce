using Ariba;

namespace QBExternalWebLibrary.Models.Ariba
{
	public class PunchOutSession
	{
		public int Id { get; set; }
		public string SessionId { get; set; }
		public string UserId { get; set; }
		public string FromId { get; set; }
		public string PostUrl { get; set; }
		public string BuyerCookie { get; set; }
		public string Operation { get; set; }
		public DateTime CreatedDateTime { get; set; }
		public DateTime ExpirationDateTime { get; set; }
		public PunchOutSetupRequestOperation? PunchOutOperation => Utility.Utility.ParseEnumAsNullable<PunchOutSetupRequestOperation>(Operation);
		public const int StartingMinutesToExpire = 30;
	}
}
