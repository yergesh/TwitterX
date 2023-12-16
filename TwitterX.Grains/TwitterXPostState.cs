using TwitterX.Grains.Interfaces.Models;

namespace TwitterX.Grains;

[GenerateSerializer]
public record class TwitterXPostState
{
    [Id(1)]
    public string Content { get; set; }

    [Id(2)]
    public List<string> Tags { get; set; }

    [Id(3)]
    public DateTimeOffset Timestamp { get; set; }

    [Id(4)]
    public string Author { get; set; }

    [Id(5)]
    public HashSet<string> Likes { get; set; } = [];

    [Id(6)]
    public List<Comment> Comments { get; set; } = [];
}
