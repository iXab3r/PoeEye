using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using Guards;
using PoeShared.Scaffolding;

namespace PoeShared.UI.Models
{
    public class ViewController : DisposableReactiveObject, IViewController
    {
        private readonly Window window;
        
        private readonly ReplaySubject<EventPattern<RoutedEventArgs>> whenLoaded =  new ReplaySubject<EventPattern<RoutedEventArgs>>(1);

        public ViewController(Window window)
        {
            Guard.ArgumentNotNull(window, nameof(window));
            
            this.window = window;

            Observable
                .FromEventPattern<RoutedEventHandler, RoutedEventArgs>(
                    x => window.Loaded += x, 
                    x => window.Loaded -= x)
                .Subscribe(whenLoaded)
                .AddTo(Anchors);
        }

        public IObservable<Unit> Loaded => whenLoaded.ToUnit();
    }
}