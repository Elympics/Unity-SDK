using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ElympicsApiModels.ApiModels.Auth
{
	public class LoginUserNamePasswordRequestModel
	{
		public string UserName { get; set; }

		public string Password { get; set; }
	}
}