using PoeEye.TradeMonitor.Modularity;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeEye.TradeMonitor.ViewModels
{
    internal sealed class MacroMessageViewModel : DisposableReactiveObject
    {
        public MacroMessageViewModel(MacroMessage message)
        {
            FromMessage(message);
        }

        public MacroMessageViewModel() {}

        private string text;

        public string Text
        {
            get { return text; }
            set { this.RaiseAndSetIfChanged(ref text, value); }
        }

        private string label;

        public string Label
        {
            get { return label; }
            set { this.RaiseAndSetIfChanged(ref label, value); }
        }

        public void FromMessage(MacroMessage message)
        {
            Text = message.Text;
            Label = message.Label;
        }

        public MacroMessage ToMessage()
        {
            return new MacroMessage()
            {
                Text = Text,
                Label = Label,
            };
        }
    }
}