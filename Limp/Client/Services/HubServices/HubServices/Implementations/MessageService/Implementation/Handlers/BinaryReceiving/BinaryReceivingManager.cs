using System.Collections.Concurrent;
using Ethachat.Client.ClientOnlyModels;
using EthachatShared.Models.Message.DataTransfer;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.BinaryReceiving;

public class BinaryReceivingManager : IBinaryReceivingManager
{
    private ConcurrentDictionary<Guid, Metadata> FileIdToMetadata = new();
    private ConcurrentDictionary<Guid, ClientPackage[]> FileIdToBytes = new();

    public void StoreMetadata(Metadata metadata)
    {
        FileIdToMetadata.TryAdd(metadata.DataFileId, metadata);
    }

    public bool StoreFile(Guid fileId, ClientPackage clientPackage)
    {
        FileIdToMetadata.TryGetValue(fileId, out var metadata);
        
        if (!FileIdToBytes.ContainsKey(fileId))
        {
            FileIdToBytes.TryAdd(fileId, new ClientPackage[metadata!.ChunksCount]);
        }
        
        FileIdToBytes.AddOrUpdate(fileId,
            _ => [clientPackage],
            (_, existingData) =>
            {
                existingData[clientPackage.Index] = clientPackage;
                return existingData;
            });
        
        FileIdToBytes.TryGetValue(fileId, out var packages);
        
        return metadata?.ChunksCount == packages?.Where(x=>x is not null).Count();
    }

    public ClientPackage[] PopData(Guid fileId)
    {
        FileIdToBytes.TryGetValue(fileId, out var data);
        FileIdToBytes.TryRemove(fileId, out var _);
        
        return data ?? Array.Empty<ClientPackage>();
    }

    public Metadata PopMetadata(Guid fileId)
    {
        FileIdToMetadata.TryGetValue(fileId, out var metadata);
        FileIdToMetadata.TryRemove(fileId, out var _);
        return metadata;
    }
}