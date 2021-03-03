using System.Windows;

namespace PoeShared.Audio.ViewModels
{
    internal sealed class AudioNotificationSelectorProxy : Freezable
    {
        public static readonly DependencyProperty DataContextProperty = DependencyProperty.Register(
            "DataContext", typeof(IAudioNotificationSelectorViewModel), typeof(AudioNotificationSelectorProxy), new PropertyMetadata(default(IAudioNotificationSelectorViewModel)));

        public IAudioNotificationSelectorViewModel DataContext
        {
            get => (IAudioNotificationSelectorViewModel) GetValue(DataContextProperty);
            set => SetValue(DataContextProperty, value);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new AudioNotificationSelectorProxy();
        }
    }
}