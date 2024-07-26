using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace PoeShared.Blazor.Scaffolding;

public static class ElementReferenceExtensions
{
    /// <summary>
    /// Gives focus to an element given its <see cref="ElementReference"/>.
    /// </summary>
    /// <param name="elementRef">A reference to the element to focus.</param>
    /// <returns>The <see cref="ValueTask"/> representing the asynchronous focus operation.</returns>
    private static async Task FocusSafe(this ElementReference? elementRef)
    {
        if (elementRef == null)
        {
            return;
        }

        await elementRef.Value.FocusAsync();
    }
}