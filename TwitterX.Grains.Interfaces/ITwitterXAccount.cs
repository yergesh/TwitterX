using System.Collections.Immutable;
using TwitterX.Grains.Interfaces.Models;

namespace TwitterX.Grains.Interfaces;
public interface ITwitterXAccount : ITwitterXPublisher, ITwitterXSubscriber
{
    ValueTask FollowUserAsync(string userNameToFollow);
    ValueTask UnfollowUserAsync(string userNameToUnfollow);
    ValueTask<ImmutableList<string>> GetFollowingListAsync();
    ValueTask<ImmutableList<string>> GetFollowersListAsync();
    ValueTask PublishPostAsync(string chirpMessage);
    ValueTask<ImmutableList<Guid>> GetReceivedPostsAsync(int n = 10, int start = 0);
}
