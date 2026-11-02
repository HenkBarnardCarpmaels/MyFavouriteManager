using System.Text.Json;
using System.Text.Json.Serialization;

namespace henkbarnardcarpmaels_psychic_engine.Features.Training;

using henkbarnardcarpmaels_psychic_engine.Data;

public sealed class TrainingStore(IHostEnvironment environment, ILogger<TrainingStore> logger)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private string DataDirectory => Path.Combine(environment.ContentRootPath, "App_Data");
    private string LinksPath => Path.Combine(DataDirectory, "training.json");
    private string CategoriesPath => Path.Combine(DataDirectory, "categories.json");

    public async Task<TrainingData> ReadAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var (data, _) = await ReadCoreAsync(cancellationToken);
            return data;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task MutateAsync(Action<TrainingData> mutation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        await gate.WaitAsync(cancellationToken);
        try
        {
            var (data, categories) = await ReadCoreAsync(cancellationToken);
            mutation(data);
            data.Version++;
            await WriteCoreAsync(data, categories, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<CategoryData> ReadCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var (_, categories) = await ReadCoreAsync(cancellationToken);
            return categories;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task MutateCategoriesAsync(Action<CategoryData> mutation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        await gate.WaitAsync(cancellationToken);
        try
        {
            var (data, categories) = await ReadCoreAsync(cancellationToken);
            mutation(categories);
            categories.Version++;
            await WriteCoreAsync(data, categories, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<(TrainingData Data, CategoryData Categories)> ReadCoreAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(DataDirectory);
        var data = await ReadJsonAsync<TrainingData>(LinksPath, cancellationToken) ?? new TrainingData();
        var categories = await ReadJsonAsync<CategoryData>(CategoriesPath, cancellationToken);

        if (categories is null)
        {
            categories = new CategoryData();
            if (data.Links.Count == 0)
            {
                var seed = CreateSeedData();
                categories.Categories = seed.Categories.Categories;
                data.Links = seed.Data.Links;
            }
            else
            {
                // Migrate the original combined training.json format once.
                var legacy = await ReadJsonAsync<LegacyTrainingData>(LinksPath, cancellationToken);
                if (legacy?.Categories is { Count: > 0 })
                {
                    categories.Categories = legacy.Categories;
                }
            }
            await WriteCoreAsync(data, categories, cancellationToken);
        }

        data.Categories = categories.Categories;
        return (data, categories);
    }

    private async Task WriteCoreAsync(TrainingData data, CategoryData categories, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(DataDirectory);
        await WriteJsonAtomicallyAsync(LinksPath, data, cancellationToken);
        await WriteJsonAtomicallyAsync(CategoriesPath, categories, cancellationToken);
        logger.LogDebug("Training data saved at link version {LinkVersion}, category version {CategoryVersion}", data.Version, categories.Version);
    }

    private async Task<T?> ReadJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return await JsonSerializer.DeserializeAsync<T>(stream, jsonOptions, cancellationToken);
    }

    private async Task WriteJsonAtomicallyAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        var temporaryPath = path + ".tmp";
        await using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        {
            await JsonSerializer.SerializeAsync(stream, value, jsonOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        File.Move(temporaryPath, path, overwrite: true);
    }

    private static (TrainingData Data, CategoryData Categories) CreateSeedData()
    {
        var foundations = new TrainingCategory
        {
            Name = "Foundations",
            Description = "Build the fundamentals that make everything else easier.",
            Subcategories =
            [
                new TrainingSubcategory { Name = "C# & .NET" },
                new TrainingSubcategory { Name = "Architecture" }
            ]
        };
        var frontend = new TrainingCategory
        {
            Name = "Frontend craft",
            Description = "Practical patterns for building interfaces people enjoy using.",
            Subcategories =
            [
                new TrainingSubcategory { Name = "Blazor" },
                new TrainingSubcategory { Name = "Design systems" }
            ]
        };
        var now = DateTimeOffset.UtcNow;
        return (new TrainingData
        {
            Links =
            [
                new TrainingLink
                {
                    Title = "ASP.NET Core fundamentals",
                    Url = "https://learn.microsoft.com/aspnet/core/",
                    Notes = "The official starting point for modern .NET web development.",
                    CategoryId = foundations.Id,
                    SubcategoryId = foundations.Subcategories[0].Id,
                    Priority = 1,
                    Type = TrainingLinkType.Web,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new TrainingLink
                {
                    Title = "Blazor in .NET 10",
                    Url = "https://www.youtube.com/watch?v=YwZdtLEtROA",
                    Notes = "A quick tour of the current Blazor Web App model.",
                    CategoryId = frontend.Id,
                    SubcategoryId = frontend.Subcategories[0].Id,
                    Priority = 2,
                    Type = TrainingLinkType.YouTube,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            ]
        }, new CategoryData { Categories = [foundations, frontend] });
    }

    private sealed class LegacyTrainingData
    {
        public int Version { get; set; } = 1;
        public List<TrainingLink> Links { get; set; } = [];
        public List<TrainingCategory> Categories { get; set; } = [];
    }
}
