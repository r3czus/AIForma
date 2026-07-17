using System.ComponentModel.DataAnnotations;
using FormaAI.Contracts.Nutrition;
using FormaAI.Domain.Assistant;
using FormaAI.Contracts.Training;

namespace FormaAI.Contracts.Assistant;

public sealed record SendAssistantMessageRequest(Guid? ConversationId, [Required, MaxLength(2000)] string Message, DateOnly? LocalDate = null);
public sealed record AssistantMessageResponse(Guid ConversationId, string Reply, AssistantDraftResponse? Draft, AssistantTrainingDraftResponse? TrainingPlanDraft = null);
public sealed record ConversationSummaryResponse(Guid Id, string Title, DateTime UpdatedAtUtc);
public sealed record ConversationMessageResponse(Guid Id, ConversationRole Role, string Content, DateTime CreatedAtUtc);
public sealed record ConversationResponse(Guid Id, string Title, IReadOnlyList<ConversationMessageResponse> Messages);
public sealed record AssistantDraftItem(Guid ProductId, string ProductName, decimal AmountGrams, bool IsEstimated, MacroResponse Macro);
public sealed record AssistantMealDraftPayload(string Name, DateTimeOffset OccurredAt, DateOnly LocalDate, IReadOnlyList<AssistantDraftItem> Items);
public sealed record AssistantDraftResponse(Guid Id, AssistantDraftStatus Status, string Name, DateTimeOffset OccurredAt, IReadOnlyList<AssistantDraftItem> Items, MacroResponse Macro, IReadOnlyList<string> MissingFromPantry, DateTime ExpiresAtUtc);
public sealed record AssistantTrainingPlanDraftPayload(SaveTrainingPlanRequest Plan);
public sealed record AssistantTrainingDraftResponse(Guid Id, AssistantDraftStatus Status, SaveTrainingPlanRequest Plan, DateTime ExpiresAtUtc);
