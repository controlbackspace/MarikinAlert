using System.ComponentModel.DataAnnotations;

namespace Frontend_MarikinaAlert.Models
{
    // A responding agency and how to reach it. Dispatchers keep the numbers up to date on the Agencies page.
    public class Agency
    {
        // Digits plus the usual separators; short codes like 161 or 911 are allowed.
        public const string PhonePattern = @"^[0-9+()\-\s]{3,30}$";
        private const string PhoneError = "Enter a phone number, e.g. 161 or (02) 8123 4567.";

        public int Id { get; set; }

        [Required(ErrorMessage = "Enter the agency name."), MaxLength(100)]
        public string Name { get; set; } = "";

        [MaxLength(30), RegularExpression(PhonePattern, ErrorMessage = PhoneError)]
        public string? Hotline { get; set; }

        [MaxLength(30), RegularExpression(PhonePattern, ErrorMessage = PhoneError)]
        public string? BackupNumber { get; set; }

        [MaxLength(40)]
        public string? RadioChannel { get; set; }

        // Station address, what they respond to, hours, etc.
        [MaxLength(200)]
        public string? Notes { get; set; }

        // Where the number was checked (official page, memo, phone call), so stale numbers can be traced.
        [MaxLength(200)]
        public string? NumberSource { get; set; }

        public int SortOrder { get; set; }

        // Inactive agencies stay in the database for old reports but are hidden from the relay list.
        public bool IsActive { get; set; } = true;

        // tel: links only want digits and a leading +; "(02) 8123-4567" becomes "tel:0281234567".
        public static string TelHref(string number) =>
            "tel:" + new string(number.Where(c => char.IsDigit(c) || c == '+').ToArray());
    }

    public enum ContactMethod
    {
        Phone = 0,
        Radio = 1
    }

    public enum ContactOutcome
    {
        Answered = 0,
        NoAnswer = 1,
        Busy = 2      // busy or could not connect
    }

    // One call or radio attempt to an agency about a report. Kept even after the report closes.
    public class ContactAttempt
    {
        public int Id { get; set; }

        public int ReportId { get; set; }

        // Copied as text, not a foreign key, so renaming an agency doesn't rewrite who was actually called.
        [Required, MaxLength(100)]
        public string AgencyName { get; set; } = "";

        [MaxLength(30)]
        public string? Number { get; set; }

        public ContactMethod Method { get; set; }
        public ContactOutcome Outcome { get; set; }

        [MaxLength(100)]
        public string? LoggedBy { get; set; }

        public DateTime AtUtc { get; set; }

        public string OutcomeText => (Method, Outcome) switch
        {
            (ContactMethod.Radio, ContactOutcome.Answered) => "reached by radio",
            (ContactMethod.Radio, _) => "no reply on radio",
            (_, ContactOutcome.Answered) => "answered",
            (_, ContactOutcome.NoAnswer) => "no answer",
            _ => "busy / couldn't connect"
        };
    }
}
