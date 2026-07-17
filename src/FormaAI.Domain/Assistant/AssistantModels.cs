namespace FormaAI.Domain.Assistant;

public enum ConversationRole { User, Assistant }
public enum AssistantActionType { Meal, TrainingPlan }
public enum AssistantDraftStatus { Pending, Confirmed, Rejected, Expired }
public enum ToolExecutionStatus { Succeeded, Failed }
public enum AiProvider { Gemini, OpenAiCompatible }

public sealed class AiConfiguration
{
    private AiConfiguration() { }
    public AiConfiguration(AiProvider provider, string apiBaseUrl, string model, string encryptedApiKey)
    {
        Id = Guid.NewGuid();
        Update(provider, apiBaseUrl, model, encryptedApiKey);
    }

    public Guid Id { get; private set; }
    public AiProvider Provider { get; private set; }
    public string ApiBaseUrl { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string EncryptedApiKey { get; private set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; private set; }

    public void Update(AiProvider provider, string apiBaseUrl, string model, string? encryptedApiKey = null)
    {
        Provider = provider;
        ApiBaseUrl = apiBaseUrl.Trim().TrimEnd('/') + "/";
        Model = model.Trim();
        if (!string.IsNullOrWhiteSpace(encryptedApiKey)) EncryptedApiKey = encryptedApiKey;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class Conversation
{
    private Conversation() { }

    public Conversation(string userId, string title)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Title = title.Trim();
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public List<ConversationMessage> Messages { get; private set; } = [];

    public void Add(ConversationRole role, string content)
    {
        Messages.Add(new ConversationMessage(role, content));
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class ConversationMessage
{
    private ConversationMessage() { }
    public ConversationMessage(ConversationRole role, string content)
    {
        Id = Guid.NewGuid();
        Role = role;
        Content = content.Trim();
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public ConversationRole Role { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
}

public sealed class AssistantActionDraft
{
    private AssistantActionDraft() { }
    public AssistantActionDraft(string userId, Guid conversationId, AssistantActionType actionType, string payloadJson, DateTime expiresAtUtc)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        ConversationId = conversationId;
        ActionType = actionType;
        PayloadJson = payloadJson;
        Status = AssistantDraftStatus.Pending;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public Guid ConversationId { get; private set; }
    public AssistantActionType ActionType { get; private set; }
    public string PayloadJson { get; private set; } = string.Empty;
    public AssistantDraftStatus Status { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? ConfirmedResourceId { get; private set; }

    public bool IsExpired(DateTime nowUtc) => Status == AssistantDraftStatus.Pending && ExpiresAtUtc <= nowUtc;
    public void Expire() { if (Status == AssistantDraftStatus.Pending) Status = AssistantDraftStatus.Expired; }
    public void Confirm(Guid resourceId) { Status = AssistantDraftStatus.Confirmed; ConfirmedResourceId = resourceId; }
    public void Reject() { if (Status == AssistantDraftStatus.Pending) Status = AssistantDraftStatus.Rejected; }
}

public sealed class AiToolExecution
{
    private AiToolExecution() { }
    public AiToolExecution(string userId, Guid conversationId, string toolName, ToolExecutionStatus status, int durationMs, string? errorCode)
    {
        Id = Guid.NewGuid(); UserId = userId; ConversationId = conversationId; ToolName = toolName;
        Status = status; DurationMs = durationMs; ErrorCode = errorCode; CreatedAtUtc = DateTime.UtcNow;
    }
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public Guid ConversationId { get; private set; }
    public string ToolName { get; private set; } = string.Empty;
    public ToolExecutionStatus Status { get; private set; }
    public int DurationMs { get; private set; }
    public string? ErrorCode { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
