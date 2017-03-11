using System;
using Guards;
using JetBrains.Annotations;
using PoeShared;
using PoeShared.Scaffolding;
using PoeWhisperMonitor.Chat;
using ReactiveUI;

namespace PoeEye.TradeMonitor.Models
{
    internal sealed class PoeMacroCommandsService : DisposableReactiveObject, IPoeMacroCommandsProvider
    {
        private readonly IPoeChatService chatService;

        public PoeMacroCommandsService(
            [NotNull] IPoeChatService chatService)
        {
            Guard.ArgumentNotNull(() => chatService);

            this.chatService = chatService;

            MacroCommands.Add(
                new ChatCommand(
                    "kick",
                    chatService,
                    context => $"/kick {context.Negotiation.CharacterName}")
                {
                    Label = "Kick character",
                    Description = "Kicks character from the party"
                }
            );

            MacroCommands.Add(
                new GenericDelegateCommand(
                    "close", 
                    context => context.CloseController?.Close())
                {
                    Label = "Close negotiation",
                    Description = "Closes ongoing negotiation window"
                });

            MacroCommands.Add(
                new ChatCommand(
                    "hideout",
                    chatService,
                    context => $"/hideout {context.Negotiation.CharacterName}")
                {
                    Label = "hideout",
                    Description = "Teleport to player's hideout"
                });
        }

        public IReactiveList<MacroCommand> MacroCommands { get; } = new ReactiveList<MacroCommand>();

        private sealed class ChatCommand : MacroCommand
        {
            private readonly IPoeChatService chatService;
            private readonly Func<IMacroCommandContext, string> messageProvider;

            public ChatCommand(
                [NotNull] string commandText,
                [NotNull] IPoeChatService chatService,
                [NotNull] Func<IMacroCommandContext, string> messageProvider) : base(commandText)
            {
                Guard.ArgumentNotNull(() => chatService);

                this.messageProvider = messageProvider;
                this.chatService = chatService;
            }

            public override void Execute(IMacroCommandContext context)
            {
                Guard.ArgumentNotNull(() => context);

                var message = messageProvider(context);
                try
                {
                    Log.Instance.Debug(
                        $"[TradeMonitor.ChatCommand] Executing commandName '{CommandText}', message '{message}'...");
                    var result = chatService.SendMessage(message);
                    Log.Instance.Debug(
                        $"[TradeMonitor.ChatCommand] Executed commandName '{CommandText}', result: {result}");
                }
                catch (Exception e)
                {
                    Log.HandleException(e);
                }
            }
        }
    }
}