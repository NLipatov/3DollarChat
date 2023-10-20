using System.Collections.Concurrent;
using LimpShared.Models.Message.DataTransfer;

namespace Limp.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Helpers;

public static class DataTransferHelper
{
    private static ConcurrentDictionary<Guid, List<Package>> ReceivedFileIdToPackages = new();
    private static ConcurrentDictionary<Guid, List<Package>> SendedFileIdPackages = new();
}