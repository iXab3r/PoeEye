using StronglyTypedIds;

namespace PoeShared.Modularity;

[StronglyTypedId(StronglyTypedIdBackingType.Guid, StronglyTypedIdConverter.NewtonsoftJson | StronglyTypedIdConverter.SystemTextJson)]
public partial struct ErrorInfoId {}