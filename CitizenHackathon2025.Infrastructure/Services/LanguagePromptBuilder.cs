using CitizenHackathon2025.Application.Interfaces;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class LanguagePromptBuilder : ILanguagePromptBuilder
    {
        public string BuildLanguageInstruction(string responseLanguage)
        {
            var lang = string.IsNullOrWhiteSpace(responseLanguage) ? "fr-FR" : responseLanguage.Trim();

            if (lang.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
            {
                return """
                    Отвечай только на русском языке.
                    Не переходи на французский или английский язык.
                    Названия мест и мероприятий копируй точно
                    из предоставленного контекста.
                    Не переводи и не выдумывай названия мест.
                    Используй только расстояния,
                    явно указанные в контексте.
                    """;
            }

            if (lang.StartsWith("ar", StringComparison.OrdinalIgnoreCase))
            {
                return """
                    أجب باللغة العربية فقط.
                    لا تنتقل إلى الفرنسية أو الإنجليزية.
                    استخدم فقط الأماكن والمسافات الموجودة
                    صراحةً في السياق المقدم.
                    لا تخترع أماكن أو مسافات.
                    """;
            }

            return lang switch
            {
                "fr-FR" => "Réponds en français.",

                "fr-BE" => "Réponds en français belge.",

                "en-US" or "en-GB" => "Answer in English.",

                "nl-NL" or "nl-BE" => "Antwoord in het Nederlands.",

                "de-DE" or "de-BE" => "Antworte auf Deutsch.",

                "it-IT" => "Rispondi in italiano.",

                "es-ES" => "Responde en español.",

                "zh-CN" => "请用中文回答。",

                "ja-JP" => "日本語で答えてください。",

                "wa-central" => """
                    Responds en mode wallon central experimental em fî.

                    IMPORTANT :
                    - Titles must be in French.
                    - Do not write "Central Walloon".
                    - Do not write "French clarification".
                    - Use exactly this format :

                    1) Simple Central Walloon :
                    [short, cautious, understandable phrase,
                    with only a few certain Walloon words]
                    2) French Clarification :
                    [clear reformulation in standard French]

                    If you are not sure about Walloon,
                    write mainly in French.
                    Never invent Walloon vocabulary.
                    """,

                _ =>
                    "Answer in French."
            };
        }
    }
}





































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.