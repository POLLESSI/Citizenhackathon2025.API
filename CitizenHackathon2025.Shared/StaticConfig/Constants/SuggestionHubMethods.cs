// CitizenHackathon2025.Shared.StaticConfig.Constants.suggestionHubMethods.cs

namespace CitizenHackathon2025.Shared.StaticConfig.Constants
{
    /// <summary>Constantes partagées pour le SuggestionHub (SignalR).</summary>
    public static class SuggestionHubMethods
    {
        /// <summary>Chemin du hub (doit matcher le MapHub côté API).</summary>
        public const string HubPath = "/hubs/suggestionHub";

        /// <summary>Événements envoyés par le serveur vers les clients.</summary>
        public static class ToClient
        {
            /// <summary>Ping de rafraîchissement (sans payload).</summary>
            public const string NewSuggestion = "NewSuggestion";

            /// <summary>Suggestion envoyée (payload SuggestionDTO).</summary>
            public const string ReceiveSuggestion = "ReceiveSuggestion";
        }

        /// <summary>Méthodes invoquées par les clients sur le hub.</summary>
        public static class FromClient
        {
            /// <summary>Demande de ping (sans argument).</summary>
            public const string RefreshSuggestion = "RefreshSuggestion";

            /// <summary>Envoi d’une suggestion (payload SuggestionDTO).</summary>
            public const string SendSuggestion = "SendSuggestion";
        }
    }
}