namespace CaptivePortal
{
    public class UnifiControllerManager
    {
        private readonly HttpClient httpClient;
        private string? loginToken = null;
        private string? csrfToken = null;

        public UnifiControllerManager(IHttpClientFactory httpClientFactory)
        {
            this.httpClient = httpClientFactory.CreateClient("IgnoreSSLErrors")
                ?? throw new NullReferenceException(nameof(httpClient));
        }

        public async Task<bool> Login()
        {
            var loginRequest = new HttpRequestMessage(HttpMethod.Post, "https://10.100.0.1:443/api/auth/login");
            loginRequest.Content = JsonContent.Create(new
            {
                username = "portal",
                password = "Asdf790asdhfka61hklmandsf"
            });
            var loginResponse = await httpClient.SendAsync(loginRequest);
            if (!loginResponse.IsSuccessStatusCode) return false;

            List<string> loginCookies = loginResponse.Headers
                .Where(x => x.Key.Equals("Set-Cookie", StringComparison.InvariantCultureIgnoreCase))
                .SelectMany(x => x.Value)
                .SelectMany(x => x.Split("; "))
            .ToList();
            loginToken = loginCookies.Where(x => x.StartsWith("TOKEN=")).FirstOrDefault();
            if (loginToken is null) return false;

            csrfToken = loginResponse.Headers
                .Where(x => x.Key.Equals("X-CSRF-Token", StringComparison.InvariantCultureIgnoreCase))
                .SelectMany(x => x.Value)
                .FirstOrDefault();
            if (csrfToken is null) return false;

            return true;
        }

        public async Task<bool> Logout()
        {
            if (loginToken is null || csrfToken is null)
            {
                throw new InvalidOperationException("Must login first!");
            }
            
            var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "https://10.100.0.1:443/api/auth/logout");
            logoutRequest.Headers.Add("Cookie", loginToken);
            logoutRequest.Headers.Add("X-CSRF-Token", csrfToken);
            var logoutResponse = await httpClient.SendAsync(logoutRequest);

            return logoutResponse.IsSuccessStatusCode;
        }

        public async Task<bool> AuthorizeGuest(string clientMac, string accessPointMac, int minutesAuthorized)
        {
            if (loginToken is null || csrfToken is null)
            {
                throw new InvalidOperationException("Must login first!");
            }

            var authorizeRequest = new HttpRequestMessage(HttpMethod.Post, "https://10.100.0.1:443/proxy/network/api/s/default/cmd/stamgr");
            authorizeRequest.Content = JsonContent.Create(new
            {
                cmd = "authorize-guest",
                mac = clientMac,
                ap_mac = accessPointMac,
                minutes = minutesAuthorized
            });
            authorizeRequest.Headers.Add("Cookie", loginToken);
            authorizeRequest.Headers.Add("X-CSRF-Token", csrfToken);
            var authorizeResponse = await httpClient.SendAsync(authorizeRequest);

            return authorizeResponse.IsSuccessStatusCode;
        }

        public async Task<bool> KickStation(string clientMac, string accessPointMac)
        {
            if (loginToken is null || csrfToken is null)
            {
                throw new InvalidOperationException("Must login first!");
            }

            var request = new HttpRequestMessage(HttpMethod.Post, "https://10.100.0.1:443/proxy/network/api/s/default/cmd/stamgr");
            request.Content = JsonContent.Create(new
            {
                cmd = "kick-sta",
                mac = clientMac,
                ap_mac = accessPointMac,
            });
            request.Headers.Add("Cookie", loginToken);
            request.Headers.Add("X-CSRF-Token", csrfToken);
            var response = await httpClient.SendAsync(request);

            return response.IsSuccessStatusCode;
        }
    }
}
