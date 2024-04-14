using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DiscordLootboxOpener;

internal class Program
{
    private const string Token = "TOKEN";
    private const string Url = "https://discord.com/api/v9/users/@me/lootboxes/open";
    private const string Referer = "https://discord.com/channels/@me";
    private const int DelayMilliseconds = 2300;

    private static int _rateLimitedCount = 0;
    private static int _errorCount = 0;

    static async Task Main(string[] args)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", Token);
            client.DefaultRequestHeaders.Add("sec-ch-ua",
                "\"Google Chrome\";v=\"123\", \"Not:A-Brand\";v=\"8\", \"Chromium\";v=\"123\"");
            client.DefaultRequestHeaders.Add("X-Super-Properties", 
                "ewogICJvcyI6ICIiLAogICJicm93c2VyIjogIiIsCiAgImR" +
                "ldmljZSI6ICIiLAogICJzeXN0ZW1fbG9jYWxlIjogIiIsCiAgImJ" +
                "yb3dzZXJfdXNlcl9hZ2VudCI6ICIiLAogICJicm93c2VyX3ZlcnN" +
                "pb24iOiAiIiwKICAib3NfdmVyc2lvbiI6ICIiLAogICJyZWZlcnJ" +
                "lciI6ICIiLAogICJyZWZlcnJpbmdfZG9tYWluIjogIiIsCiAgInJ" +
                "lZmVycmVyX2N1cnJlbnQiOiAiIiwKICAicmVmZXJyaW5nX2RvbWF" +
                "pbl9jdXJyZW50IjogIiIsCiAgInJlbGVhc2VfY2hhbm5lbCI6ICI" +
                "iLAogICJjbGllbnRfYnVpbGRfbnVtYmVyIjogMjgxODA5LAogICJ" +
                "jbGllbnRfZXZlbnRfc291cmNlIjogbnVsbAp9");

            while (true)
            {
                try
                {
                    await SendPostRequest(client);
                }
                catch (Exception ex)
                {
                    _errorCount++;
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }

                await Task.Delay(DelayMilliseconds);
            }
        }
    }

    static async Task SendPostRequest(HttpClient client)
    {
        var response = await client.PostAsync(Url, null);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var jsonObject = JObject.Parse(content);

            if (jsonObject.ContainsKey("user_lootbox_data"))
            {
                var openedItems = jsonObject["user_lootbox_data"]["opened_items"];
                int totalItems = 0;

                foreach (var item in openedItems.Children())
                {
                    totalItems += item.First.Value<int>();
                }

                Console.WriteLine(
                    $"Total items: {totalItems} (rate limited: {_rateLimitedCount}, errors: {_errorCount})");
            }
            else
            {
                Console.WriteLine("No lootbox data found in the response.");
            }
        }
        else
        {
            var content = await response.Content.ReadAsStringAsync();
            var jsonObject = JObject.Parse(content);

            if (jsonObject.ContainsKey("retry_after"))
            {
                var retryAfterSeconds = jsonObject.Value<double>("retry_after");
                int milliseconds = (int)(retryAfterSeconds * 1000);

                _rateLimitedCount++;
                Console.WriteLine($"Rate limited. Waiting for {retryAfterSeconds} seconds.");
                await Task.Delay(milliseconds);
            }
            else
            {
                _errorCount++;
                Console.WriteLine($"Failed to send request. Status code: {response.StatusCode}");
            }
        }
    }
}