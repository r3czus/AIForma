namespace FormaAI.Application.Assistant;

public static class AssistantFailureCopy
{
    public static string ForRequest(string request)
    {
        if (request.Contains("create_completed_workout_draft", StringComparison.OrdinalIgnoreCase))
        {
            return "Nie udało się rozpoznać całego treningu. Sprawdź ćwiczenia, serie, ciężary i powtórzenia.";
        }

        return "Nie udało się sprawdzić podanych danych. Doprecyzuj produkt i wielkość porcji.";
    }
}
