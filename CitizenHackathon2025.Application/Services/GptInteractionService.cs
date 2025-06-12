using CitizenHackathon2025.Shared.Utils;
using Microsoft.Data.SqlClient;

public class GptInteractionService
{
    public void SavePrompt(string prompt, string response)
    {
        string promptHash = HashHelper.GetSha256Hash(prompt);

        // Example of recording via Dapper or ADO.NET
        var command = new SqlCommand("INSERT INTO GptInteractions (Prompt, PromptHash, Response) VALUES (@prompt, @promptHash, @response)");
        command.Parameters.AddWithValue("@prompt", prompt);
        command.Parameters.AddWithValue("@promptHash", promptHash);
        command.Parameters.AddWithValue("@response", response);

        // ... Run command
    }
}