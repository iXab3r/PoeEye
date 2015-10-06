namespace WpfControls.Editors
{
    using System;
    using System.Collections;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Threading;

    using Binding = System.Windows.Data.Binding;
    using Control = System.Windows.Controls.Control;
    using KeyEventArgs = System.Windows.Input.KeyEventArgs;
    using ListBox = System.Windows.Controls.ListBox;
    using TextBox = System.Windows.Controls.TextBox;

    [TemplatePart(Name = PartEditor, Type = typeof (TextBox))]
    [TemplatePart(Name = PartPopup, Type = typeof (Popup))]
    [TemplatePart(Name = PartSelector, Type = typeof (Selector))]
    public class AutoCompleteTextBox : Control
    {
        public const string PartEditor = "PART_Editor";
        public const string PartPopup = "PART_Popup";

        public const string PartSelector = "PART_Selector";

        public static readonly DependencyProperty DelayProperty = DependencyProperty.Register("Delay", typeof (int), typeof (AutoCompleteTextBox),
            new FrameworkPropertyMetadata(200));

        public static readonly DependencyProperty DisplayMemberProperty = DependencyProperty.Register("DisplayMember", typeof (string), typeof (AutoCompleteTextBox),
            new FrameworkPropertyMetadata(string.Empty));

        public static readonly DependencyProperty IconPlacementProperty = DependencyProperty.Register("IconPlacement", typeof (IconPlacement), typeof (AutoCompleteTextBox),
            new FrameworkPropertyMetadata(IconPlacement.Left));

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof (object),
            typeof (AutoCompleteTextBox), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty IconVisibilityProperty = DependencyProperty.Register(
            "IconVisibility", typeof (Visibility), typeof (AutoCompleteTextBox),
            new FrameworkPropertyMetadata(Visibility.Visible));

        public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register(
            "IsDropDownOpen", typeof (bool), typeof (AutoCompleteTextBox), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register("IsLoading",
            typeof (bool), typeof (AutoCompleteTextBox), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly",
            typeof (bool), typeof (AutoCompleteTextBox), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register("ItemTemplate",
            typeof (DataTemplate), typeof (AutoCompleteTextBox), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ItemTemplateSelectorProperty =
            DependencyProperty.Register("ItemTemplateSelector", typeof (DataTemplateSelector),
                typeof (AutoCompleteTextBox));

        public static readonly DependencyProperty LoadingContentProperty = DependencyProperty.Register(
            "LoadingContent", typeof (object), typeof (AutoCompleteTextBox), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ProviderProperty = DependencyProperty.Register("Provider",
            typeof (ISuggestionProvider), typeof (AutoCompleteTextBox), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register("SelectedItem",
            typeof (object), typeof (AutoCompleteTextBox), new FrameworkPropertyMetadata(null, OnSelectedItemChanged));

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof (string),
            typeof (AutoCompleteTextBox), new FrameworkPropertyMetadata(string.Empty));

        public static readonly DependencyProperty WatermarkProperty = DependencyProperty.Register("Watermark",
            typeof (string), typeof (AutoCompleteTextBox), new FrameworkPropertyMetadata(string.Empty));

        private bool isUpdatingText;

        private bool selectionCancelled;

        private SuggestionsAdapter suggestionsAdapter;

        private DispatcherTimer fetchTimer;

        static AutoCompleteTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (AutoCompleteTextBox),
                new FrameworkPropertyMetadata(typeof (AutoCompleteTextBox)));
        }

        public BindingEvaluator BindingEvaluator { get; set; }

        public int Delay
        {
            get { return (int) GetValue(DelayProperty); }
            set { SetValue(DelayProperty, value); }
        }

        public string DisplayMember
        {
            get { return (string) GetValue(DisplayMemberProperty); }
            set { SetValue(DisplayMemberProperty, value); }
        }

        public TextBox Editor { get; set; }

        public string Filter { get; set; }

        public object Icon
        {
            get { return GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public IconPlacement IconPlacement
        {
            get { return (IconPlacement) GetValue(IconPlacementProperty); }
            set { SetValue(IconPlacementProperty, value); }
        }

        public Visibility IconVisibility
        {
            get { return (Visibility) GetValue(IconVisibilityProperty); }
            set { SetValue(IconVisibilityProperty, value); }
        }

        public bool IsDropDownOpen
        {
            get { return (bool) GetValue(IsDropDownOpenProperty); }
            set { SetValue(IsDropDownOpenProperty, value); }
        }

        public bool IsLoading
        {
            get { return (bool) GetValue(IsLoadingProperty); }
            set { SetValue(IsLoadingProperty, value); }
        }

        public bool IsReadOnly
        {
            get { return (bool) GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public Selector ItemsSelector { get; set; }

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate) GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public DataTemplateSelector ItemTemplateSelector
        {
            get { return ((DataTemplateSelector) (GetValue(ItemTemplateSelectorProperty))); }
            set { SetValue(ItemTemplateSelectorProperty, value); }
        }

        public object LoadingContent
        {
            get { return GetValue(LoadingContentProperty); }
            set { SetValue(LoadingContentProperty, value); }
        }

        public Popup Popup { get; set; }

        public ISuggestionProvider Provider
        {
            get { return (ISuggestionProvider) GetValue(ProviderProperty); }
            set { SetValue(ProviderProperty, value); }
        }

        public object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public SelectionAdapter SelectionAdapter { get; set; }

        public string Text
        {
            get { return (string) GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public string Watermark
        {
            get { return (string) GetValue(WatermarkProperty); }

            set { SetValue(WatermarkProperty, value); }
        }

        public static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AutoCompleteTextBox act = null;
            act = d as AutoCompleteTextBox;
            if (act == null)
            {
                return;
            }

            if (act.Editor != null & !act.isUpdatingText)
            {
                act.isUpdatingText = true;
                act.Editor.Text = act.BindingEvaluator.Evaluate(e.NewValue);
                act.isUpdatingText = false;
            }
        }

        private void ScrollToSelectedItem()
        {
            var listBox = ItemsSelector as ListBox;
            if (listBox?.SelectedItem != null)
            {
                listBox.ScrollIntoView(listBox.SelectedItem);
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            Editor = Template.FindName(PartEditor, this) as TextBox;
            Popup = Template.FindName(PartPopup, this) as Popup;
            ItemsSelector = Template.FindName(PartSelector, this) as Selector;
            BindingEvaluator = new BindingEvaluator(new Binding(DisplayMember));

            if (Editor != null)
            {
                Editor.TextChanged += OnEditorTextChanged;
                Editor.PreviewKeyDown += OnEditorKeyDown;
                Editor.LostFocus += OnEditorLostFocus;

                if (SelectedItem != null)
                {
                    isUpdatingText = true;
                    Editor.Text = BindingEvaluator.Evaluate(SelectedItem);
                    isUpdatingText = false;
                }
            }

            if (Popup != null)
            {
                Popup.StaysOpen = false;
                Popup.Opened += OnPopupOpened;
                Popup.Closed += OnPopupClosed;
            }
            if (ItemsSelector != null)
            {
                SelectionAdapter = new SelectionAdapter(ItemsSelector);
                SelectionAdapter.Commit += OnSelectionAdapterCommit;
                SelectionAdapter.Cancel += OnSelectionAdapterCancel;
                SelectionAdapter.SelectionChanged += OnSelectionAdapterSelectionChanged;
            }
        }

        private string GetDisplayText(object dataItem)
        {
            if (BindingEvaluator == null)
            {
                BindingEvaluator = new BindingEvaluator(new Binding(DisplayMember));
            }
            if (dataItem == null)
            {
                return string.Empty;
            }
            if (string.IsNullOrEmpty(DisplayMember))
            {
                return dataItem.ToString();
            }
            return BindingEvaluator.Evaluate(dataItem);
        }

        private void OnEditorKeyDown(object sender, KeyEventArgs e)
        {
            if (SelectionAdapter == null)
            {
                return;
            }
            if (IsDropDownOpen)
            {
                SelectionAdapter.HandleKeyDown(e);
            }
            else
            {
                var isUpOrDownKey = e.Key == Key.Down || e.Key == Key.Up;
                IsDropDownOpen = isUpOrDownKey;
            }
        }

        private void OnEditorLostFocus(object sender, RoutedEventArgs e)
        {
            if (!IsKeyboardFocusWithin)
            {
                IsDropDownOpen = false;
            }
        }

        private void OnEditorTextChanged(object sender, TextChangedEventArgs e)
        {
            Text = Editor.Text;

            if (isUpdatingText)
            {
                return;
            }
            if (fetchTimer == null)
            {
                fetchTimer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(Delay)};
                fetchTimer.Tick += OnFetchTimerTick;
            }
            fetchTimer.IsEnabled = false;
            fetchTimer.Stop();
            SetSelectedItem(null);
            if (Editor.Text.Length > 0 && Editor.IsFocused)
            {
                IsLoading = true;
                IsDropDownOpen = true;
                ItemsSelector.ItemsSource = null;
                fetchTimer.IsEnabled = true;
                fetchTimer.Start();
            }
            else
            {
                IsDropDownOpen = false;
            }
        }

        private void OnFetchTimerTick(object sender, EventArgs e)
        {
            fetchTimer.IsEnabled = false;
            fetchTimer.Stop();
            if (Provider != null && ItemsSelector != null)
            {
                Filter = Editor.Text;
                if (suggestionsAdapter == null)
                {
                    suggestionsAdapter = new SuggestionsAdapter(this);
                }
                suggestionsAdapter.GetSuggestions(Filter);
            }
        }

        private void OnPopupClosed(object sender, EventArgs e)
        {
            if (!selectionCancelled)
            {
                OnSelectionAdapterCommit();
            }
        }

        private void OnPopupOpened(object sender, EventArgs e)
        {
            selectionCancelled = false;
            ItemsSelector.SelectedItem = SelectedItem;
        }

        private void OnSelectionAdapterCancel()
        {
            isUpdatingText = true;
            Editor.Text = SelectedItem == null ? Filter : GetDisplayText(SelectedItem);
            Editor.SelectionStart = Editor.Text.Length;
            Editor.SelectionLength = 0;
            isUpdatingText = false;
            IsDropDownOpen = false;
            selectionCancelled = true;
        }

        private void OnSelectionAdapterCommit()
        {
            if (ItemsSelector.SelectedItem != null)
            {
                SelectedItem = ItemsSelector.SelectedItem;
                isUpdatingText = true;
                Editor.Text = GetDisplayText(ItemsSelector.SelectedItem);
                SetSelectedItem(ItemsSelector.SelectedItem);
                isUpdatingText = false;
                IsDropDownOpen = false;
            }
        }

        private void OnSelectionAdapterSelectionChanged()
        {
            isUpdatingText = true;
            if (ItemsSelector.SelectedItem == null)
            {
                Editor.Text = Filter;
            }
            else
            {
                Editor.Text = GetDisplayText(ItemsSelector.SelectedItem);
            }
            Editor.SelectionStart = Editor.Text.Length;
            Editor.SelectionLength = 0;
            ScrollToSelectedItem();
            isUpdatingText = false;
        }

        private void SetSelectedItem(object item)
        {
            isUpdatingText = true;
            SelectedItem = item;
            isUpdatingText = false;
        }

        private class SuggestionsAdapter
        {
            private readonly AutoCompleteTextBox _actb;

            private string _filter;

            public SuggestionsAdapter(AutoCompleteTextBox actb)
            {
                _actb = actb;
            }

            public void GetSuggestions(string searchText)
            {
                _filter = searchText;
                _actb.IsLoading = true;
                ParameterizedThreadStart thInfo = GetSuggestionsAsync;
                var th = new Thread(thInfo);
                th.Start(new object[]
                {
                    searchText,
                    _actb.Provider
                });
            }

            private void DisplaySuggestions(IEnumerable suggestions, string filter)
            {
                if (_filter != filter)
                {
                    return;
                }
                if (_actb.IsDropDownOpen)
                {
                    _actb.IsLoading = false;
                    _actb.ItemsSelector.ItemsSource = suggestions;
                    _actb.IsDropDownOpen = _actb.ItemsSelector.HasItems;
                }
            }

            private void GetSuggestionsAsync(object param)
            {
                var args = param as object[];
                var searchText = Convert.ToString(args[0]);
                var provider = args[1] as ISuggestionProvider;
                var list = provider.GetSuggestions(searchText);
                _actb.Dispatcher.BeginInvoke(new Action<IEnumerable, string>(DisplaySuggestions), DispatcherPriority.Background, list, searchText);
            }
        }
    }
}