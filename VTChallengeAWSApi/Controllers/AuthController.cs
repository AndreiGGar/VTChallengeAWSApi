using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NugetVTChallenge.Interfaces;
using NugetVTChallenge.Models;
using NugetVTChallenge.Models.Api;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using VTChallengeAWSApi.Services;
using VTChallengeAWSApi.Helpers;

namespace VTChallengeAWSApi.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase {

        private IVtChallenge repo;
        private HelperOAuthToken helper;
        private IServiceValorant api;

        public AuthController(IVtChallenge repo, HelperOAuthToken helper, IServiceValorant api) {
            this.repo = repo;
            this.api = api;
            this.helper = helper;
        }


        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> LogIn(LogIn model) {
            Usuario usuario = await this.repo.LoginNamePasswordAsync(model.Name, model.Password);
            if (usuario == null) {
                return Unauthorized();
            } else {
                // DEBEMOS CREAR UNAS CREDENCIALES DENTRO DEL TOKEN
                SigningCredentials credentials = new SigningCredentials(this.helper.GetToken(), SecurityAlgorithms.HmacSha256);

                string jsonUsuario = JsonConvert.SerializeObject(usuario);
                Claim[] information = new[] {
                    new Claim("UserData", jsonUsuario)
                };
                // EL TOKEN SE GENERA CON UNA CLASE Y DEBEMOS INDICAR
                // LOS DATOS QUE CONFORMAN DICHO TOKEN
                JwtSecurityToken token = new JwtSecurityToken(
                    claims: information,
                    issuer: this.helper.Issuer,
                    audience: this.helper.Audience,
                    signingCredentials: credentials,
                    expires: DateTime.UtcNow.AddMinutes(60),
                    notBefore: DateTime.UtcNow
                );
                return Ok(new {
                    response = new JwtSecurityTokenHandler().WriteToken(token)
                });
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> Register(Usuario user) {
            DataApi data = null;
            UserApi userapi = await this.api.GetAccountAsync(user.Name, user.Tag);

            if (userapi != null) {
                data = userapi.Data;

                user.Uid = data.Puuid;
                user.ImageLarge = data.Card.Large;
                user.ImageSmall = data.Card.Small;
                user.Rank = await this.api.GetRankAsync(user.Name, user.Tag);
            }
            if(data != null){
                await this.repo.RegisterUserAsync(user.Uid, user.Name, user.Tag, user.Email, user.Password, user.ImageSmall, user.ImageLarge, user.Rank);
            }
            return Ok();
        }
    }
}
