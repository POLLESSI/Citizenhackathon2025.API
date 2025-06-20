using CitizenHackathon2025.Shared.Utils;
using Microsoft.Data.SqlClient;

public class GptInteractionService
{
    public void SavePrompt(string prompt, string response)
    {
        string promptHash = Convert.ToHexString(HashHelper.HashPassword(prompt, "static-stamp-gpt"));

        // Example of recording via Dapper or ADO.NET
        var command = new SqlCommand("INSERT INTO GptInteractions (Prompt, PromptHash, Response) VALUES (@prompt, @promptHash, @response)");
        command.Parameters.AddWithValue("@prompt", prompt);
        command.Parameters.AddWithValue("@promptHash", promptHash);
        command.Parameters.AddWithValue("@response", response);

        // ... Run command here with SqlConnection
    }
}

















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.