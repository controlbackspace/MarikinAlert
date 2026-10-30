using System.Security.Cryptography;
using Frontend_MarikinaAlert.Repositories;

namespace Frontend_MarikinaAlert.Services
{
    // Random codes residents use on "Check my report". Shared by the resident form and the dispatcher's "New report".
    public static class TrackingCodes
    {
        // No 0/O or 1/I/L so codes are easy to read aloud and type on a phone.
        private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

        public static async Task<string> NewAsync(IReportRepository reports)
        {
            // 31^8 possible codes; a collision is practically impossible, but check anyway.
            while (true)
            {
                var code = RandomNumberGenerator.GetString(Alphabet, 8);
                if (await reports.GetByTrackingCodeAsync(code) is null) return code;
            }
        }

        public static string? Normalize(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            var cleaned = new string(code.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            return cleaned.Length == 8 && cleaned.All(Alphabet.Contains) ? cleaned : null;
        }
    }
}
