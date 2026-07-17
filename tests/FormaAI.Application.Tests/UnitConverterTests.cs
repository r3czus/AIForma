using FormaAI.Application.Nutrition;
using FormaAI.Domain.Nutrition;

namespace FormaAI.Application.Tests;

public sealed class UnitConverterTests
{
    [Fact]
    public void ConvertsPiecesAndGramsWhenProductHasPieceWeight()
    {
        Assert.Equal(150m, UnitConverter.Convert(3m, ServingUnit.Piece, ServingUnit.Gram, 50m));
        Assert.Equal(3m, UnitConverter.Convert(150m, ServingUnit.Gram, ServingUnit.Piece, 50m));
        Assert.Null(UnitConverter.Convert(1m, ServingUnit.Piece, ServingUnit.Gram, null));
        Assert.Null(UnitConverter.Convert(100m, ServingUnit.Milliliter, ServingUnit.Gram, null));
    }
}
