using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Frontend_MarikinaAlert.Localization
{
    /// <summary>
    /// English -> Filipino text for the resident pages. The English text is the key, so views read
    /// naturally (<c>@T["Report an emergency"]</c>) and any missing entry simply shows the English.
    /// Dispatcher pages stay in English.
    /// </summary>
    public class Translations
    {
        public static readonly IReadOnlyDictionary<string, string> Filipino = new Dictionary<string, string>
        {
            // Layout
            ["Skip to form"] = "Lumaktaw sa form",
            ["Dispatcher login"] = "Login ng dispatcher",
            ["Can't use this form? Call {0} or {1}."] = "Hindi magamit ang form na ito? Tumawag sa {0} o {1}.",
            ["Check my report"] = "Tingnan ang ulat ko",

            // Report form
            ["Report an emergency"] = "Mag-ulat ng emergency",
            ["Tell us what's happening and where. Dispatchers see the most urgent reports first."] =
                "Sabihin kung ano ang nangyayari at saan. Inuuna ng mga dispatcher ang pinakamadaliang ulat.",
            ["Already sent a report?"] = "Nakapagpadala ka na ng ulat?",
            ["Check its status"] = "Tingnan ang status nito",
            ["What is happening?"] = "Ano ang nangyayari?",
            ["Pick one if it fits, then describe it below."] = "Pumili ng isa kung tugma, saka ilarawan sa ibaba.",
            ["Fire"] = "Sunog",
            ["Flames or smoke"] = "Apoy o usok",
            ["Flood"] = "Baha",
            ["Rising water"] = "Mataas na tubig",
            ["Medical"] = "Medical",
            ["Injured or sick"] = "May sugatan o may sakit",
            ["Collapse / Trapped"] = "Gumuho o Naipit",
            ["Building fell or someone is stuck"] = "May gumuho o may naipit",
            ["Unclassified"] = "Hindi pa tukoy",
            ["Critical"] = "Kritikal",
            ["High"] = "Mataas",
            ["Normal"] = "Karaniwan",
            ["E.g. Help! A house is on fire at the corner, someone is trapped inside..."] =
                "Hal. Tulong! May nasusunog na bahay dito sa kanto, may naipit sa loob...",
            ["E.g. In front of Mercury Drug, J.P. Rizal St."] = "Hal. Tapat ng Mercury Drug, J.P. Rizal St.",
            ["E.g. Maria Santos"] = "Hal. Maria Santos",
            ["Describe the emergency"] = "Ilarawan ang emergency",
            ["Tagalog or English is fine. Say if someone is trapped, injured, or unconscious."] =
                "Puwede ang Tagalog o English. Sabihin kung may naipit, sugatan, o walang malay.",
            ["Where are you?"] = "Nasaan ka?",
            ["Select your barangay"] = "Piliin ang barangay mo",
            ["Street or landmark"] = "Kalye o palatandaan",
            ["(helps rescuers find you)"] = "(para mas madali kang mahanap ng rescuers)",
            ["How can we reach you?"] = "Paano ka namin makokontak?",
            ["Optional"] = "Hindi kailangan",
            ["Your name"] = "Pangalan mo",
            ["Mobile number"] = "Numero ng cellphone",
            ["Your report goes straight to Marikina dispatchers over the local network. No internet needed."] =
                "Diretso sa mga dispatcher ng Marikina ang ulat mo sa lokal na network. Hindi kailangan ng internet.",
            ["Couldn't send your report."] = "Hindi naipadala ang ulat mo.",
            ["Check that you're still connected to the Wi-Fi. Your message is saved on this phone."] =
                "Tingnan kung nakakonekta ka pa sa Wi-Fi. Naka-save ang mensahe mo sa phone na ito.",
            ["The system had a problem. Reload the page and send again. Your message is saved on this phone."] =
                "Nagka-problema ang system. I-reload ang page at ipadala ulit. Naka-save ang mensahe mo sa phone na ito.",
            ["Send emergency report"] = "Ipadala ang ulat",
            ["Sending…"] = "Ipinapadala…",
            ["Try again"] = "Subukan ulit",
            ["We restored your unsent report."] = "Ibinalik namin ang hindi pa naipadalang ulat mo.",
            ["Start over"] = "Magsimula ulit",

            // Validation
            ["Choose your barangay so rescuers know where to go."] = "Piliin ang barangay mo para alam ng rescuers kung saan pupunta.",
            ["Choose a barangay from the list."] = "Pumili ng barangay mula sa listahan.",
            ["Tell us what is happening."] = "Sabihin kung ano ang nangyayari.",
            ["Please describe the emergency in a few words."] = "Ilarawan ang emergency sa ilang salita.",
            ["Enter a valid mobile number, e.g. 0917 123 4567."] = "Maglagay ng tamang numero, hal. 0917 123 4567.",

            // Confirmation
            ["Report received"] = "Natanggap ang ulat",
            ["Dispatchers can see it now."] = "Nakikita na ito ng mga dispatcher.",
            ["Reference no."] = "Numero ng ulat",
            ["Location"] = "Lokasyon",
            ["Sorted as"] = "Uri at antas",
            ["Keep your phone on. A dispatcher may call {0}."] = "Huwag patayin ang phone mo. Maaaring tumawag ang dispatcher sa {0}.",
            ["Your tracking code"] = "Ang tracking code mo",
            ["Use it to check the status of your report later. It's also saved on this phone."] =
                "Gamitin ito para tingnan ang status ng ulat mo mamaya. Naka-save din ito sa phone na ito.",
            ["Check status"] = "Tingnan ang status",
            ["While you wait"] = "Habang naghihintay",
            ["Send another report"] = "Magpadala ng isa pang ulat",

            ["Get everyone out and stay out. Don't go back for belongings."] = "Ilabas ang lahat at huwag nang bumalik. Huwag nang balikan ang mga gamit.",
            ["Stay low if there is smoke."] = "Yumuko o gumapang kung may usok.",
            ["Don't use elevators."] = "Huwag gumamit ng elevator.",
            ["Move to higher ground or an upper floor."] = "Pumunta sa mataas na lugar o sa itaas na palapag.",
            ["Turn off the main power switch if it's safe."] = "Patayin ang main switch ng kuryente kung ligtas.",
            ["Don't walk or drive through moving water."] = "Huwag lumusong o magmaneho sa rumaragasang tubig.",
            ["If trapped, cover your mouth and stay still."] = "Kung naipit, takpan ang bibig at huwag gumalaw.",
            ["Tap on a pipe or wall so rescuers can hear you."] = "Kumatok sa tubo o pader para marinig ka ng rescuers.",
            ["Shout only as a last resort to save your strength."] = "Sumigaw lang kung wala nang ibang paraan para makatipid ng lakas.",
            ["Stay with the person and keep them still."] = "Samahan ang pasyente at huwag siyang galawin.",
            ["Press firmly on any bleeding with a clean cloth."] = "Diinan ang sugat na dumudugo gamit ang malinis na tela.",
            ["Don't give food or water to someone who is unconscious."] = "Huwag bigyan ng pagkain o tubig ang walang malay.",
            ["Move to a safe place if you can."] = "Lumipat sa ligtas na lugar kung kaya.",
            ["Keep your phone on and nearby."] = "Huwag patayin ang phone at ilapit ito sa iyo.",
            ["Send another report if things get worse."] = "Magpadala ulit ng ulat kung lumala ang sitwasyon.",

            // Status page
            ["Enter the tracking code from your confirmation page."] = "Ilagay ang tracking code mula sa confirmation page mo.",
            ["Tracking code"] = "Tracking code",
            ["We couldn't find a report with that code. Check the code and try again."] =
                "Walang ulat na may ganyang code. Pakisuri ang code at subukan ulit.",
            ["Reports sent from this phone"] = "Mga ulat na ipinadala mula sa phone na ito",
            ["Received"] = "Natanggap",
            ["Your report is in the dispatchers' queue."] = "Nasa listahan na ng mga dispatcher ang ulat mo.",
            ["Verified by a dispatcher"] = "Na-verify ng dispatcher",
            ["A dispatcher confirmed your report."] = "Kinumpirma ng dispatcher ang ulat mo.",
            ["Help requested"] = "Humingi na ng tulong",
            ["Your report was sent to {0}."] = "Ipinasa ang ulat mo sa {0}.",
            ["Resolved"] = "Tapos na",
            ["Dispatchers marked this incident as handled."] = "Minarkahan ng mga dispatcher na naasikaso na ito.",
            ["Closed without a response"] = "Isinara nang walang aksyon",
            ["A dispatcher marked this report as not needing a response. If you still need help, send a new report or call 161."] =
                "Minarkahan ng dispatcher na hindi na kailangan ng aksyon ang ulat na ito. Kung kailangan mo pa rin ng tulong, magpadala ng bagong ulat o tumawag sa 161.",
            ["Last updated {0}"] = "Huling update {0}",
            ["Check another code"] = "Ibang code",

            // Errors
            ["Page not found"] = "Hindi nahanap ang page",
            ["Something went wrong"] = "May nangyaring mali",
            ["We couldn't find that page."] = "Hindi namin nahanap ang page na iyon.",
            ["The system hit a problem while handling your request."] = "Nagka-problema ang system habang pinoproseso ang request mo.",
            ["If this is an emergency, use the report form or call {0}."] = "Kung emergency ito, gamitin ang report form o tumawag sa {0}.",
        };

        public static bool IsFilipino => CultureInfo.CurrentUICulture.Name.StartsWith("fil", StringComparison.OrdinalIgnoreCase);
    }

    public class DictionaryStringLocalizer : IStringLocalizer
    {
        public LocalizedString this[string name]
        {
            get
            {
                if (Translations.IsFilipino && Translations.Filipino.TryGetValue(name, out var value))
                    return new LocalizedString(name, value);
                return new LocalizedString(name, name, resourceNotFound: Translations.IsFilipino);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var template = this[name];
                return new LocalizedString(name, string.Format(template.Value, arguments), template.ResourceNotFound);
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Translations.Filipino.Keys.Select(k => this[k]);
    }

    // Replaces the default .resx-based factory so IStringLocalizer<T> and DataAnnotations use the dictionary above.
    public class DictionaryStringLocalizerFactory : IStringLocalizerFactory
    {
        private static readonly DictionaryStringLocalizer Localizer = new();

        public IStringLocalizer Create(Type resourceSource) => Localizer;

        public IStringLocalizer Create(string baseName, string location) => Localizer;
    }
}
