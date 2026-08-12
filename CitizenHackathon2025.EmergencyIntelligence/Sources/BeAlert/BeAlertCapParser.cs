using System.Xml;
using System.Xml.Linq;

namespace CitizenHackathon2025.EmergencyIntelligence.Sources.BeAlert
{
    internal static class BeAlertCapParser
    {
        private static readonly XNamespace Cap = "urn:oasis:names:tc:emergency:cap:1.2";


        public static IReadOnlyList<BeAlertCapMessage> Parse(Stream stream)
        {
            var settings =
                new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    IgnoreComments = true,
                    IgnoreWhitespace = true,
                    MaxCharactersInDocument = 2_000_000
                };

            using var reader = XmlReader.Create(stream, settings);

            var document = XDocument.Load(reader, LoadOptions.None);

            /*
             * A feed may contain either one CAP <alert>
             * or several embedded alerts.
             */
            var alertElements = document.Descendants(Cap + "alert").ToList();

            /*
             * If <alert> itself is the root,
             * Descendants() does not include itself.
             */
            if (document.Root?.Name == Cap + "alert")
            {
                alertElements.Insert(0, document.Root);
            }


            return alertElements
                .Select(ParseAlert)
                .Where(x => !string.IsNullOrWhiteSpace(x.Identifier))
                .ToList();
        }


        private static BeAlertCapMessage ParseAlert(XElement alert)
        {
            var infos = alert.Elements(Cap + "info").ToList();
            var info = SelectInfo(infos, "fr-BE");
            var areas = info?.Elements(Cap + "area").Select(ParseArea).ToList() ?? [];


            return new BeAlertCapMessage
            {
                Identifier = Value(alert, "identifier"),
                Sender = Value(alert, "sender"),
                Sent = ParseDate(Value(alert, "sent")) ?? DateTimeOffset.UtcNow,
                Status = Value(alert, "status"),
                MessageType = Value(alert, "msgType"),
                Scope = Value(alert, "scope"),
                References = NullableValue(alert, "references"),
                Language = NullableValue(info, "language"),
                Event = NullableValue(info, "event"),
                Urgency = NullableValue(info, "urgency"),
                Severity = NullableValue(info, "severity"),
                Certainty = NullableValue(info, "certainty"),
                Headline = NullableValue(info, "headline"),
                Description = NullableValue(info, "description"),
                Instruction = NullableValue(info, "instruction"),
                Effective = ParseDate(NullableValue(info, "effective")),
                Expires = ParseDate(NullableValue(info, "expires")),
                Areas = areas,
                RawXml = alert.ToString(SaveOptions.DisableFormatting)
            };
        }


        private static XElement? SelectInfo(IReadOnlyList<XElement> infos, string preferredLanguage)
        {
            if (infos.Count == 0)
                return null;


            return infos.FirstOrDefault(x => string.Equals(NullableValue(x,"language"), preferredLanguage, StringComparison.OrdinalIgnoreCase))
                   ?? infos.FirstOrDefault(x => NullableValue(x, "language") ?.StartsWith("fr", StringComparison.OrdinalIgnoreCase) == true)
                   ?? infos[0];
        }


        private static BeAlertCapArea ParseArea(XElement area)
        {
            return new BeAlertCapArea
            {
                AreaDescription =Value(area, "areaDesc"),

                Polygons = area
                    .Elements(Cap + "polygon")
                    .Select(x => x.Value.Trim())
                    .Where(x => x.Length > 0).ToList(),

                Circles = area
                    .Elements(Cap + "circle")
                    .Select(x => x.Value.Trim())
                    .Where(x => x.Length > 0)
                    .ToList()
            };
        }


        private static string Value(XElement? parent, string name)
        {
            return parent ?.Element(Cap + name) ?.Value ?.Trim() ?? "";
        }


        private static string? NullableValue(XElement? parent, string name)
        {
            var value = Value(parent, name);

            return string.IsNullOrWhiteSpace(value) ? null: value;
        }


        private static DateTimeOffset? ParseDate(string? value)
        {
            if (DateTimeOffset.TryParse(value, out var result))
            {
                return result;
            }

            return null;
        }
    }
}
























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.