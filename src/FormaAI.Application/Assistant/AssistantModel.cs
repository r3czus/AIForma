using System.Text.Json;

namespace FormaAI.Application.Assistant;

public interface IAssistantModel
{
    Task<AssistantModelTurn> Generate(AssistantModelRequest request, CancellationToken cancellationToken);
}

public sealed record AssistantModelRequest(string SystemInstruction, IReadOnlyList<AssistantModelMessage> Messages, IReadOnlyList<AssistantToolResult> ToolResults);
public sealed record AssistantModelMessage(string Role, string Text);
public sealed record AssistantToolResult(string Name, JsonElement Arguments, string Result);
public sealed record AssistantToolCall(string Name, JsonElement Arguments);
public sealed record AssistantModelTurn(string? Reply, AssistantToolCall? ToolCall, int InputTokens, int OutputTokens);

public sealed class AssistantModelUnavailableException : Exception
{
    public AssistantModelUnavailableException(string message) : base(message) { }
}
