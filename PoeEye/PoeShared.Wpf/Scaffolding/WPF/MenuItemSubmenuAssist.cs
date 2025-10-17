using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PoeShared.Scaffolding.WPF
{
    /// <summary>
    /// Assist class to control how quickly WPF MenuItem submenus open when hovering.
    /// Set ui1:MenuItemSubmenuAssist.OpenOnHoverDelay="125" on a MenuItem style to open submenus faster.
    /// </summary>
    public static class MenuItemSubmenuAssist
    {
        public static readonly DependencyProperty OpenOnHoverDelayProperty = DependencyProperty.RegisterAttached(
            name: "OpenOnHoverDelay",
            propertyType: typeof(int),
            ownerType: typeof(MenuItemSubmenuAssist),
            defaultMetadata: new PropertyMetadata(-1, OnOpenOnHoverDelayChanged));

        public static void SetOpenOnHoverDelay(DependencyObject element, int value) => element.SetValue(OpenOnHoverDelayProperty, value);
        public static int GetOpenOnHoverDelay(DependencyObject element) => (int)element.GetValue(OpenOnHoverDelayProperty);

        private static readonly ConditionalWeakTable<MenuItem, HoverState> States = new();

        private static void OnOpenOnHoverDelayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MenuItem menuItem)
            {
                return;
            }

            // cleanup previous
            if (States.TryGetValue(menuItem, out var state))
            {
                state.Dispose();
                States.Remove(menuItem);
            }

            var delayMs = e.NewValue is int i ? i : -1;
            if (delayMs < 0)
            {
                // disabled
                return;
            }

            state = new HoverState(menuItem, TimeSpan.FromMilliseconds(delayMs));
            States.Add(menuItem, state);
        }

        private sealed class HoverState : IDisposable
        {
            private readonly MenuItem menuItem;
            private readonly TimeSpan delay;
            private readonly DispatcherTimer timer;

            public HoverState(MenuItem menuItem, TimeSpan delay)
            {
                this.menuItem = menuItem;
                this.delay = delay;

                timer = new DispatcherTimer { Interval = delay, IsEnabled = false };
                timer.Tick += TimerOnTick;

                menuItem.Unloaded += MenuItemOnUnloaded;
                menuItem.MouseEnter += MenuItemOnMouseEnter;
                menuItem.MouseLeave += MenuItemOnMouseLeave;
                menuItem.SubmenuClosed += MenuItemOnSubmenuClosed;
            }

            private void MenuItemOnMouseEnter(object sender, MouseEventArgs e)
            {
                // open only for submenu headers
                if (menuItem.Role == MenuItemRole.SubmenuHeader && menuItem.IsEnabled)
                {
                    // If already open, no need to wait
                    if (!menuItem.IsSubmenuOpen)
                    {
                        timer.Stop();
                        timer.Interval = delay; // in case changed
                        timer.Start();
                    }
                }
            }

            private void MenuItemOnMouseLeave(object sender, MouseEventArgs e)
            {
                timer.Stop();
            }

            private void MenuItemOnSubmenuClosed(object? sender, RoutedEventArgs e)
            {
                timer.Stop();
            }

            private void TimerOnTick(object? sender, EventArgs e)
            {
                timer.Stop();
                if (menuItem.Role == MenuItemRole.SubmenuHeader && menuItem.IsEnabled)
                {
                    menuItem.IsSubmenuOpen = true;
                }
            }

            private void MenuItemOnUnloaded(object sender, RoutedEventArgs e)
            {
                Dispose();
            }

            public void Dispose()
            {
                timer.Stop();
                timer.Tick -= TimerOnTick;

                menuItem.Unloaded -= MenuItemOnUnloaded;
                menuItem.MouseEnter -= MenuItemOnMouseEnter;
                menuItem.MouseLeave -= MenuItemOnMouseLeave;
                menuItem.SubmenuClosed -= MenuItemOnSubmenuClosed;
            }
        }
    }
}
