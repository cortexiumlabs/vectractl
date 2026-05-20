using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using Vectra.Client.Abstractions;
using Vectra.Client.Models.Hitl;
using VectraCtl.Core.Services.Logger;

namespace VectraCtl.Commands;

internal static class HitlCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("hitl", "Manage Human-in-the-Loop (HITL) requests");

        command.Subcommands.Add(CreateListCommand(serviceProvider));
        command.Subcommands.Add(CreateStatusCommand(serviceProvider));
        command.Subcommands.Add(CreateApproveCommand(serviceProvider));
        command.Subcommands.Add(CreateDenyCommand(serviceProvider));

        return command;
    }

    private static Command CreateListCommand(IServiceProvider serviceProvider)
    {
        var pageOption = new Option<int>("--page") { Description = "Page number", DefaultValueFactory = _ => 1 };
        var pageSizeOption = new Option<int>("--page-size") { Description = "Page size", DefaultValueFactory = _ => 25 };
        var outputOption = new Option<OutputType>("--output", "-o")
        {
            Description = "Formatting Command-Line output",
            Required = false,
            DefaultValueFactory = _ => OutputType.Json
        };

        var cmd = new Command("list", "List all pending HITL requests")
        {
            pageOption,
            pageSizeOption,
            outputOption
        };

        cmd.SetAction(async (parseResult, ct) =>
        {
            var logger = serviceProvider.GetRequiredService<IVectraCtlLogger>();
            try
            {
                var client = serviceProvider.GetRequiredService<IVectraClient>();
                var items = await client.Hitl.GetAllPendingAsync(
                    parseResult.GetValue(pageOption),
                    parseResult.GetValue(pageSizeOption), ct);

                logger.Write(items, parseResult.GetValue(outputOption));
            }
            catch (Exception ex)
            {
                logger.WriteError(ex.Message);
            }
        });

        return cmd;
    }

    private static Command CreateStatusCommand(IServiceProvider serviceProvider)
    {
        var idOption = new Option<string>("--id") { Description = "HITL request ID", Required = true };
        var outputOption = new Option<OutputType>("--output", "-o")
        {
            Description = "Formatting Command-Line output",
            Required = false,
            DefaultValueFactory = _ => OutputType.Json
        };

        var cmd = new Command("status", "Get the status of a HITL request")
        {
            idOption,
            outputOption
        };

        cmd.SetAction(async (parseResult, ct) =>
        {
            var logger = serviceProvider.GetRequiredService<IVectraCtlLogger>();
            try
            {
                var client = serviceProvider.GetRequiredService<IVectraClient>();
                var status = await client.Hitl.GetStatusAsync(parseResult.GetValue(idOption)!, ct);
                logger.Write(status, parseResult.GetValue(outputOption));
            }
            catch (Exception ex)
            {
                logger.WriteError(ex.Message);
            }
        });

        return cmd;
    }

    private static Command CreateApproveCommand(IServiceProvider serviceProvider)
    {
        var idOption = new Option<string>("--id") { Description = "HITL request ID", Required = true };
        var commentOption = new Option<string?>("--comment") { Description = "Optional reviewer comment" };

        var cmd = new Command("approve", "Approve a pending HITL request")
        {
            idOption,
            commentOption
        };

        cmd.SetAction(async (parseResult, ct) =>
        {
            var logger = serviceProvider.GetRequiredService<IVectraCtlLogger>();
            try
            {
                var client = serviceProvider.GetRequiredService<IVectraClient>();
                await client.Hitl.ApproveAsync(
                    parseResult.GetValue(idOption)!,
                    new ReviewDecisionRequest { Comment = parseResult.GetValue(commentOption) },
                    ct);

                logger.Write("HITL request approved.");
            }
            catch (Exception ex)
            {
                logger.WriteError(ex.Message);
            }
        });

        return cmd;
    }

    private static Command CreateDenyCommand(IServiceProvider serviceProvider)
    {
        var idOption = new Option<string>("--id") { Description = "HITL request ID", Required = true };
        var commentOption = new Option<string?>("--comment") { Description = "Optional reviewer comment" };

        var cmd = new Command("deny", "Deny a pending HITL request")
        {
            idOption,
            commentOption
        };

        cmd.SetAction(async (parseResult, ct) =>
        {
            var logger = serviceProvider.GetRequiredService<IVectraCtlLogger>();
            try
            {
                var client = serviceProvider.GetRequiredService<IVectraClient>();
                await client.Hitl.DenyAsync(
                    parseResult.GetValue(idOption)!,
                    new ReviewDecisionRequest { Comment = parseResult.GetValue(commentOption) },
                    ct);

                logger.Write("HITL request denied.");
            }
            catch (Exception ex)
            {
                logger.WriteError(ex.Message);
            }
        });

        return cmd;
    }
}

