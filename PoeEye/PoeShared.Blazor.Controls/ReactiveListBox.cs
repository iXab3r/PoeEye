using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using BlazorComponentUtilities;
using DynamicData.Binding;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Blazor.Controls;

partial class ReactiveListBox<TItem>
{
    private TItem selectedItem;
    private string selectedValue;

    [Parameter] public bool CanSelect { get; set; } = true;
    [Parameter] public IEnumerable<TItem> ItemsSource { get; set; }
    [Parameter] public Func<TItem, string> ValueSelector { get; set; }

    [Parameter] public RenderFragment<TItem> ItemTemplate { get; set; }

    [Parameter]
    public string SelectedValue
    {
        get => selectedValue;
        set
        {
            if (ReferenceEquals(selectedValue, value))
            {
                return;
            }

            SetSelectedValueAsync(value, force: true).AndForget();
        }
    }

    [Parameter] public EventCallback<string> SelectedValueChanged { get; set; }

    [Parameter]
    public TItem SelectedItem
    {
        get => selectedItem;
        set
        {
            if (ReferenceEquals(selectedItem, value))
            {
                return;
            }

            var selector = ValueSelector;
            if (selector == null)
            {
                return;
            }

            var itemValue = value == null ? null : selector(value);
            SetSelectedValueAsync(itemValue, force: true).AndForget();
        }
    }

    [Parameter] public EventCallback<TItem> SelectedItemChanged { get; set; }

    public int ItemsCount { get; private set; }

    protected string Classname => new CssBuilder("form-control").AddClass((string) Class).Build();

    public ReactiveListBox()
    {
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (ItemsSource == null || ValueSelector == null)
        {
            throw new InvalidOperationException("ListBox requires Items, TextSelector, and ValueSelector parameters.");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            this.WhenAnyValue(x => x.ItemsSource)
                .Select(x => x is INotifyCollectionChanged collectionChanged ? collectionChanged.ObserveCollectionChanges() : Observable.Never<EventPattern<NotifyCollectionChangedEventArgs>>())
                .Switch()
                .Subscribe(x =>
                {
                    ItemsCount = ItemsSource?.Count() ?? 0;
                    WhenRefresh.OnNext("Collection changed");
                }).AddTo(Anchors);

            this.WhenAnyValue(x => x.SelectedValue)
                .Subscribe(selectedValue => { }).AddTo(Anchors);
        }

        if (!string.IsNullOrEmpty(SelectedValue))
        {
            await JsRuntime.InvokeVoidAsync("scrollToSelectedOption", ComponentId.ToString());
        }
    }

    internal async Task SetSelectedValueAsync(string value, bool force = false)
    {
        if (!CanSelect && !force)
            return;
        if (object.Equals(selectedValue, value))
        {
            return;
        }

        selectedValue = value;
        await SelectedValueChanged.InvokeAsync(value);

        selectedItem = default; // <-- for now, we'll see which item matches the value below
        var selector = ValueSelector;
        foreach (var listItem in ItemsSource.EmptyIfNull().ToArray())
        {
            var listItemValue = selector(listItem);
            var isSelected = listItemValue != null && object.Equals(value, listItemValue);
            //listItem.SetSelected(isSelected);
            if (isSelected)
            {
                selectedItem = listItem;
            }
        }

        await SelectedItemChanged.InvokeAsync(selectedItem);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (!string.IsNullOrEmpty(SelectedValue))
        {
        }
    }
}