using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using PoeShared.Dialogs.ViewModels;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.UI;
using ReactiveUI;

namespace PoeShared.Dialogs.Services
{
    internal sealed class MessageBoxService : DisposableReactiveObject, IMessageBoxService
    {
        private static readonly IFluentLog Log = typeof(MessageBoxService).PrepareLogger();

        private readonly IFactory<InputMessageBoxViewModel> inputMessageBoxFactory;
        private readonly IFactory<TextMessageBoxViewModel> textMessageBoxFactory;
        private readonly IFactory<MessageBoxHostWithContentViewModelBase> messageBoxFactory;

        public MessageBoxService(
            IFactory<InputMessageBoxViewModel> inputMessageBoxFactory,
            IFactory<TextMessageBoxViewModel> textMessageBoxFactory,
            IFactory<MessageBoxHostWithContentViewModelBase> messageBoxFactory)
        {
            this.inputMessageBoxFactory = inputMessageBoxFactory;
            this.textMessageBoxFactory = textMessageBoxFactory;
            this.messageBoxFactory = messageBoxFactory;
        }

        public IMessageBoxHost MessageBox { get; private set; }

        public async Task<MessageBoxElement> ShowDialog(string title, IMessageBoxViewModel content, params MessageBoxElement[] buttons)
        {
            Log.Debug(() => $"Showing message box {new {title, content, buttons}}");

            using var newMessageBox = messageBoxFactory.Create();
            newMessageBox.Content = content;
            newMessageBox.Title = title;
            newMessageBox.AvailableCommands.Clear();
            newMessageBox.AvailableCommands.Add(buttons);
            var result = await Show(newMessageBox);
            return result;
        }

        public Task<MessageBoxElement> ShowDialog(string title, object content, params MessageBoxElement[] buttons)
        {
            throw new NotImplementedException();
        }

        public async Task<T> ShowDialog<T>(string title, IMessageBoxViewModel<T> content)
        {
            Log.Debug(() => $"Showing message box with generic content {new {title, content}}");

            using var newMessageBox = messageBoxFactory.Create();
            newMessageBox.Content = content;
            newMessageBox.Title = title;

            var dialogCloseController = new CloseController<T>(_ => newMessageBox.Close());
            var capturingCloseController = new CapturingCloseController<T>();
            var closeController = new ForwardingCloseController<T>(capturingCloseController, dialogCloseController);
            content.CloseController = closeController;
            
            await ShowDialog(newMessageBox);
            return capturingCloseController.Result;
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

        public async Task<string> ShowInputBox(string title, string content, string contentHint)
        {
            using var inputBox = inputMessageBoxFactory.Create();
            inputBox.Content = content;
            inputBox.ContentHint = contentHint;
            var result = await ShowDialog(title, inputBox);
            return result;
        }

        public async Task<(MessageBoxElement DialogResult, string InputContent)> ShowMessageBox(
            string title,
            string content,
            string contentHint,
            bool isReadOnly,
            params MessageBoxElement[] buttons
        )
        {
            Log.Debug(() => $"Showing message box {new {title, content, contentHint, isReadOnly, buttons}}");

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

        private async Task<MessageBoxElement> Show(IMessageBoxHostViewModel newMessageBox)
        {
            await ShowDialog(newMessageBox);
            return newMessageBox.Result;
        }
        
        private async Task ShowDialog(IMessageBoxHostViewModel newMessageBox)
        {
            if (MessageBox != null)
            {
                Log.Warn($"Currently already showing message box {newMessageBox}");
                throw new NotSupportedException($"Multiple message boxes are not supported yet, please report this bug");
            }
            
            try
            {
                MessageBox = newMessageBox;
                newMessageBox.IsOpen = true;
                await newMessageBox.WhenAnyValue(x => x.IsOpen).Where(x => !x).Take(1);
            }
            finally
            {
                MessageBox = default;
            }
        }
    }
}