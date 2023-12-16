using TwitterX.Grains.Interfaces;
using TwitterX.Grains.Interfaces.Models;

namespace TwitterX.Grains;

[GenerateSerializer]
public record class TwitterXAccountState
{
    [Id(0)]
    public Dictionary<string, ITwitterXPublisher> Subscriptions { get; init; } = [];

    [Id(1)]
    public Dictionary<string, ITwitterXSubscriber> Followers { get; init; } = [];

    [Id(2)]
    public Queue<Guid> RecentReceivedPosts { get; init; } = new();

    [Id(3)]
    public Queue<Guid> MyPublishedPosts { get; init; } = new();
}
