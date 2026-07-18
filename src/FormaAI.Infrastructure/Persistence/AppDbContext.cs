using FormaAI.Domain.Users;
using FormaAI.Domain.Nutrition;
using FormaAI.Domain.Training;
using FormaAI.Domain.Progress;
using FormaAI.Domain.Pantry;
using FormaAI.Domain.Assistant;
using FormaAI.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FormaAI.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<NutritionTarget> NutritionTargets => Set<NutritionTarget>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<MealItem> MealItems => Set<MealItem>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<TrainingPlan> TrainingPlans => Set<TrainingPlan>();
    public DbSet<TrainingDay> TrainingDays => Set<TrainingDay>();
    public DbSet<PlannedExercise> PlannedExercises => Set<PlannedExercise>();
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
    public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();
    public DbSet<CompletedSet> CompletedSets => Set<CompletedSet>();
    public DbSet<BodyMeasurement> BodyMeasurements => Set<BodyMeasurement>();
    public DbSet<PantryItem> PantryItems => Set<PantryItem>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();
    public DbSet<AssistantActionDraft> AssistantActionDrafts => Set<AssistantActionDraft>();
    public DbSet<AiToolExecution> AiToolExecutions => Set<AiToolExecution>();
    public DbSet<AiConfiguration> AiConfigurations => Set<AiConfiguration>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserProfile>(profile =>
        {
            profile.ToTable("UserProfiles");
            profile.HasKey(x => x.Id);
            profile.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            profile.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired();
            profile.Property(x => x.HeightCm).HasPrecision(5, 1);
            profile.Property(x => x.StartingWeightKg).HasPrecision(6, 2);
            profile.Property(x => x.TargetWeightKg).HasPrecision(6, 2);
            profile.Property(x => x.MealSlots).HasMaxLength(500).IsRequired();
            profile.HasIndex(x => x.UserId).IsUnique();
        });

        builder.Entity<NutritionTarget>(target =>
        {
            target.HasKey(x => x.Id);
            target.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            target.Property(x => x.CaloriesKcal).HasPrecision(7, 2);
            target.Property(x => x.ProteinG).HasPrecision(7, 2);
            target.Property(x => x.FatG).HasPrecision(7, 2);
            target.Property(x => x.CarbohydratesG).HasPrecision(7, 2);
            target.HasIndex(x => new { x.UserId, x.EffectiveFrom });
        });

        builder.Entity<Product>(product =>
        {
            product.HasKey(x => x.Id);
            product.Property(x => x.OwnerUserId).HasMaxLength(450);
            product.Property(x => x.Name).HasMaxLength(200).IsRequired();
            product.Property(x => x.Brand).HasMaxLength(150);
            product.Property(x => x.Barcode).HasMaxLength(32);
            product.Property(x => x.ExternalId).HasMaxLength(100);
            product.Property(x => x.DefaultServingAmount).HasPrecision(8, 2);
            product.Property(x => x.GramsPerPiece).HasPrecision(8, 2);
            product.Property(x => x.CaloriesPer100).HasPrecision(8, 2);
            product.Property(x => x.ProteinPer100).HasPrecision(8, 2);
            product.Property(x => x.FatPer100).HasPrecision(8, 2);
            product.Property(x => x.CarbohydratesPer100).HasPrecision(8, 2);
            product.HasIndex(x => x.Barcode);
            product.HasIndex(x => x.Name);
        });

        builder.Entity<Meal>(meal =>
        {
            meal.HasKey(x => x.Id);
            meal.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            meal.Property(x => x.Name).HasMaxLength(120).IsRequired();
            meal.Property(x => x.Notes).HasMaxLength(1000);
            meal.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.MealId).OnDelete(DeleteBehavior.Cascade);
            meal.HasIndex(x => new { x.UserId, x.LocalDate });
        });

        builder.Entity<MealItem>(item =>
        {
            item.HasKey(x => x.Id);
            item.Property(x => x.ProductNameSnapshot).HasMaxLength(200).IsRequired();
            item.Property(x => x.AmountGrams).HasPrecision(9, 2);
            item.Property(x => x.CaloriesKcal).HasPrecision(9, 2);
            item.Property(x => x.ProteinG).HasPrecision(9, 2);
            item.Property(x => x.FatG).HasPrecision(9, 2);
            item.Property(x => x.CarbohydratesG).HasPrecision(9, 2);
            item.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Exercise>(exercise =>
        {
            exercise.HasKey(x => x.Id);
            exercise.Property(x => x.OwnerUserId).HasMaxLength(450);
            exercise.Property(x => x.Name).HasMaxLength(150).IsRequired();
            exercise.Property(x => x.Description).HasMaxLength(1000);
            exercise.HasIndex(x => x.Name);
            var seededAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            exercise.HasData(
                new { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), OwnerUserId = (string?)null, Name = "Przysiad ze sztangą", PrimaryMuscleGroup = MuscleGroup.Quadriceps, Equipment = Equipment.Barbell, IsUnilateral = false, IsActive = true, CreatedAtUtc = seededAt, UpdatedAtUtc = seededAt },
                new { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), OwnerUserId = (string?)null, Name = "Wyciskanie sztangi leżąc", PrimaryMuscleGroup = MuscleGroup.Chest, Equipment = Equipment.Barbell, IsUnilateral = false, IsActive = true, CreatedAtUtc = seededAt, UpdatedAtUtc = seededAt },
                new { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), OwnerUserId = (string?)null, Name = "Martwy ciąg", PrimaryMuscleGroup = MuscleGroup.Back, Equipment = Equipment.Barbell, IsUnilateral = false, IsActive = true, CreatedAtUtc = seededAt, UpdatedAtUtc = seededAt },
                new { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), OwnerUserId = (string?)null, Name = "Wiosłowanie sztangą", PrimaryMuscleGroup = MuscleGroup.Back, Equipment = Equipment.Barbell, IsUnilateral = false, IsActive = true, CreatedAtUtc = seededAt, UpdatedAtUtc = seededAt },
                new { Id = Guid.Parse("10000000-0000-0000-0000-000000000005"), OwnerUserId = (string?)null, Name = "Wyciskanie nad głowę", PrimaryMuscleGroup = MuscleGroup.Shoulders, Equipment = Equipment.Barbell, IsUnilateral = false, IsActive = true, CreatedAtUtc = seededAt, UpdatedAtUtc = seededAt },
                new { Id = Guid.Parse("10000000-0000-0000-0000-000000000006"), OwnerUserId = (string?)null, Name = "Podciąganie", PrimaryMuscleGroup = MuscleGroup.Back, Equipment = Equipment.Bodyweight, IsUnilateral = false, IsActive = true, CreatedAtUtc = seededAt, UpdatedAtUtc = seededAt });
        });

        builder.Entity<TrainingPlan>(plan =>
        {
            plan.HasKey(x => x.Id);
            plan.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            plan.Property(x => x.Name).HasMaxLength(150).IsRequired();
            plan.Property(x => x.Goal).HasMaxLength(500);
            plan.HasMany(x => x.Days).WithOne().HasForeignKey(x => x.TrainingPlanId).OnDelete(DeleteBehavior.Cascade);
            plan.HasIndex(x => new { x.UserId, x.IsActive });
        });

        builder.Entity<TrainingDay>(day =>
        {
            day.HasKey(x => x.Id);
            day.Property(x => x.Name).HasMaxLength(100).IsRequired();
            day.HasMany(x => x.Exercises).WithOne().HasForeignKey(x => x.TrainingDayId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PlannedExercise>(planned =>
        {
            planned.HasKey(x => x.Id);
            planned.Property(x => x.TargetRir).HasPrecision(3, 1);
            planned.Property(x => x.Notes).HasMaxLength(500);
            planned.HasOne<Exercise>().WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<WorkoutSession>(session =>
        {
            session.HasKey(x => x.Id);
            session.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            session.Property(x => x.NameSnapshot).HasMaxLength(150).IsRequired();
            session.Property(x => x.Notes).HasMaxLength(1000);
            session.HasMany(x => x.Exercises).WithOne().HasForeignKey(x => x.WorkoutSessionId).OnDelete(DeleteBehavior.Cascade);
            session.HasOne<TrainingPlan>().WithMany().HasForeignKey(x => x.TrainingPlanId).OnDelete(DeleteBehavior.NoAction);
            session.HasOne<TrainingDay>().WithMany().HasForeignKey(x => x.TrainingDayId).OnDelete(DeleteBehavior.NoAction);
            session.HasIndex(x => new { x.UserId, x.StartedAtUtc });
        });

        builder.Entity<WorkoutExercise>(workout =>
        {
            workout.HasKey(x => x.Id);
            workout.Property(x => x.ExerciseNameSnapshot).HasMaxLength(150).IsRequired();
            workout.Property(x => x.TargetRir).HasPrecision(3, 1);
            workout.HasMany(x => x.Sets).WithOne().HasForeignKey(x => x.WorkoutExerciseId).OnDelete(DeleteBehavior.Cascade);
            workout.HasOne<Exercise>().WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<CompletedSet>(set =>
        {
            set.HasKey(x => x.Id);
            set.Property(x => x.WeightKg).HasPrecision(7, 2);
            set.Property(x => x.Rir).HasPrecision(3, 1);
            set.Property(x => x.Notes).HasMaxLength(500);
        });

        builder.Entity<BodyMeasurement>(measurement =>
        {
            measurement.HasKey(x => x.Id);
            measurement.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            measurement.Property(x => x.WeightKg).HasPrecision(6, 2);
            measurement.Property(x => x.WaistCm).HasPrecision(6, 2);
            measurement.Property(x => x.ChestCm).HasPrecision(6, 2);
            measurement.Property(x => x.HipsCm).HasPrecision(6, 2);
            measurement.Property(x => x.ArmCm).HasPrecision(6, 2);
            measurement.Property(x => x.ThighCm).HasPrecision(6, 2);
            measurement.Property(x => x.Notes).HasMaxLength(500);
            measurement.HasIndex(x => new { x.UserId, x.LocalDate });
        });

        builder.Entity<PantryItem>(item =>
        {
            item.HasKey(x => x.Id);
            item.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            item.Property(x => x.Quantity).HasPrecision(10, 2);
            var rowVersion = item.Property(x => x.RowVersion);
            if (Database.IsRelational()) rowVersion.IsRequired().IsRowVersion();
            else rowVersion.IsRequired(false).IsConcurrencyToken(false).ValueGeneratedNever();
            item.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            item.HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();
        });

        builder.Entity<Recipe>(recipe =>
        {
            recipe.HasKey(x => x.Id);
            recipe.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            recipe.Property(x => x.Name).HasMaxLength(150).IsRequired();
            recipe.Property(x => x.Description).HasMaxLength(1000);
            recipe.HasMany(x => x.Ingredients).WithOne().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Cascade);
            recipe.HasIndex(x => new { x.UserId, x.Name });
        });

        builder.Entity<RecipeIngredient>(ingredient =>
        {
            ingredient.HasKey(x => x.Id);
            ingredient.Property(x => x.Quantity).HasPrecision(10, 2);
            ingredient.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ShoppingList>(list =>
        {
            list.HasKey(x => x.Id);
            list.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            list.Property(x => x.Name).HasMaxLength(150).IsRequired();
            list.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.ShoppingListId).OnDelete(DeleteBehavior.Cascade);
            list.HasIndex(x => new { x.UserId, x.Status });
        });

        builder.Entity<ShoppingListItem>(item =>
        {
            item.HasKey(x => x.Id);
            item.Property(x => x.Name).HasMaxLength(200).IsRequired();
            item.Property(x => x.Quantity).HasPrecision(10, 2);
            item.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Conversation>(conversation =>
        {
            conversation.HasKey(x => x.Id);
            conversation.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            conversation.Property(x => x.Title).HasMaxLength(120).IsRequired();
            conversation.HasMany(x => x.Messages).WithOne().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            conversation.HasIndex(x => new { x.UserId, x.UpdatedAtUtc });
        });

        builder.Entity<ConversationMessage>(message =>
        {
            message.HasKey(x => x.Id);
            message.Property(x => x.Content).HasMaxLength(4000).IsRequired();
            message.HasIndex(x => new { x.ConversationId, x.CreatedAtUtc });
        });

        builder.Entity<AssistantActionDraft>(draft =>
        {
            draft.HasKey(x => x.Id);
            draft.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            draft.Property(x => x.PayloadJson).HasMaxLength(12000).IsRequired();
            draft.HasIndex(x => new { x.UserId, x.Status, x.ExpiresAtUtc });
        });

        builder.Entity<AiToolExecution>(execution =>
        {
            execution.HasKey(x => x.Id);
            execution.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            execution.Property(x => x.ToolName).HasMaxLength(80).IsRequired();
            execution.Property(x => x.ErrorCode).HasMaxLength(80);
            execution.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        });

        builder.Entity<AiConfiguration>(config =>
        {
            config.HasKey(x => x.Id);
            config.Property(x => x.ApiBaseUrl).HasMaxLength(500).IsRequired();
            config.Property(x => x.Model).HasMaxLength(120).IsRequired();
            config.Property(x => x.EncryptedApiKey).HasMaxLength(4000).IsRequired();
        });
    }
}
