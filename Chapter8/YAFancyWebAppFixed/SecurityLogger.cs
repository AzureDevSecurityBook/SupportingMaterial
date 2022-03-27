using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using LogAnalytics.Client;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace YAFancyWebAppFixed
{
    public class SecurityLogger
    {
        private static readonly SecurityLogger _instance = new SecurityLogger();
        private LogAnalyticsClient? _client;

        private SecurityLogger()
        {
        }

        public static SecurityLogger Instance => _instance;

        public void Log(string category, string payload)
        {
            var encoder = HtmlEncoder.Create(allowedRanges : new[] { UnicodeRanges.BasicLatin });

            _client?.SendLogEntry<PossibleAttackAttempt>(new PossibleAttackAttempt()
            {
                Category = category,
                Payload = encoder.Encode(payload)
            }, "Security");
        }

        public async Task Initialize()
        {
            var uri = Environment.GetEnvironmentVariable("KeyVaultUri");
            if (uri != null)
            {
                SecretClient client = new SecretClient(new Uri(uri), new DefaultAzureCredential());

                var workspaceId = (await client.GetSecretAsync("WorkspaceId"))?.Value?.Value;
                var workspaceKey = (await client.GetSecretAsync("WorkspaceKey"))?.Value?.Value;

                if (!string.IsNullOrWhiteSpace(workspaceId) && !string.IsNullOrWhiteSpace(workspaceKey))
                { 
                    _client = new LogAnalyticsClient(
                            workspaceId: workspaceId,
                            sharedKey: workspaceKey);
                }
            }
        }
    }
}
