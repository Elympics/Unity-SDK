namespace Daftmobile.Api
{
	public class ApiResponse
	{
		public bool   IsSuccess    => ErrorMessage == null;
		public string ErrorMessage { get; set; }
	}
}
