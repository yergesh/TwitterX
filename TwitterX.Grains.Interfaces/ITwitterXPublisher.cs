using System.Collections.Immutable;
using TwitterX.Grains.Interfaces.Models;

namespace TwitterX.Grains.Interfaces;

public interface ITwitterXPublisher : IGrainWithStringKey
{
    ValueTask<ImmutableList<Guid>> GetPublishedPostsAsync(int n = 10, int start = 0);
    ValueTask AddFollowerAsync(string userName, ITwitterXSubscriber subscriber);
    ValueTask RemoveFollowerAsync(string userName);
}
