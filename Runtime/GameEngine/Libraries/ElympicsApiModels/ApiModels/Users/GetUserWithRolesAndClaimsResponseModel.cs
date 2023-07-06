using System.Collections.Generic;

namespace ElympicsApiModels.ApiModels.Users
{
    public class GetUserWithRolesAndClaimsResponseModel
    {
        public List<ClaimModel> Claims { get; set; }
    }
}
