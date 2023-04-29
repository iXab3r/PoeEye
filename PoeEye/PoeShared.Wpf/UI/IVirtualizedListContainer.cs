using System;

namespace PoeShared.UI;

public interface IVirtualizedListContainer<T> where T : class
{
    T Value { get; set; }    
    
    Type ValueType { get; set; }
    
    bool HasValue { get; }
}