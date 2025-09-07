using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ResoAPITool
{
    public class LoginCredentials
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? TotpCode { get; set; } = null; // Optional TOTP code for 2FA
    }

    public class Authentication
    {
        [JsonProperty("$type")]
        public string Type { get; set; } = "password";
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public Authentication Authentication { get; set; } = new();
        public string SecretMachineId { get; set; } = string.Empty;
        public bool RememberMe { get; set; } = false;
    }

    public class AuthResponse
    {
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    public class AuthEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    public class AuthResponseWrapper
    {
        public AuthEntity Entity { get; set; } = new();
    }

    public class ResoniteAuthService
    {
        private static string GenerateRandomMachineId()
        {

            const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_";
            var random = new Random();
            var result = new char[128];
            
            for (int i = 0; i < 128; i++)
            {
                result[i] = characters[random.Next(characters.Length)];
            }
            
            return new string(result);
        }

        private static string GenerateUID()
        {
            var randomBytes = new byte[16];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            
            var data = $"reso-api-delete-dashboards-{Convert.ToBase64String(randomBytes)}";
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
                return Convert.ToHexString(hashBytes);
            }
        }

        public static Task<LoginCredentials> AskLoginAsync()
        {
            Console.Write("Username: ");
            var username = Console.ReadLine() ?? string.Empty;

            Console.Write("Password: ");
            var password = ReadPassword();

            Console.Write("TOTP Code (optional, press Enter to skip): ");
            var totpInput = Console.ReadLine();
            var totpCode = string.IsNullOrWhiteSpace(totpInput) ? null : totpInput.Trim();

            return Task.FromResult(new LoginCredentials
            {
                Username = username,
                Password = password,
                TotpCode = totpCode
            });
        }

        private static string ReadPassword()
        {
            var password = new StringBuilder();
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password.Append(key.KeyChar);
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Remove(password.Length - 1, 1);
                    Console.Write("\b \b");
                }
            }
            while (key.Key != ConsoleKey.Enter);

            Console.WriteLine();
            return password.ToString();
        }

        public static async Task<AuthResponse> CreateTokenAsync()
        {
            var login = await AskLoginAsync();

            var loginRequest = new LoginRequest
            {
                Username = login.Username,
                Authentication = new Authentication { Password = login.Password },
                SecretMachineId = GenerateRandomMachineId(),
                RememberMe = false
            };

            var json = JsonConvert.SerializeObject(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");


            var uid = GenerateUID();
            
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("UID", uid);
            

            httpClient.DefaultRequestHeaders.Add("TOTP", login.TotpCode ?? "");

            var response = await httpClient.PostAsync("https://api.resonite.com/userSessions", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Authentication failed with status {response.StatusCode}: {responseContent}");
            }

            try
            {
                var authResponse = JsonConvert.DeserializeObject<AuthResponseWrapper>(responseContent);
                var auth = new AuthResponse
                {
                    UserId = authResponse?.Entity.UserId ?? string.Empty,
                    Token = authResponse?.Entity.Token ?? string.Empty
                };

                return auth;
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                throw new Exception($"Failed to parse authentication response. Response was: {responseContent}", ex);
            }
        }
    }
}
