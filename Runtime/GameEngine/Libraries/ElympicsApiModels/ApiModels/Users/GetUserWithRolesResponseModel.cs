using System.Collections.Generic;

namespace ElympicsApiModels.ApiModels.Users
{
	public class GetUserWithRolesResponseModel
	{
		public string UserId { get; set; }

		public string UserName { get; set; }

		public List<string> Roles { get; set; }
	}
}