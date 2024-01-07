using System.Collections.Concurrent;
using Ethachat.Client.ClientOnlyModels;
using EthachatShared.Models.Message.DataTransfer;

namespace Ethachat.Client.Services.DataTransmission.PackageForming.BinaryDataBoxService;

public class BinaryDataBox : IBinaryDataBox
{
    private ConcurrentDictionary<Guid, Metadata> _idToMetadata = new();
    private ConcurrentDictionary<Guid, ClientPackage[]> _idToBytes = new();

    public void StoreMetadata(Metadata metadata)
    {
        _idToMetadata.TryAdd(metadata.DataFileId, metadata);
    }

    public bool StoreData(Guid fileId, ClientPackage clientPackage)
    {
        _idToMetadata.TryGetValue(fileId, out var metadata);
        
        if (!_idToBytes.ContainsKey(fileId))
        {
            _idToBytes.TryAdd(fileId, new ClientPackage[metadata!.ChunksCount]);
        }

        _idToBytes.AddOrUpdate(fileId,
            _ => [clientPackage],
            (_, existingData) =>
            {
                existingData[clientPackage.Index] = clientPackage;
                return existingData;
            });

        _idToBytes.TryGetValue(fileId, out var packages);

        return metadata?.ChunksCount == packages?.Where(x=>x is not null).Count();
    }

    public ClientPackage[]? GetData(Guid fileId)
    {
        _idToBytes.TryGetValue(fileId, out var data);
        _idToBytes.TryRemove(fileId, out var _);
        
        return data;
    }

    public Metadata GetMetadata(Guid fileId)
    {
        _idToMetadata.TryGetValue(fileId, out var metadata);
        _idToMetadata.TryRemove(fileId, out var _);
        return metadata;
    }
}