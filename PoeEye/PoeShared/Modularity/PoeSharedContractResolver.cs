using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PoeShared.Modularity;

internal sealed class PoeSharedContractResolver : DefaultContractResolver
{
    protected override JsonContract CreateContract(Type objectType)
    {
        var contract = base.CreateContract(objectType);

        if (contract is JsonContainerContract objectContract)
        {
            objectContract.ItemTypeNameHandling = TypeNameHandling.None;
        }

        return contract;
    }

}