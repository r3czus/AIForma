using FormaAI.Domain.Nutrition;

namespace FormaAI.Application.Nutrition;

public static class UnitConverter
{
    public static decimal? Convert(decimal amount, ServingUnit from, ServingUnit to, decimal? gramsPerPiece)
    {
        if (from == to) return amount;
        if (gramsPerPiece is null or <= 0) return null;
        if (from == ServingUnit.Piece && to == ServingUnit.Gram) return amount * gramsPerPiece.Value;
        if (from == ServingUnit.Gram && to == ServingUnit.Piece) return amount / gramsPerPiece.Value;
        return null;
    }

    public static decimal? ToGrams(decimal amount, ServingUnit unit, decimal? gramsPerPiece) => unit switch
    {
        ServingUnit.Gram => amount,
        ServingUnit.Milliliter => amount,
        ServingUnit.Piece when gramsPerPiece is > 0 => amount * gramsPerPiece.Value,
        _ => null
    };
}
