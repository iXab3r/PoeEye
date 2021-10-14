using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using log4net;
using PoeShared.Dialogs.ViewModels;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using ReactiveUI;

namespace PoeShared.Dialogs.Services
{
    internal sealed class MessageBoxService : DisposableReactiveObject, IMessageBoxService
    {
        private static readonly IFluentLog Log = typeof(MessageBoxService).PrepareLogger();

        private readonly IFactory<TextMessageBoxViewModel> textMessageBoxFactory;
        private readonly IFactory<MessageBoxWithContentViewModelBase> messageBoxFactory;

        public MessageBoxService(
            IFactory<TextMessageBoxViewModel> textMessageBoxFactory,
            IFactory<MessageBoxWithContentViewModelBase> messageBoxFactory)
        {
            this.textMessageBoxFactory = textMessageBoxFactory;
            this.messageBoxFactory = messageBoxFactory;
        }

        public IMessageBoxViewModel MessageBox { get; private set; }

        public async Task<MessageBoxElement> ShowDialog(string title, object content, params MessageBoxElement[] buttons)
        {
            Log.Debug($"Showing message box {new {title, content, buttons}}");

            using var newMessageBox = messageBoxFactory.Create();
            newMessageBox.Content = content;
            newMessageBox.Title = title;
            newMessageBox.AvailableCommands.Clear();
            newMessageBox.AvailableCommands.Add(buttons);
            var result = await Show(newMessageBox);
            return result;
        }

        public async Task<bool> ShowConfirmation(string title, string content)
        {
            var result = await ShowMessageBox(
                title,
                content,
                string.Empty,
                true,
                MessageBoxElement.Yes,
                MessageBoxElement.No
            );
            return result.DialogResult == MessageBoxElement.Yes;
        }

        public async Task<(MessageBoxElement DialogResult, string InputContent)> ShowMessageBox(
            string title,
            string content,
            string contentHint,
            bool isReadOnly,
            params MessageBoxElement[] buttons
        )
        {
            Log.Debug($"Showing message box {new {title, content, contentHint, isReadOnly, buttons}}");

            using var newMessageBox = textMessageBoxFactory.Create();
            newMessageBox.Content = content;
            newMessageBox.ContentHint = contentHint;
            newMessageBox.Title = title;
            newMessageBox.IsReadOnly = isReadOnly;
            newMessageBox.AvailableCommands.Clear();
            newMessageBox.AvailableCommands.Add(buttons);
            var result = await Show(newMessageBox);
            return (result, newMessageBox.Content);
        }

        private async Task<MessageBoxElement> Show(IMessageBoxViewModel newMessageBox)
        {
            if (this.MessageBox != null)
            {
                Log.Warn($"Currently showing message box {new {this.MessageBox.Title}}");
                throw new NotSupportedException($"Multiple message boxes are not supported yet, please report this bug");
            }
            
            try
            {
                MessageBox = newMessageBox;
                newMessageBox.IsOpen = true;
                await newMessageBox.WhenAnyValue(x => x.IsOpen).Where(x => !x).Take(1);
                return newMessageBox.Result;
            }
            finally
            {
                MessageBox = default;
                newMessageBox.Dispose();
            }
        }
    }
}