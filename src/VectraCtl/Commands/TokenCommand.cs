using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using Vectra.Client.Abstractions;
using Vectra.Client.Models.Tokens;
using VectraCtl.Core.Services.Logger;

namespace VectraCtl.Commands;

internal static class TokenCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var agentIdOption = new Option<Guid>("--agent-id")
        {
            Description = "Agent ID (GUID)",
            Required = true,
        };

        var secretOption = new Option<string>("--secret")
        {
            Description = "Agent client secret",
            Required = true
        };

        var outputOption = CommandHelpers.CreateOutputOption();

        var command = new Command("token", "Exchange agent credentials for a JWT bearer token")
        {
            agentIdOption,
            secretOption,
            outputOption
        };

        command.SetAction((parseResult, ct) => CommandHelpers.ExecuteAsync(serviceProvider, async (logger, sp) =>
        {
            var client = sp.GetRequiredService<IVectraClient>();
            var result = await client.Tokens.GenerateAsync(new GenerateTokenRequest
            {
                AgentId = parseResult.GetValue(agentIdOption),
                ClientSecret = parseResult.GetValue(secretOption)!
            }, ct);
            logger.Write(result, parseResult.GetValue(outputOption));
        }));

        return command;
    }
}

