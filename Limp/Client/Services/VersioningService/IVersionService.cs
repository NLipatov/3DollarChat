namespace Ethachat.Client.Services.VersioningService;

public interface IVersionService
{
    string GetCommitDate();
    string GetSemVer();
}