using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using PoeEye.TradeMonitor.Models;
using PoeShared.Scaffolding;
using PoeShared.UI.Controls;
using ReactiveUI;

namespace PoeEye.TradeMonitor.Controls
{
    internal sealed class MacroEditBox : ExtendedTextEditor
    {
        public static readonly DependencyProperty MacroCommandsProperty = DependencyProperty.Register(
            "MacroCommands", typeof(IReactiveList<MacroCommand>), typeof(MacroEditBox),
            new PropertyMetadata(default(IReactiveList<MacroCommand>), PropertyChangedCallback));

        public static readonly DependencyProperty CompletionListStyleProperty = DependencyProperty.Register(
            "CompletionListStyle", typeof(Style), typeof(MacroEditBox), new PropertyMetadata(default(Style)));

        private CompletionWindow completionWindow;

        public MacroEditBox()
        {
            Options = new TextEditorOptions
            {
                AllowScrollBelowDocument = false,
                AllowToggleOverstrikeMode = false,
                ConvertTabsToSpaces = true,
                EnableEmailHyperlinks = false,
                EnableHyperlinks = false,
                EnableImeSupport = false,
                EnableRectangularSelection = false,
                EnableTextDragDrop = false,
                HighlightCurrentLine = false,
                EnableVirtualSpace = false
            };

            TextArea.Document.UndoStack.SizeLimit = 0;

            var commandsToRemove = new[]
            {
                EditingCommands.EnterParagraphBreak,
                EditingCommands.EnterLineBreak,
                EditingCommands.MoveUpByLine,
                EditingCommands.MoveUpByPage,
                EditingCommands.MoveUpByParagraph,
                EditingCommands.MoveDownByLine,
                EditingCommands.MoveDownByPage,
                EditingCommands.MoveDownByParagraph,
                EditingCommands.SelectUpByLine,
                EditingCommands.SelectUpByPage,
                EditingCommands.SelectUpByParagraph,
                EditingCommands.SelectDownByLine,
                EditingCommands.SelectDownByPage,
                EditingCommands.SelectDownByParagraph
            };
            foreach (
                var binding in
                TextArea.DefaultInputHandler.CommandBindings.Where(x => commandsToRemove.Contains(x.Command)).ToArray())
            {
                TextArea.DefaultInputHandler.CommandBindings.Remove(binding);
            }

            foreach (
                var binding in
                TextArea.DefaultInputHandler.InputBindings.Where(x => commandsToRemove.Contains(x.Command)).ToArray())
            {
                TextArea.DefaultInputHandler.InputBindings.Remove(binding);
            }

            foreach (
                var binding in
                TextArea.DefaultInputHandler.CaretNavigation.CommandBindings.Where(
                    x => commandsToRemove.Contains(x.Command)).ToArray())
            {
                TextArea.DefaultInputHandler.CaretNavigation.CommandBindings.Remove(binding);
            }

            foreach (
                var binding in
                TextArea.DefaultInputHandler.CaretNavigation.InputBindings.Where(
                    x => commandsToRemove.Contains(x.Command)).ToArray())
            {
                TextArea.DefaultInputHandler.CaretNavigation.InputBindings.Remove(binding);
            }

            TextArea.TextEntered += TextAreaOnTextEntered;

            TextArea.TextView.LineTransformers.Add(new ColorizeAvalonEdit(() => MacroCommands));
        }

        public IReactiveList<MacroCommand> MacroCommands
        {
            get => (IReactiveList<MacroCommand>)GetValue(MacroCommandsProperty);
            set => SetValue(MacroCommandsProperty, value);
        }

        public Style CompletionListStyle
        {
            get => (Style)GetValue(CompletionListStyleProperty);
            set => SetValue(CompletionListStyleProperty, value);
        }

        private void TextAreaOnTextEntered(object sender, TextCompositionEventArgs textCompositionEventArgs)
        {
            if (textCompositionEventArgs.Text == "/")
            {
                completionWindow = new CompletionWindow(TextArea);
                completionWindow.CompletionList.Style = CompletionListStyle;

                var data = completionWindow.CompletionList.CompletionData;
                data.Clear();
                MacroCommands.EmptyIfNull()
                             .Select(x => new MacroCommandCompletionData(x))
                             .ForEach(data.Add);

                completionWindow.CompletionList.SelectedItem = data.FirstOrDefault();

                completionWindow.Show();
                completionWindow.Closed += delegate { completionWindow = null; };
            }
        }

        private static void PropertyChangedCallback(DependencyObject dependencyObject,
                                                    DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var editor = dependencyObject as MacroEditBox;
            if (editor == null)
            {
                return;
            }

            editor.TextArea.TextView.Redraw();
        }

        private sealed class MacroCommandCompletionData : ICompletionData
        {
            private readonly MacroCommand command;

            public MacroCommandCompletionData(MacroCommand command)
            {
                this.command = command;
            }

            public ImageSource Image => null;

            public string Text => command.Label;

            public object Content => Text;

            public object Description => $"'/{command.CommandText}' - {command.Description}";

            public void Complete(
                TextArea textArea,
                ISegment completionSegment,
                EventArgs insertionRequestEventArgs)
            {
                textArea.Document.Replace(completionSegment, command.CommandText);
            }

            public double Priority { get; } = 0;
        }

        private sealed class VisualElementGenerator : VisualLineElementGenerator
        {
            private readonly Func<IEnumerable<MacroCommand>> commandsSupplier;

            public VisualElementGenerator(Func<IEnumerable<MacroCommand>> commands)
            {
                commandsSupplier = commands;
            }

            private Match FindMatch(int startOffset)
            {
                var endOffset = CurrentContext.VisualLine.LastDocumentLine.EndOffset;
                var document = CurrentContext.Document;
                var relevantText = document.GetText(startOffset, endOffset - startOffset);

                return FindMatch(relevantText);
            }

            private Match FindMatch(string text)
            {
                var commands = commandsSupplier();
                var match = commands
                            .EmptyIfNull()
                            .Select(x => x.TryToMatch(text))
                            .Where(x => x.Success)
                            .ToArray();

                return match.Length > 0 ? match[0] : Match.Empty;
            }

            public override int GetFirstInterestedOffset(int startOffset)
            {
                var match = FindMatch(startOffset);

                return match.Success
                    ? match.Index + startOffset
                    : -1;
            }

            public override VisualLineElement ConstructElement(int startOffset)
            {
                var m = FindMatch(startOffset);
                // check whether there's a match exactly at offset
                if (!m.Success || m.Index != 0)
                {
                    return null;
                }

                var border = new Border
                {
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(1),
                    Child = new TextBlock
                    {
                        Text = m.Value,
                        FontSize = 12,
                        Margin = new Thickness(2, 1, 2, 1)
                    }
                };
                return new InlineObjectElement(m.Length, border);
            }
        }

        public class ColorizeAvalonEdit : DocumentColorizingTransformer
        {
            private readonly Func<IEnumerable<MacroCommand>> commandsSupplier;

            public ColorizeAvalonEdit(Func<IEnumerable<MacroCommand>> commands)
            {
                commandsSupplier = commands;
            }

            protected override void ColorizeLine(DocumentLine line)
            {
                var lineStartOffset = line.Offset;
                var text = CurrentContext.Document.GetText(line);

                var commands = commandsSupplier();

                foreach (var macroCommand in commands.EmptyIfNull())
                {
                    Match match;
                    var startIdx = 0;
                    while ((match = macroCommand.TryToMatch(text, startIdx)).Success)
                    {
                        startIdx = match.Index + match.Length;
                        ChangeLinePart(
                            lineStartOffset + match.Index, // startOffset
                            lineStartOffset + startIdx, // endOffset
                            Highlight);
                    }
                }
            }

            private void Highlight(VisualLineElement element)
            {
                var tf = element.TextRunProperties.Typeface;

                element.TextRunProperties.SetForegroundBrush(Brushes.Aqua);

                element.TextRunProperties.SetTypeface(
                    new Typeface(
                        tf.FontFamily,
                        FontStyles.Italic,
                        FontWeights.Normal,
                        tf.Stretch
                    ));
            }
        }
    }
}