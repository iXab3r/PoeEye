using System.Collections;
using System.Collections.Generic;
using AutoCompleteTextBox.Editors;

namespace PoeShared.Scaffolding.WPF;

public class AutoCompleteSuggestionProvider<T> : IComboSuggestionProvider, ISuggestionProvider
{
    private readonly IEnumerable<T> items;

    public AutoCompleteSuggestionProvider(IEnumerable<T> items)
    {
        this.items = items;
    }

    public IEnumerable GetFullCollection()
    {
        return items;
    }
    
    IEnumerable IComboSuggestionProvider.GetSuggestions(string filter)
    {
        return GetFullCollection();
    }

    IEnumerable ISuggestionProvider.GetSuggestions(string filter)
    {
        return GetFullCollection();
    }
}