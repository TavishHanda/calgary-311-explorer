namespace Calgary311.Web;

/// <summary>
/// Settings for the City of Calgary open data API, bound from the "OpenCalgary" section of appsettings.json.
/// </summary>
public class OpenCalgaryOptions
{
    public const string SectionName = "OpenCalgary";

    /// <summary>Base URL of the Socrata API, e.g. https://data.calgary.ca/resource/</summary>
    public string BaseUrl { get; set; } = "https://data.calgary.ca/resource/";

    /// <summary>The 311 Service Requests dataset ID.</summary>
    public string DatasetId { get; set; } = "iahh-g8bj";

    /// <summary>How many days back the first sync should load.</summary>
    public int InitialSyncDays { get; set; } = 90;

    /// <summary>Rows per API request when paging through results.</summary>
    public int PageSize { get; set; } = 5000;

    /// <summary>Optional Socrata app token for higher rate limits. Leave empty to run without one.</summary>
    public string? AppToken { get; set; }
}
