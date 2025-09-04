using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Infrastructure.Services
{
    /// <summary>
    /// Service responsible for the user interface presentation logic.
    /// For example: colors, icons, visual formatting.
    /// </summary>
    public class UIService
    {
        public CrowdInfoUIDTO EnhanceCrowdInfo(CrowdInfoDTO dto)
        {
            var level = dto.CrowdLevel;

            var ui = new CrowdInfoUIDTO
            {
                Original = dto,
                Icon = GetIcon(level),
                Color = GetColor(level),
                VisualLevel = GetVisualLevel(level)
            };

            return ui;
        }

        private static string GetIcon(int level)
        {
            if (level <= 3) return "✅";
            if (level <= 6) return "⚠️";
            if (level <= 8) return "🔥";
            return "⛔";
        }

        private static string GetColor(int level)
        {
            if (level <= 3) return "#4CAF50";   // Green
            if (level <= 6) return "#FFC107";   // Yellow
            if (level <= 8) return "#FF5722";   // dark orange
            return "#D32F2F";                   // red
        }

        private static string GetVisualLevel(int level)
        {
            if (level <= 3) return "Low";
            if (level <= 6) return "Medium";
            if (level <= 8) return "High";
            return "Critical";
        }
    }
}
public class WeatherForecastUIDTO
{
#nullable disable
    public WeatherForecastDTO Original { get; set; }
    public string Icon { get; set; }
    public string DisplayDate { get; set; }
    public string TemperatureColor { get; set; }
}

public class TrafficConditionUIDTO
{
#nullable disable
    public TrafficConditionDTO Original { get; set; }
    public string Icon { get; set; }
    public string DisplayDate { get; set; }
    public string CongestionColor { get; set; }
}








































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.