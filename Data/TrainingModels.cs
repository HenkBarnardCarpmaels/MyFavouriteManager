using System.Text.Json.Serialization;

namespace henkbarnardcarpmaels_psychic_engine.Data;

public sealed class TrainingData
{
    public int Version { get; set; } = 1;
    [JsonIgnore]
    public List<TrainingCategory> Categories { get; set; } = [];
    public List<TrainingLink> Links { get; set; } = [];
}

public sealed class CategoryData
{
    public int Version { get; set; } = 1;
    public List<TrainingCategory> Categories { get; set; } = [];
}

public sealed class TrainingCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<TrainingSubcategory> Subcategories { get; set; } = [];
}

public sealed class TrainingSubcategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
}

public enum TrainingLinkType
{
    Web,
    YouTube
}

public sealed class TrainingLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Guid? SubcategoryId { get; set; }
    public TrainingLinkType Type { get; set; } = TrainingLinkType.Web;
    public int Priority { get; set; } = 3;
    public bool IsSeen { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
