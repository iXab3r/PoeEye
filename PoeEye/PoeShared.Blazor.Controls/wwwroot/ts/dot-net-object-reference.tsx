import DotNetObject = DotNet.DotNetObject;

export interface DotNetObjectReference extends DotNetObject {
    _id: number;//FIXME not really safe to use as it is implementation detail of Blazor and could be changed at any point
}