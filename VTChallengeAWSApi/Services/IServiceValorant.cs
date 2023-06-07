using NugetVTChallenge.Models;
using NugetVTChallenge.Models.Api;

namespace VTChallengeAWSApi.Services
{
    public interface IServiceValorant {

        Task<UserApi> GetAccountAsync(string username, string tagline);
        Task<UserApi> GetAccountUidAsync(string uid);
        Task<string> GetRankAsync(string username, string tag);

        Task<List<Weapon>> GetWeaponsAsync();
        Task<Skin> GetSkinById(string uuid);
        Task<Weapon> GetWeaponBySkinUuid(string skinUuid);
    }
}
