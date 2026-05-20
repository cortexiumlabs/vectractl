using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using Vectra.Client.Abstractions;
using VectraCtl.Core.Services.Logger;

namespace VectraCtl.Commands;

internal static class PoliciesCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("policies", "Browse Vectra governance policies");

        command.Subcommands.Add(CreateListCommand(serviceProvider));
        command.Subcommands.Add(CreateDetailsCommand(serviceProvider));

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

        var cmd = new Command("list", "List all governance policies")
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
                var policies = await client.Policies.ListAsync(
                    parseResult.GetValue(pageOption),
                    parseResult.GetValue(pageSizeOption), ct);

                logger.Write(policies, parseResult.GetValue(outputOption));
            }
            catch (Exception ex)
            {
                logger.WriteError(ex.Message);
            }
        });

        return cmd;
    }

    private static Command CreateDetailsCommand(IServiceProvider serviceProvider)
    {
        var nameOption = new Option<string>("--name") { Description = "Policy name", Required = true };
        var outputOption = new Option<OutputType>("--output", "-o")
        {
            Description = "Formatting Command-Line output",
            Required = false,
            DefaultValueFactory = _ => OutputType.Json
        };

        var cmd = new Command("details", "Show full details of a specific policy")
        {
            nameOption,
            outputOption
        };

        cmd.SetAction(async (parseResult, ct) =>
        {
            var logger = serviceProvider.GetRequiredService<IVectraCtlLogger>();
            try
            {
                var client = serviceProvider.GetRequiredService<IVectraClient>();
                var policy = await client.Policies.GetAsync(parseResult.GetValue(nameOption)!, ct);
                logger.Write(policy, parseResult.GetValue(outputOption));
            }
            catch (Exception ex)
            {
                logger.WriteError(ex.Message);
            }
        });

        return cmd;
    }
}
