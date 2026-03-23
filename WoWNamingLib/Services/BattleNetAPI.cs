namespace WoWNamingLib.Services
{
    public static class BattleNetAPI
    {
        private static string Token = "";
        private static DateTime TokenExpiration = DateTime.MinValue;
        private static HttpClient HttpClient = new HttpClient();

        private static bool CheckToken()
        {
            if (string.IsNullOrEmpty(Namer.battleNetClientID) || string.IsNullOrEmpty(Namer.battleNetClientSecret))
                return false;

            if (DateTime.UtcNow < TokenExpiration)
                return true;

            var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth.battle.net/token");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "grant_type", "client_credentials" },
                { "client_id", Namer.battleNetClientID },
                { "client_secret", Namer.battleNetClientSecret }
            });

            var response = HttpClient.Send(request);
            if (!response.IsSuccessStatusCode)
                throw new Exception("Failed to get Battle.net API token: " + response.StatusCode);

            var responseContent = response.Content.ReadAsStringAsync().Result;
            var json = System.Text.Json.JsonDocument.Parse(responseContent);

            Token = json.RootElement.GetProperty("access_token").GetString() ?? "";

            int expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();
            TokenExpiration = DateTime.UtcNow.AddSeconds(expiresIn - 60);

            return true;
        }

        public static string GetBaseNameForMediaFDID(uint fileDataID)
        {
            CheckToken();

            HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);

            var response = HttpClient.GetAsync($"https://us.api.blizzard.com/data/wow/search/media?namespace=static-us&assets.file_data_id=" + fileDataID).Result;
            if (!response.IsSuccessStatusCode)
                throw new Exception("Failed to get media info for FDID " + fileDataID + ": " + response.StatusCode);

            var responseContent = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Failed to get media info for FDID " + fileDataID + ": " + response.StatusCode);
                if(response.StatusCode != System.Net.HttpStatusCode.NotFound)
                    Console.WriteLine(responseContent);
            }

            var json = System.Text.Json.JsonDocument.Parse(responseContent);
            if(!json.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            {
                Console.WriteLine("No media found for FDID " + fileDataID);
                return "";
            }

            var url = json.RootElement.GetProperty("results")[0].GetProperty("data").GetProperty("assets")[0].GetProperty("value").GetString() ?? "";

            if(string.IsNullOrEmpty(url))
            {
                Console.WriteLine("Failed to get media URL for FDID " + fileDataID);
                return "";
            }
            else
            {
                return Path.GetFileNameWithoutExtension(url);
            }
        }
    }
}
