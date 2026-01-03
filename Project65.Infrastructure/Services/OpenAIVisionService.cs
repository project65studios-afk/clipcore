using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Project65.Core.Interfaces;

namespace Project65.Infrastructure.Services;

public class OpenAIVisionService : IVisionService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenAIVisionService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenAI:ApiKey"] ?? "";
    }

    public async Task<string[]> AnalyzeImageAsync(Stream imageStream)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            // Fallback for demo/testing without key
            return new[] { "AI_Analysis_Pending", "No_API_Key" };
        }

        try 
        {
            // Convert stream to base64
            string base64Image;
            using (var memoryStream = new MemoryStream())
            {
                await imageStream.CopyToAsync(memoryStream);
                base64Image = Convert.ToBase64String(memoryStream.ToArray());
            }

            var requestBody = new
            {
                model = "gpt-4o-mini", // Cost effective
                messages = new object[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = "Analyze the image and identify the main vehicle. Return ONLY a JSON array of strings in this order: ['Type' (Car/Motorcycle), 'Make', 'Model', 'Color', 'Rider/Driver' (e.g. 'Man', 'Woman', or 'Rider'), and 3-5 visual keywords]. Avoid 'Unknown' if possible; make a best guess based on visual evidence. Example: [\"Motorcycle\", \"Yamaha\", \"R6\", \"Blue\", \"Male Rider\", \"Track\", \"Knee Down\"]" },
                            new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64Image}" } }
                        }
                    }
                },
                max_tokens = 100
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorDetail = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[VisionService] HTTP Error: {response.StatusCode} - {errorDetail}");
                return new[] { $"Error: {response.StatusCode}" };
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OpenAIResponse>(responseString);

            var content = result?.choices?[0]?.message?.content?.Trim();
            
            // Clean content to ensure valid JSON array (remove markdown code blocks if any)
            if (!string.IsNullOrEmpty(content))
            {
                content = content.Replace("```json", "").Replace("```", "").Trim();
                // Attempt to parse array
                try 
                {
                    var tags = JsonSerializer.Deserialize<string[]>(content);
                    return tags ?? Array.Empty<string>();
                }
                catch
                {
                    // Fallback if AI returns non-JSON text
                    return new[] { "AI_Parsing_Error" };
                }
            }

            return Array.Empty<string>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VisionService] Error: {ex.Message}");
            // Return actual error for debugging UI
            var shortError = ex.Message.Length > 20 ? ex.Message.Substring(0, 20) : ex.Message;
            return new[] { $"Error: {shortError}" };
        }
    }

    // Helper classes for OpenAI Response
    private class OpenAIResponse
    {
        public Choice[] choices { get; set; } = null!;
    }

    private class Choice
    {
        public Message message { get; set; } = null!;
    }

    private class Message
    {
        public string content { get; set; } = null!;
    }

    public async Task<string> GenerateBatchSummaryAsync(IEnumerable<string> imageUrls)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return "AI summary generation is currently disabled (No API Key).";
        }

        try
        {
            var contentList = new List<object>();
            contentList.Add(new { type = "text", text = "I am providing a batch of images from a single event. Based on these images, generate a punchy, descriptive, and engaging summary for the event (2-3 sentences max). Focus on the atmosphere, the action, and the subjects." });

            foreach (var url in imageUrls)
            {
                contentList.Add(new { type = "image_url", image_url = new { url = url } });
            }

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new object[]
                {
                    new
                    {
                        role = "user",
                        content = contentList.ToArray()
                    }
                },
                max_tokens = 300
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                 var errorDetail = await response.Content.ReadAsStringAsync();
                 Console.WriteLine($"[VisionService] Batch Summary HTTP Error: {response.StatusCode} - {errorDetail}");
                 return "Error generating AI summary.";
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OpenAIResponse>(responseString);
            return result?.choices?[0]?.message?.content?.Trim() ?? "Summary generation failed.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VisionService] Batch Summary Error: {ex.Message}");
            return "Error during summary generation.";
        }
    }
}
