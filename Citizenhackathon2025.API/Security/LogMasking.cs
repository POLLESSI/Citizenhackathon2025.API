using Serilog;
using System.Text.RegularExpressions;

namespace CitizenHackathon2025.API.Security
{
    public static class LogMasking
    {
        static readonly Regex Email = new(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled);
        static readonly Regex Bearer = new(@"Bearer\s+[A-Za-z0-9\-\._~\+\/]+=*", RegexOptions.Compiled);

        public static string Scrub(string input) =>
            string.IsNullOrEmpty(input) ? input :
            Bearer.Replace(Email.Replace(input, "***@***"), "Bearer ***");
    }

    // use
    //Log.Information("Body={Body}", LogMasking.Scrub(bodyString));
}
