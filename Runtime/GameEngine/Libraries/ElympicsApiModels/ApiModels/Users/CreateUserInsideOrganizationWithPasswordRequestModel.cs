namespace ElympicsApiModels.ApiModels.Users
{
	public class CreateUserInsideOrganizationWithPasswordRequestModel
	{
		public string UserName { get; set; }

		public string Password { get; set; }

		public string Email { get; set; }
	}
}