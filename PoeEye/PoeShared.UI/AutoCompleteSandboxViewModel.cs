using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using AutoCompleteTextBox.Editors;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;

namespace PoeShared.UI
{
    internal sealed class AutoCompleteSandboxViewModel : DisposableReactiveObject
    {
        public AutoCompleteSandboxViewModel()
        {
            var items = new ObservableCollection<TestComboItem>(new[]
            {
                new TestComboItem()
                {
                    Name = "a",
                    IsActive = true
                },
                new TestComboItem()
                {
                    Name = "b",
                    IsActive = false
                },
                new TestComboItem()
                {
                    Name = "a",
                    IsActive = true
                }
            });
            TextSuggestions = new TextSuggestionProvider<TestComboItem>(items);
            
            AddItem = CommandWrapper.Create<string>(x => items.Add(new TestComboItem()
            {
                Name = x,
                IsActive = false
            })); 
            RemoveItem = CommandWrapper.Create<string>(x =>
            {
                var itemsToRemove = items.Where(item => item.Name == x).ToArray();
                itemsToRemove.ForEach(x => items.Remove(x));
            });
        }

        public IComboSuggestionProvider TextSuggestions { get; }

        public string SelectedText { get; set; }
        
        public TestComboItem SelectedItem { get; set; }

        public ICommand AddItem { get; }
        public ICommand RemoveItem { get; }

        private sealed class TextSuggestionProvider<T> : IComboSuggestionProvider
        {
            private readonly IEnumerable<T> items;

            public TextSuggestionProvider(IEnumerable<T> items)
            {
                this.items = items;
            }

            public IEnumerable GetSuggestions(string filter)
            {
                return items;
            }

            public IEnumerable GetFullCollection()
            {
                return items;
            }
        }
    }
    
    
    public sealed class TestComboItem : DisposableReactiveObject
    {
        public bool IsActive { get; set; }

        public string Name { get; set; }
    }

}