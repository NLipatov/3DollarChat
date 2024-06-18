using System.Reflection;

namespace Ethachat.Client.Services.VersioningService;

public class VersionService : IVersionService
{
    /// <summary>
    /// Gets string fields provided by GitVersion library
    /// </summary>
    private FieldInfo[] GitVersionFields =>
        Assembly.GetExecutingAssembly().GetType("GitVersionInformation")?.GetFields() ?? [];

    #region GitVersionKeys
    private const string SemVerKey = "SemVer";
    private const string LastCommitDateKey = "CommitDate";
    #endregion

    private string GetItem(string key) =>
        GitVersionFields.FirstOrDefault(x => x.Name == key)?.GetValue(null)?.ToString() ?? string.Empty;

    public string GetCommitDate() => GetItem(LastCommitDateKey);

    public string GetSemVer() => GetItem(SemVerKey);
}