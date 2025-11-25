using Google.Apis.Auth.OAuth2;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace UniversidadDB.Services
{
    public class FcmService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly GoogleCredential _credential;
        private readonly string _projectId;

        public FcmService(IConfiguration config, IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;

            var json = config["FCM_SERVICE_ACCOUNT_JSON"];
            _projectId = config["FCM_PROJECT_ID"] ?? "";

            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException("FCM_SERVICE_ACCOUNT_JSON no configurado.");

            if (string.IsNullOrWhiteSpace(_projectId))
                throw new InvalidOperationException("FCM_PROJECT_ID no configurado.");

            _credential = GoogleCredential.FromJson(json)
                .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
        }

        public async Task SendToTokenAsync(string token, string title, string body, Dictionary<string, string>? data = null)
        {
            var accessToken = await _credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
            var client = _httpFactory.CreateClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var payload = new
            {
                message = new
                {
                    token = token,
                    notification = new { title, body },
                    data = data ?? new Dictionary<string, string>()
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var resp = await client.PostAsync(
                $"https://fcm.googleapis.com/v1/projects/{_projectId}/messages:send",
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync();
                throw new Exception($"FCM error {(int)resp.StatusCode}: {err}");
            }
        }
    }
}
