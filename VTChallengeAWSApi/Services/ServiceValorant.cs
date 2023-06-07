using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using NugetVTChallenge.Models.Api;
using NugetVTChallenge.Models;

namespace VTChallengeAWSApi.Services
{
    public class ServiceValorant : IServiceValorant
    {

        private HttpClient httpClient;
        private string url;
        private string urlweapons;

        public ServiceValorant(HttpClient httpClient)
        {
            this.httpClient = httpClient;
            this.url = "https://api.henrikdev.xyz/";
            this.urlweapons = "https://valorant-api.com/";
        }

        public async Task<UserApi> GetAccountAsync(string username, string tagline)
        {
            string request = "valorant/v1/account/" + username + "/" + tagline;
            string url = this.url + request;

            var response = await httpClient.GetAsync(url);

            string jsonReponse = await response.Content.ReadAsStringAsync();

            if (jsonReponse == null)
            {
                return null;
            }
            else
            {
                return JsonConvert.DeserializeObject<UserApi>(jsonReponse);
            }
        }

        public async Task<UserApi> GetAccountUidAsync(string uid)
        {
            string request = "valorant/v1/by-puuid/account/" + uid;
            string url = this.url + request;

            var response = await httpClient.GetAsync(url);

            string jsonReponse = await response.Content.ReadAsStringAsync();

            if (jsonReponse == null)
            {
                return null;
            }
            else
            {
                return JsonConvert.DeserializeObject<UserApi>(jsonReponse);
            }
        }

        public async Task<string> GetRankAsync(string username, string tag)
        {
            string request = "valorant/v1/mmr-history/eu/" + username + "/" + tag;
            string url = this.url + request;

            var response = await httpClient.GetAsync(url);

            string jsonReponse = await response.Content.ReadAsStringAsync();

            if (jsonReponse == null)
            {
                return "";
            }
            else
            {
                // Parse JSON string to a JObject
                JObject jsonObj = JObject.Parse(jsonReponse);

                // Get the "data" array
                JArray dataArray = (JArray)jsonObj["data"];

                // Get the first object in the "data" array
                JObject dataObj = (JObject)dataArray[0];

                // Get the value of the "currenttier_patched" property
                string currentTierPatched = dataObj.GetValue("currenttierpatched").Value<string>();

                return currentTierPatched;
            }
        }

        public async Task<List<Weapon>> GetWeaponsAsync()
        {
            string request = "v1/weapons";
            string url = this.urlweapons + request;

            var response = await httpClient.GetAsync(url);

            string jsonResponse = await response.Content.ReadAsStringAsync();

            if (jsonResponse == null)
            {
                return new List<Weapon>();
            }
            else
            {
                // Parse JSON string to a JObject
                JObject jsonObj = JObject.Parse(jsonResponse);

                // Get the "data" array
                JArray dataArray = (JArray)jsonObj["data"];

                // Create a list to store the weapons
                List<Weapon> weapons = new List<Weapon>();

                // Loop through each object in the "data" array
                foreach (JObject dataObj in dataArray)
                {
                    // Get the values from the properties
                    string uuid = dataObj.GetValue("uuid").Value<string>();
                    string displayName = dataObj.GetValue("displayName").Value<string>();

                    // Create a new Weapon object
                    Weapon weapon = new Weapon
                    {
                        Uuid = uuid,
                        DisplayName = displayName,
                        Skins = new List<Skin>()
                    };

                    // Get the "skins" array
                    JArray skinsArray = (JArray)dataObj["skins"];
                    if (skinsArray != null)
                    {
                        // Loop through each object in the "skins" array
                        foreach (JObject skinObj in skinsArray)
                        {
                            // Get the values from the properties
                            string skinUuid = skinObj.GetValue("uuid").Value<string>();
                            string skinDisplayName = skinObj.GetValue("displayName").Value<string>();

                            // Check if displayIcon is null
                            JToken displayIconToken = skinObj.GetValue("displayIcon");
                            if (displayIconToken != null && displayIconToken.Type != JTokenType.Null)
                            {
                                string displayIcon = displayIconToken.Value<string>();

                                // Create a new Skin object only if displayIcon is not null
                                if (displayIcon != null)
                                {
                                    // Create a new Skin object
                                    Skin skin = new Skin
                                    {
                                        Uuid = skinUuid,
                                        DisplayName = skinDisplayName,
                                        DisplayIcon = displayIcon
                                    };

                                    // Add the skin object to the weapon's skins list
                                    weapon.Skins.Add(skin);
                                }
                            }
                        }
                    }

                    // Add the weapon object to the list if it has at least one non-null skin
                    if (weapon.Skins.Count > 0)
                    {
                        weapons.Add(weapon);
                    }
                }

                return weapons;
            }
        }

        public async Task<Skin> GetSkinById(string uuid)
        {
            var weapons = await GetWeaponsAsync();

            foreach (var weapon in weapons)
            {
                var skin = weapon.Skins.FirstOrDefault(s => s.Uuid == uuid);
                if (skin != null)
                {
                    return skin;
                }
            }

            return null; // Skin with the given UUID not found
        }

        public async Task<Weapon> GetWeaponBySkinUuid(string skinUuid)
        {
            List<Weapon> weapons = await GetWeaponsAsync();

            foreach (Weapon weapon in weapons)
            {
                Skin skin = weapon.Skins.FirstOrDefault(s => s.Uuid == skinUuid);
                if (skin != null)
                {
                    return weapon;
                }
            }

            return null; // Weapon with the given skin UUID not found
        }
    }
}
