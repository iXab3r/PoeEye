using PoeEye.TradeMonitor.Models;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeEye.TradeMonitor.ViewModels
{
    internal sealed class MacroMessageViewModel : DisposableReactiveObject
    {
        private string label;

        private string text;

        public MacroMessageViewModel(MacroMessage message)
        {
            FromMessage(message);
        }

        public MacroMessageViewModel()
        {
        }

        public string Text
        {
            get => text;
            set => this.RaiseAndSetIfChanged(ref text, value);
        }

        public string Label
        {
            get => label;
            set => this.RaiseAndSetIfChanged(ref label, value);
        }

        public void FromMessage(MacroMessage message)
        {
            Text = message.Text;
            Label = message.Label;
        }

        public MacroMessage ToMessage()
        {
            return new MacroMessage
            {
                Text = Text,
                Label = Label
            };
        }
    }
}