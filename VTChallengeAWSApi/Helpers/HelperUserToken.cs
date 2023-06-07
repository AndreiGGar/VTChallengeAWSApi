using Newtonsoft.Json;
using NugetVTChallenge.Models;
using System.Security.Claims;

namespace VTChallengeAWSApi.Helpers {
    public class HelperUserToken {

        private readonly IHttpContextAccessor _httpContextAccessor;

        public HelperUserToken(IHttpContextAccessor httpContextAccessor) {
            _httpContextAccessor = httpContextAccessor;
        }


        public Usuario GetUserToken() {
            Claim claim = _httpContextAccessor.HttpContext.User.Claims.SingleOrDefault(x => x.Type == "UserData");
            string jsonUsuario = claim.Value;
            Usuario usuario = JsonConvert.DeserializeObject<Usuario>(jsonUsuario);
            return usuario;
        }
    }
}
