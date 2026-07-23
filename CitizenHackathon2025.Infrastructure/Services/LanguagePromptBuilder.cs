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

                    Названия мест и мероприятий являются
                    неизменяемыми собственными именами.

                    ОБЯЗАТЕЛЬНО:
                    - Копируй каждое название места точно,
                      символ за символом, из контекста.
                    - Сохраняй латинский алфавит названия.
                    - Не переводи название на русский язык.
                    - Не транслитерируй название кириллицей.
                    - Переводи только описание вокруг названия.
                    - Используй только расстояния,
                      явно указанные в контексте.
                    - Не добавляй места,
                      отсутствующие в контексте.

                    Правильный пример:
                    1. Centre Culturel de Stavelot — 0.4 km —
                       культурный центр.

                    Неправильный пример:
                    1. Культурный центр Ставело — 0.4 km.
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