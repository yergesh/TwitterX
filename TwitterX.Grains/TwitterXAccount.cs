using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using Orleans.Runtime;
using System.Collections.Immutable;
using TwitterX.Grains.Interfaces;
using TwitterX.Grains.Interfaces.Models;

namespace TwitterX.Grains;

[Reentrant]
public sealed class TwitterXAccount(
   [PersistentState(stateName: "account", storageName: "AccountState")] IPersistentState<TwitterXAccountState> state,
   ILogger<TwitterXAccount> logger) : Grain, ITwitterXAccount
{

    private const int PostCacheSize = 100;

    private const int MaxChirpLength = 280;

    private readonly ILogger<TwitterXAccount> _logger = logger;
    private readonly IPersistentState<TwitterXAccountState> _state = state;

    private Task? _outstandingWriteStateOperation;

    private static string GrainType => nameof(TwitterXAccount);
    private string GrainKey => this.GetPrimaryKeyString();

    public override Task OnActivateAsync(CancellationToken _)
    {
        _logger.LogInformation("{GrainType} {GrainKey} activated.", GrainType, GrainKey);

        return Task.CompletedTask;
    }

    public async ValueTask PublishPostAsync(string message)
    {
        var chirp = CreateNewChirpMessage(message);

        _logger.LogInformation("{GrainType} {GrainKey} publishing new chirp message '{Chirp}'.",
            GrainType, GrainKey, chirp);

        _state.State.MyPublishedPosts.Enqueue(chirp.Id);

        while (_state.State.MyPublishedPosts.Count > PostCacheSize)
        {
            _state.State.MyPublishedPosts.Dequeue();
        }

        await WriteStateAsync();

        // notify followers of a new message
        _logger.LogInformation("{GrainType} {GrainKey} sending new chirp message to {FollowerCount} followers.",
            GrainType, GrainKey, _state.State.Followers.Count);

        await Task.WhenAll(_state.State.Followers.Values.Select(_ => _.NewChirpAsync(chirp)).ToArray());
    }

    public ValueTask<ImmutableList<Guid>> GetReceivedPostsAsync(int number, int start)
    {
        if (start < 0) start = 0;
        if (start + number > _state.State.RecentReceivedPosts.Count)
        {
            number = _state.State.RecentReceivedPosts.Count - start;
        }

        return ValueTask.FromResult(
            _state.State.RecentReceivedPosts
                .Skip(start)
                .Take(number)
                .ToImmutableList());
    }

    public async ValueTask FollowUserAsync(string username)
    {
        _logger.LogInformation(
            "{GrainType} {UserName} > FollowUserName({TargetUserName}).",
            GrainType,
            GrainKey,
            username);

        var userToFollow = GrainFactory.GetGrain<ITwitterXPublisher>(username);

        await userToFollow.AddFollowerAsync(GrainKey, this.AsReference<ITwitterXSubscriber>());

        _state.State.Subscriptions[username] = userToFollow;

        await WriteStateAsync();
    }

    public async ValueTask UnfollowUserAsync(string username)
    {
        _logger.LogInformation(
            "{GrainType} {GrainKey} > UnfollowUserName({TargetUserName}).",
            GrainType,
            GrainKey,
            username);

        await GrainFactory.GetGrain<ITwitterXPublisher>(username)
            .RemoveFollowerAsync(GrainKey);

        _state.State.Subscriptions.Remove(username);

        await WriteStateAsync();
    }

    public ValueTask<ImmutableList<string>> GetFollowingListAsync() =>
        ValueTask.FromResult(_state.State.Subscriptions.Keys.ToImmutableList());

    public ValueTask<ImmutableList<string>> GetFollowersListAsync() =>
        ValueTask.FromResult(_state.State.Followers.Keys.ToImmutableList());

    public ValueTask<ImmutableList<Guid>> GetPublishedPostsAsync(int number, int start)
    {
        if (start < 0) start = 0;
        if (start + number > _state.State.MyPublishedPosts.Count)
        {
            number = _state.State.MyPublishedPosts.Count - start;
        }
        return ValueTask.FromResult(
            _state.State.MyPublishedPosts
                .Skip(start)
                .Take(number)
                .ToImmutableList());
    }

    public async ValueTask AddFollowerAsync(string username, ITwitterXSubscriber follower)
    {
        _state.State.Followers[username] = follower;
        await WriteStateAsync();
    }

    public ValueTask RemoveFollowerAsync(string username)
    {
        _state.State.Followers.Remove(username);
        return WriteStateAsync();
    }

    public async Task NewChirpAsync(Post chirp)
    {
        _logger.LogInformation(
            "{GrainType} {GrainKey} received chirp message = {Chirp}",
            GrainType,
            GrainKey,
            chirp);

        _state.State.RecentReceivedPosts.Enqueue(chirp.Id);

        while (_state.State.RecentReceivedPosts.Count > PostCacheSize)
        {
            _state.State.RecentReceivedPosts.Dequeue();
        }

        await WriteStateAsync();
    }

    private Post CreateNewChirpMessage(string message) =>
        new(message, DateTimeOffset.UtcNow, GrainKey);

    private async ValueTask WriteStateAsync()
    {
        if (_outstandingWriteStateOperation is Task currentWriteStateOperation)
        {
            try
            {
                await currentWriteStateOperation;
            }
            catch { }
            finally
            {
                if (_outstandingWriteStateOperation == currentWriteStateOperation)
                {
                    _outstandingWriteStateOperation = null;
                }
            }
        }

        if (_outstandingWriteStateOperation is null)
        {
            currentWriteStateOperation = _state.WriteStateAsync();
            _outstandingWriteStateOperation = currentWriteStateOperation;
        }
        else
        {
            currentWriteStateOperation = _outstandingWriteStateOperation;
        }

        try
        {
            await currentWriteStateOperation;
        }
        finally
        {
            if (_outstandingWriteStateOperation == currentWriteStateOperation)
            {
                _outstandingWriteStateOperation = null;
            }
        }
    }
}
