using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using Orleans.Runtime;
using System.Collections.Immutable;
using TwitterX.Grains.Interfaces;
using TwitterX.Grains.Interfaces.Models;

namespace TwitterX.Grains;

[Reentrant]
public sealed class TwitterXPost(
   [PersistentState(stateName: "post", storageName: "PostState")] IPersistentState<TwitterXPostState> state,
   ILogger<TwitterXPost> logger) : Grain, ITwitterXPost
{

    private readonly ILogger<TwitterXPost> _logger = logger;
    private readonly IPersistentState<TwitterXPostState> _state = state;

    private Task? _outstandingWriteStateOperation;

    private static string GrainType => nameof(TwitterXAccount);
    private string GrainKey => this.GetPrimaryKeyString();

    public override Task OnActivateAsync(CancellationToken _)
    {
        _logger.LogInformation("{GrainType} {GrainKey} activated.", GrainType, GrainKey);

        return Task.CompletedTask;
    }

    public async ValueTask LikePostAsync(Guid postId, string liker)
    {
        _logger.LogInformation("{GrainType} {GrainKey} liking post {PostId} by {Liker}.",
            GrainType, GrainKey, postId, liker);

        _state.State.Likes.Add(liker);
        await WriteStateAsync();
    }

    public async ValueTask UnlikePostAsync(Guid postId, string unliker)
    {
        _logger.LogInformation("{GrainType} {GrainKey} unliking post {PostId} by {Unliker}.",
            GrainType, GrainKey, postId, unliker);

        _state.State.Likes.Remove(unliker);
        await WriteStateAsync();

    }

    public async ValueTask CommentOnPostAsync(string user, Guid postId, string commentText)
    {
        _logger.LogInformation("{GrainType} {GrainKey} commenting on post {PostId}.",
            GrainType, GrainKey, postId);

        var comment = new Comment(commentText, user, DateTimeOffset.UtcNow);
        _state.State.Comments.Add(comment);
        await WriteStateAsync();
    }

    public async ValueTask DeleteCommentOnPostAsync(Guid commentId)
    {
        _state.State.Comments.Remove(_state.State.Comments.FirstOrDefault(s => s.Id == commentId)!);
        await WriteStateAsync();
    }

    public ValueTask<ImmutableList<Comment>> GetPostCommentsAsync()
    {
        var r = new List<Comment>();
        foreach (var item in _state.State.Comments)
        {
            r.Add(new Comment(item.CommentText, item.Author, item.Timestamp) { Id = item.Id });
        }
        return ValueTask.FromResult(r.ToImmutableList());
    }

    public ValueTask<ImmutableList<string>> GetPostLikesAsync()
    {
        return ValueTask.FromResult(_state.State.Likes.ToImmutableList());
    }


    public async ValueTask CreateNewPostAsync(Post post)
    {
        _state.State.Content = post.Content;
        _state.State.Tags = post.Tags;
        _state.State.Timestamp = post.Timestamp;
        _state.State.Author = post.Author;
        await WriteStateAsync();
    }

    public ValueTask<Post> GetPostAsync()
    {
        return ValueTask.FromResult(new Post(_state.State.Content, _state.State.Timestamp, _state.State.Author));
    }

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
