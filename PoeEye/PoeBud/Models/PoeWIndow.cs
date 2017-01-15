using WindowsInput.Native;
using PoeBud.OfficialApi.DataTypes;

namespace PoeBud.Models
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Media;

    using Guards;

    using JetBrains.Annotations;

    using ReactiveUI;

    internal sealed class PoeWindow : ReactiveObject, IPoeWindow
    {
        private const int StashItemsMaxX = 12;
        private const int StashItemsMaxY = 12;
        private const double PoeFontSize = 20;

        private readonly IUserInteractionsManager userInteractionsManager;
        private readonly IntPtr nativeWindowHandle;

        public PoeWindow(
            [NotNull] IUserInteractionsManager userInteractionsManager,
            IntPtr nativeWindowHandle)
        {
            Guard.ArgumentNotNull(() => userInteractionsManager);

            if (nativeWindowHandle == IntPtr.Zero)
            {
                throw new ArgumentException("Invalid Poe window handle");
            }

            this.nativeWindowHandle = nativeWindowHandle;
            this.userInteractionsManager = userInteractionsManager;
        }

        public void MoveMouseToStashItem(int itemX, int itemY, StashTabType tabType)
        {
            if (tabType == StashTabType.QuadStash)
            {
                Guard.ArgumentIsBetween(() => itemX, 0, StashItemsMaxX * 2 - 1, true);
                Guard.ArgumentIsBetween(() => itemY, 0, StashItemsMaxY * 2 - 1, true);
            }
            else
            {
                Guard.ArgumentIsBetween(() => itemX, 0, StashItemsMaxX - 1, true);
                Guard.ArgumentIsBetween(() => itemY, 0, StashItemsMaxY - 1, true);
            }

            var stash = StashBounds;

            var itemSize = new Size(stash.Width / StashItemsMaxX, stash.Height / StashItemsMaxY);
            if (tabType == StashTabType.QuadStash)
            {
                itemSize = new Size(itemSize.Width / 2, itemSize.Height / 2);
            }

            var moveLocation = new Point(
                    stash.Left + itemSize.Width * itemX + itemSize.Width / 2,
                    stash.Top + itemSize.Height * itemY + itemSize.Height / 2);

            userInteractionsManager.MoveMouseTo(moveLocation);
        }

        public void SelectStashTabByName(int tabIndex, string[] knownTabs)
        {
            Guard.ArgumentNotNull(() => knownTabs);
            Guard.ArgumentIsBetween(() => tabIndex, 0, knownTabs.Length - 1, true);

            var windowBounds = WindowBounds;

            var tabsSpace = new Size(windowBounds.Width * 0.00677083333333333333333333333333, windowBounds.Height * 0.02592592592592592592592592592593);
            var defaultTabWidth = windowBounds.Width * 0.025;

            var tabWidths = knownTabs
                .Select(x => EstimateTabWidth(x, defaultTabWidth, tabsSpace.Width))
                .ToArray();

            var stashesTopLeftLocation = new Point(
                windowBounds.X + windowBounds.Width * 0.0078125,
                windowBounds.Y + windowBounds.Height * 0.12037037037037037037037037037037);

            var targetPoint = new Point(
                stashesTopLeftLocation.X + tabWidths.Take(tabIndex).Sum(x => x) + tabsSpace.Width * (tabIndex + 1) + tabWidths[tabIndex] * 0.5,
                stashesTopLeftLocation.Y + tabsSpace.Height * 0.5);

            userInteractionsManager.MoveMouseTo(targetPoint);
            userInteractionsManager.SendClick();
        }

        public Rect WindowBounds => NativeMethods.GetWindowBounds(nativeWindowHandle);

        public Rect StashBounds
        {
            get
            {
                var windowBounds = WindowBounds;

                var result = new Rect(
                        new Point(
                                windowBounds.X + windowBounds.Width * 0.0078125,
                                windowBounds.Y + windowBounds.Height * 0.14814814814814814814814814814815),
                        new Size(
                                windowBounds.Width * 0.33072916666666666666666666666667,
                                windowBounds.Height * 0.58796296296296296296296296296296)
                    );
                return result;
            }
        }

        public void TransferItemFromStash(int itemX, int itemY, StashTabType tabType)
        {
            MoveMouseToStashItem(itemX, itemY, tabType);
            userInteractionsManager.SendControlLeftClick();
        }

        public IntPtr NativeWindowHandle => nativeWindowHandle;

        private static double EstimateTabWidth(string tabName, double defaultTabWidth, double spaceWidth)
        {
            var textWidth = MeasureString(tabName).Width;

            var requiredSizeForText = textWidth + spaceWidth * 2;

            return Math.Max(defaultTabWidth, requiredSizeForText);
        }

        private static Size MeasureString(string candidate, double fontSize = PoeFontSize)
        {
            var poeFont = new FontFamily("pack://application:,,,/Resources/Fontin-Regular.otf");
            var formattedText = new FormattedText(
                candidate,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(
                        poeFont,
                        new FontStyle(),
                        new FontWeight(),
                        new FontStretch()),
                fontSize,
                Brushes.Black);

            return new Size(formattedText.Width, formattedText.Height);
        }

        public void SelectStashTabByIdx(ITab tabToSelect, ITab[] tabs)
        {
            for (var i = 0; i < tabs.Length; i++)
            {
                userInteractionsManager.SendKey(VirtualKeyCode.LEFT);
                userInteractionsManager.Delay(TimeSpan.FromMilliseconds(50));
            }
            for (var i = 0; i < tabs.Length - 1; i++)
            {
                var currentTab = tabs[i];
                if (currentTab.Idx == tabToSelect.Idx)
                {
                    break;
                }
                userInteractionsManager.SendKey(VirtualKeyCode.RIGHT);
                userInteractionsManager.Delay(TimeSpan.FromMilliseconds(50));
            }
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool GetWindowRect(IntPtr hWnd, out WinRectangle lpWinRectangle);

            [StructLayout(LayoutKind.Sequential)]
            private struct WinRectangle
            {
                public int Left;        // itemX position of upper-left corner
                public int Top;         // y position of upper-left corner
                public int Right;       // itemX position of lower-right corner
                public int Bottom;      // y position of lower-right corner
            }

            public static Rect GetWindowBounds(IntPtr hwnd)
            {
                WinRectangle rect;
                GetWindowRect(hwnd, out rect);
                return new Rect(
                        new Point(rect.Left, rect.Top),
                        new Size(Math.Abs(rect.Right - rect.Left), Math.Abs(rect.Bottom - rect.Top)));
            }
        }
    }
}