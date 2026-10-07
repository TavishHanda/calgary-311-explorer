namespace Calgary311.Web.Services;

/// <summary>
/// The short codes the City puts at the front of service and department names,
/// like "WRS - Cart Management" or "OS - Waste and Recycling Services", and what they stand for.
/// </summary>
public static class Abbreviations
{
    // Only codes with a confirmed meaning are listed; anything else is shown as-is rather than guessed.
    // Sources: the City's 311 acronym list (calgary.ca/311/summary-reports-reference.html), its list of
    // departments, and codes that the data ties to exactly one department (e.g. every "AT" request is
    // handled by "CPFS - Assessment and Tax").
    private static readonly Dictionary<string, string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        // Departments (the code before the dash in the Department column).
        ["CPFS"] = "Corporate Planning & Financial Services",
        ["CS"] = "Community Services",
        ["IS"] = "Infrastructure Services",
        ["LLSS"] = "Law, Legislative Services & Security",
        ["OS"] = "Operational Services",
        ["PDS"] = "Planning & Development Services",
        ["PICS"] = "People, Innovation & Collaboration Services",

        // Service types, from the City's 311 acronym list.
        ["AS"] = "Animal Services",
        ["CEMA"] = "Calgary Emergency Management Agency",
        ["CFD"] = "Calgary Fire Department",
        ["CT"] = "Calgary Transit",
        ["WATS"] = "Water Services",
        ["WRS"] = "Waste & Recycling Services",

        // Service types, from the one department that handles each of them.
        ["AT"] = "Assessment and Tax",
        ["CAO"] = "Chief Administrator's Office",
        ["CHO"] = "Chief Housing Office",
        ["CP"] = "Community Planning",
        ["CPBS"] = "Capital Planning and Business Services",
        ["CRP"] = "City and Regional Planning",
        ["DBBS"] = "Development, Business and Building Services",
        ["ERR"] = "Environmental Risk & Resilience",
        ["HR"] = "Human Resources",
        ["IT"] = "Information Technology",
        ["MC"] = "Marketing and Communication",
        ["PD"] = "Project Development",
        ["PSD"] = "Public Spaces Delivery",
        ["REDS"] = "Real Estate and Development Services",
        ["RSP"] = "Recreation and Social Programs",
        ["UD"] = "Utilities Delivery",
        ["VFH"] = "Vehicle for Hire (taxis, limousines and ride sharing)",
    };

    /// <summary>Splits "WRS - Cart Management" into ("WRS", "Cart Management"), or returns null if there's no dash.</summary>
    // (Not "Rest": C# tuples already use that name for their eighth-and-later elements.)
    public static (string Code, string Name)? Split(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        var dash = name.IndexOf(" - ", StringComparison.Ordinal);
        return dash > 0 ? (name[..dash], name[(dash + 3)..]) : null;
    }

    /// <summary>What a code stands for, or null if it isn't a known abbreviation.</summary>
    public static string? Meaning(string code) => Known.GetValueOrDefault(code);

    /// <summary>"WRS - Cart Management" becomes "Waste & Recycling Services: Cart Management". Unknown codes are left alone.</summary>
    public static string Describe(string name) =>
        Split(name) is var (code, rest) && Meaning(code) is { } meaning ? $"{meaning}: {rest}" : name;

    /// <summary>The known codes used in these names, with their meanings, sorted by code. For a legend.</summary>
    public static List<KeyValuePair<string, string>> Legend(IEnumerable<string?> names) =>
        names
            .Select(Split)
            .Where(split => split is not null)
            .Select(split => split!.Value.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(Known.ContainsKey)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(code => KeyValuePair.Create(code, Known[code]))
            .ToList();
}
