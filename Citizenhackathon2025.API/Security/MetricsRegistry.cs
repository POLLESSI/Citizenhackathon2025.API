using Prometheus;

namespace CitizenHackathon2025.API.Security
{
    public static class MetricsRegistry
    {
        public static readonly Counter AiPromptCount =
            Metrics.CreateCounter("ch2025_ai_prompt_total", "Total prompts sent to AI",
                new CounterConfiguration { LabelNames = new[] { "road", "model" } });
    }

    // Use
    //MetricsRegistry.AiPromptCount.WithLabels("/api/gpt/ask", "gpt-4o").Inc();
}
