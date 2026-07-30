namespace FliqPayroll.Core.Options;

public class GmailOptions
{
    public const string SectionName = "Gmail";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = "fliqdeveloper@gmail.com";
    public string RedirectUri { get; set; } = string.Empty;
    /// <summary>Relative to content root, or absolute path.</summary>
    public string TokenStorePath { get; set; } = "App_Data/gmail-oauth.tokens";

    /// <summary>
    /// Google App Password for the sender account. When set, SMTP authenticates with it
    /// instead of OAuth, which avoids Google's restricted-scope verification requirement.
    /// </summary>
    public string AppPassword { get; set; } = string.Empty;

    /// <summary>Google displays app passwords in groups of four; spaces are not part of the secret.</summary>
    public string NormalizedAppPassword =>
        string.IsNullOrWhiteSpace(AppPassword) ? string.Empty : AppPassword.Replace(" ", string.Empty);

    public bool UsesAppPassword => NormalizedAppPassword.Length > 0;
}
