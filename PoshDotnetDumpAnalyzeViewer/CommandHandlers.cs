﻿using System.Collections.Immutable;
using Terminal.Gui;

namespace PoshDotnetDumpAnalyzeViewer;

public struct DefaultOutputParser : IOutputParser
{
    public CommandOutput<OutputLine> Parse(string command, string[] output, bool isOk)
    {
        return new(command, isOk, output.Map(x => new OutputLine(x)));
    }
}

public sealed record DefaultCommandHandler(IClipboard Clipboard) : CommandHandlerBase<DefaultOutputParser>(Clipboard)
{
    public override ImmutableArray<string> SupportedCommands { get; } = new();
    public override bool IsSupported(string command) => true;

    protected override Task<View> ProcessOutput(CommandOutput<OutputLine> output)
    {
        var views = UI.MakeDefaultCommandViews().SetupLogic(Clipboard, output.Lines);
        return Task.FromResult((View) views.Window);
    }
}

public sealed record QuitCommandHandler(IClipboard Clipboard) : CommandHandlerBase<DefaultOutputParser>(Clipboard)
{
    public override ImmutableArray<string> SupportedCommands { get; } =
        ImmutableArray.Create(Commands.Exit, Commands.Q, Commands.Quit);

    public override bool IsSupported(string command) =>
        SupportedCommands.Any(supportedCommand => command.Equals(supportedCommand, StringComparison.OrdinalIgnoreCase));

    protected override Task<View> ProcessOutput(CommandOutput<OutputLine> output)
    {
        Environment.Exit(0);
        return Task.FromResult<View>(null);
    }
}

public struct HelpOutputParser : IOutputParser
{
    public CommandOutput<OutputLine> Parse(string command, string[] output, bool isOk)
        => Parser.Help.Parse(command, output, isOk);
}

public sealed record HelpCommandHandler
    (IClipboard Clipboard, CommandQueue CommandQueue) : CommandHandlerBase<HelpOutputParser>(Clipboard)
{
    public override ImmutableArray<string> SupportedCommands { get; } = ImmutableArray.Create(Commands.Help);

    public override bool IsSupported(string command) =>
        command.Equals(Commands.Help, StringComparison.OrdinalIgnoreCase);

    protected override Task<View> ProcessOutput(CommandOutput<OutputLine> output)
    {
        var (window, listView, _) = UI.MakeDefaultCommandViews().SetupLogic(Clipboard, output.Lines);

        listView.KeyPress += args =>
        {
            // To function ?
            if (args.KeyEvent.Key == Key.Enter)
            {
                if (listView.GetSelectedOutput<IHelpCommand>() is { } line)
                {
                    var command = line.Commands[0];
                    CommandQueue.SendCommand($"help {command}");
                    args.Handled = true;
                }
            }
        };

        return Task.FromResult((View) window);
    }
}

public struct DumpHeapOutputParser : IOutputParser
{
    public CommandOutput<OutputLine> Parse(string command, string[] output, bool isOk) =>
        Parser.DumpHeap.Parse(command, output, isOk);
}

public sealed record DumpHeapCommandHandler
    (IClipboard Clipboard, CommandQueue CommandQueue) : DefaultViewsHandlerBase<DumpHeapOutputParser>(Clipboard, CommandQueue)
{
    public override ImmutableArray<string> SupportedCommands { get; } = ImmutableArray.Create(Commands.DumpHeap);
}