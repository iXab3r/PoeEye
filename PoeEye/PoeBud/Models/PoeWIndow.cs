using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using WindowsInput.Native;
using Guards;
using JetBrains.Annotations;
using PoeEye.StashGrid.Modularity;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using PoeShared.StashApi.DataTypes;
using ReactiveUI;

namespace PoeBud.Models
{
    internal sealed class PoeWindow : DisposableReactiveObject, IPoeWindow
    {
        private const int StashItemsMaxX = 12;
        private const int StashItemsMaxY = 12;
        private const double PoeFontSize = 20;
        private readonly IConfigProvider<PoeStashGridConfig> stashConfigProvider;

        private readonly IUserInteractionsManager userInteractionsManager;

        public PoeWindow(
            [NotNull] IConfigProvider<PoeStashGridConfig> stashConfigProvider,
            [NotNull] IUserInteractionsManager userInteractionsManager,
            IntPtr nativeWindowHandle)
        {
            Guard.ArgumentNotNull(stashConfigProvider, nameof(stashConfigProvider));
            Guard.ArgumentNotNull(userInteractionsManager, nameof(userInteractionsManager));

            if (nativeWindowHandle == IntPtr.Zero)
            {
                throw new ArgumentException("Invalid Poe window handle");
            }

            this.stashConfigProvider = stashConfigProvider;
            NativeWindowHandle = nativeWindowHandle;
            this.userInteractionsManager = userInteractionsManager;

            stashConfigProvider.WhenChanged.Subscribe(() => this.RaisePropertyChanged(nameof(StashBounds))).AddTo(Anchors);
        }

        public void MoveMouseToStashItem(int itemX, int itemY, StashTabType tabType)
        {
            if (tabType == StashTabType.QuadStash)
            {
                Guard.ArgumentIsBetween(() => itemX, 0, StashItemsMaxX * 2 - 1, true);
                Guard.ArgumentIsBetween(() => itemY, 0, StashItemsMaxY * 2 - 1, true);
            }
            else if (tabType == StashTabType.NormalStash || tabType == StashTabType.PremiumStash)
            {
                Guard.ArgumentIsBetween(() => itemX, 0, StashItemsMaxX - 1, true);
                Guard.ArgumentIsBetween(() => itemY, 0, StashItemsMaxY - 1, true);
            }
            else
            {
                throw new NotSupportedException($"Stash tabs of type {tabType} are not supported yet");
            }

            var stashBounds = StashBounds;
            if (stashBounds.IsEmpty)
            {
                throw new ApplicationException("Stash bounds are not configured");
            }

            var itemSize = new Size(stashBounds.Width / StashItemsMaxX, stashBounds.Height / StashItemsMaxY);
            if (tabType == StashTabType.QuadStash)
            {
                itemSize = new Size(itemSize.Width / 2, itemSize.Height / 2);
            }

            var moveLocation = new Point(
                stashBounds.Left + itemSize.Width * itemX + itemSize.Width / 2,
                stashBounds.Top + itemSize.Height * itemY + itemSize.Height / 2);

            userInteractionsManager.MoveMouseTo(moveLocation);
        }

        public Rect WindowBounds => NativeMethods.GetWindowBounds(NativeWindowHandle);

        public Rect StashBounds => stashConfigProvider.ActualConfig.StashBounds;

        public void TransferItemFromStash(int itemX, int itemY, StashTabType tabType)
        {
            MoveMouseToStashItem(itemX, itemY, tabType);
            userInteractionsManager.SendControlLeftClick();
        }

        public IntPtr NativeWindowHandle { get; }

        public void SelectStashTabByIdx(IStashTab tabToSelect, IStashTab[] tabs)
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

        private Rect GetDefaultFullScreenStashSize()
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

        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool GetWindowRect(IntPtr hWnd, out WinRectangle lpWinRectangle);

            public static Rect GetWindowBounds(IntPtr hwnd)
            {
                WinRectangle rect;
                GetWindowRect(hwnd, out rect);
                return new Rect(
                    new Point(rect.Left, rect.Top),
                    new Size(Math.Abs(rect.Right - rect.Left), Math.Abs(rect.Bottom - rect.Top)));
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct WinRectangle
            {
                public readonly int Left; // itemX position of upper-left corner
                public readonly int Top; // y position of upper-left corner
                public readonly int Right; // itemX position of lower-right corner
                public readonly int Bottom; // y position of lower-right corner
            }
        }
    }
}