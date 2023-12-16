using System.Collections.Immutable;
using TwitterX.Grains.Interfaces.Models;

namespace TwitterX.Grains.Interfaces;
public interface ITwitterXPost : IGrainWithGuidKey
{
    ValueTask CreateNewPostAsync(Post post);
    ValueTask<Post> GetPostAsync();
    ValueTask LikePostAsync(Guid postId, string liker);
    ValueTask UnlikePostAsync(Guid postId, string unliker);
    ValueTask<ImmutableList<string>> GetPostLikesAsync();
    ValueTask CommentOnPostAsync(string user, Guid postId, string comment);
    ValueTask DeleteCommentOnPostAsync(Guid commentId);
    ValueTask<ImmutableList<Comment>> GetPostCommentsAsync();
}
