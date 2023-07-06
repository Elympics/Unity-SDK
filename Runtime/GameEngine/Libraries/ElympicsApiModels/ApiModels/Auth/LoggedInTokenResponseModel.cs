
namespace ElympicsApiModels.ApiModels.Auth
{
    public class LoggedInTokenResponseModel
    {
        public string UserName { get; set; }

        public string AuthToken { get; set; }

        public string RefreshToken { get; set; }
    }
}
