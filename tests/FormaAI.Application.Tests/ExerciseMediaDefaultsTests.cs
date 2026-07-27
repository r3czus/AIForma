using FormaAI.Application.Training;

namespace FormaAI.Application.Tests;

public sealed class ExerciseMediaDefaultsTests
{
    [Theory]
    [InlineData("Wyciskanie sztangi leżąc", "/images/exercises/wyciskanie-sztangi-lezac.png")]
    [InlineData("Wiosłowanie na maszynie z podparciem", "/images/exercises/wioslowanie-maszyna-podparcie.png")]
    public void ResolvesTwoBundledExerciseImages(string exerciseName, string expectedUrl)
    {
        var media = ExerciseMediaDefaults.Resolve(exerciseName);

        Assert.NotNull(media);
        Assert.Equal(expectedUrl, media.Url);
        Assert.Equal("image/png", media.ContentType);
    }

    [Fact]
    public void LeavesOtherExercisesForUserUploads()
    {
        Assert.Null(ExerciseMediaDefaults.Resolve("Przysiad ze sztangą"));
    }
}
