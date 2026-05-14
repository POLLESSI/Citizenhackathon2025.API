using CitizenHackathon2025.Application.Interfaces;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class LanguagePromptBuilder : ILanguagePromptBuilder
    {
        public string BuildLanguageInstruction(string responseLanguage)
        {
            var lang = string.IsNullOrWhiteSpace(responseLanguage)
                ? "fr-FR"
                : responseLanguage.Trim();

            return lang switch
            {
                "fr-FR" => "Réponds en français.",
                "fr-BE" => "Réponds en français belge.",
                "en-US" or "en-GB" => "Answer in English.",
                "nl-NL" => "Antwoord in het Nederlands.",
                "de-DE" => "Antworte auf Deutsch.",
                "it-IT" => "Rispondi in italiano.",
                "es-ES" => "Responde en español.",
                "ru-RU" => "Отвечай на русском языке.",
                "zh-CN" => "请用中文回答。",
                "ja-JP" => "日本語で答えてください。",

                "wa-central" => """
                        Réponds en mode wallon central expérimental.

                        IMPORTANT :
                        - Les titres doivent être en français.
                        - N'écris pas "Central Walloon".
                        - N'écris pas "French clarification".
                        - Utilise exactement ce format :

                        1) Wallon central simple :
                        [phrase courte, prudente, compréhensible, avec seulement quelques mots wallons sûrs]

                        2) Clarification française :
                        [reformulation claire en français standard]

                        Si tu n'es pas sûr du wallon, écris surtout en français.
                        N'invente jamais de vocabulaire wallon.
                        """,

                _ => "Answer in French."
            };
        }
    }
}





































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.