using StronglyTypedIds;

namespace PoeShared.Blazor;

[StronglyTypedId(StronglyTypedIdBackingType.NullableString, StronglyTypedIdConverter.NewtonsoftJson | StronglyTypedIdConverter.SystemTextJson)]
public partial struct ReactiveComponentId {}