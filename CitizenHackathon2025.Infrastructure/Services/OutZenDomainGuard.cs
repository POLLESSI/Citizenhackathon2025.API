namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class OutZenDomainGuard
    {
        private static readonly string[] BlockedTerms =
        {
        "missile",
        "explosif",
        "bombe",
        "engin explosif",
        "arme",
        "attentat",
        "cibler la foule",
        "émeute",
        "haine",
        "club libertin",
        "prostitution",
        "sexe",
        "escort",
        "vente voiture",
        "acheter voiture",
        "sabotage",
        "drogue",
        "trafic de drogue",
        "vente de drogue",
        "cocaine",
        "héroïne",
        "meth",
        "crack",
        "opium",
        "cannabis",
        "marijuana",
        "thérapie",
        "cocaïne",
        "héroine",
        "méth",
        "amphétamine",
        "amphét",
        "hash",
        "ecstasy",
        "blow",
        "crystal meth",
        "cristal meth",
        "cristal",
        "shit",
        "dope",
        "weed",
        "doves",
        "mitsubishis",
        "yokes",
        "coke",
        "shamrocks",
        "gear",
        "junk",
        "Charlie",
        "neige",
        "snow",
        "speed",
        "whizz",
        "uppers",
        "sniffer",
        "sniffer de la drogue",
        "sniffer de la colle",
        "lsd",
        "acide",
        "méthamphétamine",
        "méthamphétamines",
        "méthamphét",
        "MDMA",
        "Kétamine",
        "Két",
        "phencyclidine",
        "PCP",
        "GHB",
        "Drogue du viol",
        "Drogue du violeur",
        "peroxide d'azote",
        "protoxyde d'azote",
        "lock the target",
        "bait the line",
        "slowly spread the net",
        "catch the man",
        "catch the woman",
        "catch the child",
        "Cibler la foule"
    };

        private static readonly string[] AllowedTerms =
        {
        "tourisme",
        "culture",
        "patrimoine",
        "musée",
        "château",
        "abbaye",
        "balade",
        "randonnée",
        "nature",
        "événement",
        "restaurant",
        "café",
        "brasserie",
        "météo",
        "trafic",
        "itinéraire",
        "activité",
        "visite",
        "alternative",
        "cool",
        "zen",
        "pour enfants",
    };

        public OutZenGuardResult CheckInput(string? prompt)
        {
            var text = Normalize(prompt);

            if (string.IsNullOrWhiteSpace(text))
            {
                return OutZenGuardResult.Deny(
                    "Votre demande est vide.");
            }

            if (BlockedTerms.Any(x => text.Contains(x, StringComparison.OrdinalIgnoreCase)))
            {
                return OutZenGuardResult.Deny(
                    "Je ne peux pas aider pour ce type de demande. OutZen est limité aux suggestions touristiques, culturelles, locales et à la sécurité douce des visiteurs.");
            }

            if (!AllowedTerms.Any(x => text.Contains(x, StringComparison.OrdinalIgnoreCase)))
            {
                return OutZenGuardResult.SoftAllow(
                    "Demande acceptée avec restriction au domaine touristique et culturel.");
            }

            return OutZenGuardResult.Allow();
        }

        public OutZenGuardResult CheckOutput(string? response)
        {
            var text = Normalize(response);

            if (BlockedTerms.Any(x => text.Contains(x, StringComparison.OrdinalIgnoreCase)))
            {
                return OutZenGuardResult.Deny(
                    "La réponse générée a été bloquée car elle sort du domaine autorisé d’OutZen.");
            }

            return OutZenGuardResult.Allow();
        }

        private static string Normalize(string? value)
            => (value ?? string.Empty).Trim().ToLowerInvariant();
    }

    public sealed record OutZenGuardResult(
        bool Allowed,
        bool SoftRestricted,
        string? Message)
    {
        public static OutZenGuardResult Allow()
            => new(true, false, null);

        public static OutZenGuardResult SoftAllow(string message)
            => new(true, true, message);

        public static OutZenGuardResult Deny(string message)
            => new(false, false, message);
    }
}
