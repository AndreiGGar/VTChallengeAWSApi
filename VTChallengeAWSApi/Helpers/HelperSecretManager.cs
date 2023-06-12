/*using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using NugetVTChallenge.Models;
using Newtonsoft.Json;

namespace VTChallengeAWSApi.Helpers
{
    public static class HelperSecretManager
    {
        public static async Task<KeysModel> GetSecretAsync()
        {
            string secretName = "VTChallengeSecrets";
            string region = "us-east-1";

            IAmazonSecretsManager client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));

            GetSecretValueRequest request = new GetSecretValueRequest
            {
                SecretId = secretName,
                VersionStage = "AWSCURRENT",
            };

            GetSecretValueResponse response;

            response = await client.GetSecretValueAsync(request);
            string secret = response.SecretString;

            KeysModel model = JsonConvert.DeserializeObject<KeysModel>(secret);
            return model;
        }
    }
}
*/