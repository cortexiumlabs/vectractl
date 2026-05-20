using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using Vectra.Client.Abstractions;
using Vectra.Client.Models.Agents;
using VectraCtl.Core.Services.Logger;

namespace VectraCtl.Commands;

internal static class AgentsCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("agents", "Manage AI agents registered in Vectra");

        command.Subcommands.Add(CreateListCommand(serviceProvider));
        command.Subcommands.Add(CreateRegisterCommand(serviceProvider));
        command.Subcommands.Add(CreateAssignPolicyCommand(serviceProvider));
        command.Subcommands.Add(CreateDeleteCommand(serviceProvider));

        return command;
    }

    private static Command CreateListCommand(IServiceProvider serviceProvider)
    {
        var pageOption = new Option<int>("--page")
        {
            Description = "Page number for pagination (default: 1)",
            DefaultValueFactory = (result) => 1
        };

        var pageSizeOption = new Option<int>("--page-size")
        {
            Description = "Page size for pagination (default: 25)",
            DefaultValueFactory = (result) => 25
        };

        var outputOption = new Option<OutputType>("--output", "-o")
        {
            Description = "Formatting Command-Line output",
            Required = false,
            DefaultValueFactory = (result) => OutputType.Json
        };

        var cmd = new Command("list", "List all registered AI agents")
        {
            pageOption,
            pageSizeOption,
            outputOption
        };

        cmd.SetAction(async (parseResult, cancellationToken) =>
        {
            var logger = serviceProvider.GetRequiredService<IVectraCtlLogger>();
            try
            {
                var client = serviceProvider.GetRequiredService<IVectraClient>();
                var agents = await client.Agents.ListAsync(
                    parseResult.GetValue(pageOption),
                    parseResult.GetValue(pageSizeOption), cancellationToken);

                logger.Write(agents, parseResult.GetValue(outputOption));
            }
            catch (Exception ex)
            {
                logger.WriteError(ex.Message);
            }
        });

        return cmd;
    }

    private static Command CreateRegisterCommand(IServiceProvider serviceProvider)
    {
        var nameOption = new Option<string>("--name") { Description = "Agent name", Required = true };
        var ownerOption = new Option<string>("--owner") { Description = "Owner / team identifier", Required = true };
        var secretOption = new Option<string>("--secret") { Description = "Client secret for the agent", Required = true };

        var cmd = new Command("register", "Register a new AI agent")
        {
            nameOption,
            ownerOption,
            secretOption
        };

        cmd.SetAction(async (parseResult, ct) =>
        {
            var logger = serviceProvider.GetRequiredService<IVectraCtlLogger>();
            try
            {
                var client = serviceProvider.GetRequiredService<IVectraClient>();
                var result = await client.Agents.RegisterAsync(new RegisterAgentRequest
                {
                    Name = parseResult.GetValue(nameOption)!,
                    OwnerId = parseResult.GetValue(ownerOption)!,
                    ClientSecret = parseResult.GetValue(secretOption)!
                }, ct);

                logger.Write(result);
            }
            catch (Exception ex)
            {
                logger.WriteError(ex.Message);
            }
        });

        return cmd;
    }

    private static Command CreateAssignPolicyCommand(IServiceProvider serviceProvider)
    {
        var agentIdOption = new Option<Guid>("--agent-id") { Description = "Agent ID (GUID)", Required = true };
        var policyOption = new Option<string>("--policy") { Description = "Policy name to assign", Required = true };

        var cmd = new Command("assign-policy", "Assign a policy to an agent")
        {
            agentIdOption,
            policyOption
        };

        cmd.SetAction(async (parseResult, ct) =>
        {
            var logger = serviceProvider.GetRequiredService<IVectraCtlLogger>();
            try
            {
                var client = serviceProvider.GetRequiredService<IVectraClient>();
                await client.Agents.AssignPolicyAsync(
                    parseResult.GetValue(agentIdOption),
                    new AssignPolicyRequest { PolicyName = parseResult.GetValue(policyOption)! },
                    ct);

                logger.Write("Policy assigned successfully.");
            }
            catch (Exception ex)
            {
                logger.WriteError(ex.Message);
            }
        });

        return cmd;
    }

    private static Command CreateDeleteCommand(IServiceProvider serviceProvider)
    {
        var agentIdOption = new Option<Guid>("--agent-id") { Description = "Agent ID (GUID)", Required = true };

        var cmd = new Command("delete", "Delete an AI agent")
        {
            agentIdOption
        };

        cmd.SetAction(async (parseResult, ct) =>
        {
            var logger = serviceProvider.GetRequiredService<IVectraCtlLogger>();
            try
            {
                var client = serviceProvider.GetRequiredService<IVectraClient>();
                await client.Agents.DeleteAsync(parseResult.GetValue(agentIdOption), ct);
                logger.Write("Agent deleted.");
            }
            catch (Exception ex)
            {
                logger.WriteError(ex.Message);
            }
        });

        return cmd;
    }
}
