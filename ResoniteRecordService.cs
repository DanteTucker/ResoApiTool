using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ResoAPITool
{
    public class Record
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public DateTime LastModificationTime { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }

    public class UserProfile
    {
        [JsonProperty("iconUrl")]
        public string? IconUrl { get; set; }

        [JsonProperty("tagline")]
        public string? Tagline { get; set; }

        [JsonProperty("displayBadges")]
        public List<string> DisplayBadges { get; set; } = new List<string>();

        [JsonProperty("description")]
        public string? Description { get; set; }
    }

    public class UserInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("username")]
        public string Username { get; set; } = string.Empty;

        [JsonProperty("normalizedUsername")]
        public string NormalizedUsername { get; set; } = string.Empty;

        [JsonProperty("registrationDate")]
        public DateTime RegistrationDate { get; set; }

        [JsonProperty("isVerified")]
        public bool IsVerified { get; set; }

        [JsonProperty("isLocked")]
        public bool IsLocked { get; set; }

        [JsonProperty("supressBanEvasion")]
        public bool SupressBanEvasion { get; set; }

        [JsonProperty("2fa_login")]
        public bool TwoFactorLogin { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        [JsonProperty("profile")]
        public UserProfile? Profile { get; set; }

        [JsonProperty("isActiveSupporter")]
        public bool IsActiveSupporter { get; set; }
    }

    public class ResoniteRecordService
    {
        public static async Task<List<Record>> GetAllRecordsAsync(AuthResponse auth)
        {
            using var requestClient = new HttpClient();
            
            requestClient.DefaultRequestHeaders.Add("Authorization", $"res {auth.UserId}:{auth.Token}");
            requestClient.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
            requestClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

            var response = await requestClient.GetAsync($"https://api.resonite.com/users/{auth.UserId}/records");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to get records with status {response.StatusCode}: {responseContent}");
            }

            var allRecords = JsonConvert.DeserializeObject<List<Record>>(responseContent) ?? new List<Record>();
            return allRecords;
        }

        public static async Task<List<Record>> GetRecordsByFilterAsync(AuthResponse auth, Func<Record, bool> filter)
        {
            var allRecords = await GetAllRecordsAsync(auth);
            return allRecords.Where(filter).ToList();
        }

        public static async Task<List<Record>> GetRecordsByNameAsync(AuthResponse auth, string name)
        {
            return await GetRecordsByFilterAsync(auth, record => record.Name == name);
        }

        public static async Task<List<Record>> GetRecordsByPathAsync(AuthResponse auth, string path)
        {
            return await GetRecordsByFilterAsync(auth, record => record.Path == path);
        }

        public static async Task<List<Record>> GetRecordsByNameAndPathAsync(AuthResponse auth, string name, string path)
        {
            return await GetRecordsByFilterAsync(auth, record => record.Name == name && record.Path == path);
        }

        public static async Task<List<Record>> GetRecordsByTagAsync(AuthResponse auth, string tag)
        {
            return await GetRecordsByFilterAsync(auth, record => record.Tags.Contains(tag));
        }

        public static async Task<List<Record>> GetRecordsByAnyTagAsync(AuthResponse auth, List<string> tags)
        {
            return await GetRecordsByFilterAsync(auth, record => record.Tags.Any(tag => tags.Contains(tag)));
        }

        public static async Task<List<Record>> GetRecordsByAllTagsAsync(AuthResponse auth, List<string> tags)
        {
            return await GetRecordsByFilterAsync(auth, record => tags.All(tag => record.Tags.Contains(tag)));
        }

        public static async Task<List<Record>> GetMessageItemRecordsAsync(AuthResponse auth)
        {
            return await GetRecordsByFilterAsync(auth, record => 
                record.Tags.Contains("message_item") && 
                !(record.Tags.Contains("voice") && record.Tags.Contains("message")));
        }

        public static async Task DeleteRecordAsync(AuthResponse auth, Record record)
        {
            using var deleteClient = new HttpClient();
            
            deleteClient.DefaultRequestHeaders.Add("Authorization", $"res {auth.UserId}:{auth.Token}");
            deleteClient.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
            deleteClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

            var response = await deleteClient.DeleteAsync($"https://api.resonite.com/users/{auth.UserId}/records/{record.Id}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to delete record {record.Id}: {(int)response.StatusCode} {errorContent}");
            }
        }

        public static async Task DeleteRecordsAsync(AuthResponse auth, List<Record> records)
        {
            if (records.Count == 0)
            {
                Console.WriteLine("No records to delete");
                return;
            }

            Console.WriteLine($"Deleting {records.Count} records");

            foreach (var record in records)
            {
                try
                {
                    await DeleteRecordAsync(auth, record);
                    Console.WriteLine($"Successfully deleted record: {record.Name} ({record.Id})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete record {record.Id}: {ex.Message}");
                }
            }

            Console.WriteLine("Records deleted");
        }

        public static void DisplayRecords(List<Record> records)
        {
            if (records.Count == 0)
            {
                Console.WriteLine("No records found");
                return;
            }

            Console.WriteLine($"Found {records.Count} records:");
            foreach (var record in records)
            {
                Console.WriteLine($"  - {record.Name} ({record.Id})");
                Console.WriteLine($"    Path: {record.Path}");
                Console.WriteLine($"    Last Modified: {record.LastModificationTime:yyyy-MM-dd HH:mm:ss}");
                if (record.Tags.Count > 0)
                {
                    Console.WriteLine($"    Tags: {string.Join(", ", record.Tags)}");
                }
                Console.WriteLine();
            }
        }

        public static void DisplayRecordSummary(List<Record> records)
        {
            if (records.Count == 0)
            {
                Console.WriteLine("No records found");
                return;
            }

            Console.WriteLine($"Found {records.Count} records");
            var groupedByName = records.GroupBy(r => r.Name).OrderBy(g => g.Key);
            foreach (var group in groupedByName)
            {
                Console.WriteLine($"  {group.Key}: {group.Count()} record(s)");
            }
        }

        public static async Task<UserProfile?> GetUserProfileAsync(AuthResponse auth)
        {
            using var httpClient = new HttpClient();
            
            httpClient.DefaultRequestHeaders.Add("Authorization", $"res {auth.UserId}:{auth.Token}");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

            var response = await httpClient.GetAsync($"https://api.resonite.com/users/{auth.UserId}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to get user info with status {response.StatusCode}: {responseContent}");
            }

            try
            {
                var userInfo = JsonConvert.DeserializeObject<UserInfo>(responseContent);
                return userInfo?.Profile ?? new UserProfile();
            }
            catch (JsonException ex)
            {
                throw new Exception($"Failed to parse user info response. Response was: {responseContent}", ex);
            }
        }

        public static async Task UpdateUserProfileAsync(AuthResponse auth, UserProfile profile)
        {
            using var httpClient = new HttpClient();
            
            httpClient.DefaultRequestHeaders.Add("Authorization", $"res {auth.UserId}:{auth.Token}");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

            var json = JsonConvert.SerializeObject(profile, Formatting.None, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PutAsync($"https://api.resonite.com/users/{auth.UserId}/profile", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to update user profile with status {response.StatusCode}: {responseContent}");
            }
        }
    }
}
