using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NugetVTChallenge.Interfaces;
using NugetVTChallenge.Models;
using System.Security.Claims;
using VTChallengeAWSApi.Helpers;

namespace VTChallengeAWSApi.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase {

        private IVtChallenge repo;
        private HelperUserToken helper;

        public UsuarioController(IVtChallenge repo, HelperUserToken helper) {
            this.repo = repo;
            this.helper = helper;
        }

        [HttpGet]
        [Route("[action]")]
        [Authorize]
        public async Task<ActionResult<Usuario>> Profile() {
            Claim claim = HttpContext.User.Claims.SingleOrDefault(x => x.Type == "UserData");
            string jsonUsuario = claim.Value;
            Usuario usuario = JsonConvert.DeserializeObject<Usuario>(jsonUsuario);
            return usuario;
        }

        [HttpGet]
        [Route("[action]")]
        [Authorize]
        public async Task<ActionResult> Update() {
            Usuario usuario = this.helper.GetUserToken();
            await this.repo.UpdateProfileAsync(usuario.Uid);

            return Ok("Debe volver a iniciar sesión para ver los cambios");
        }

        [HttpGet]
        [Route("[action]")]
        [Authorize]
        public async Task<ActionResult<int>> Victories() {
            return await this.repo.GetTotalWinsAsync(this.helper.GetUserToken().Uid);
        }


    }
}
