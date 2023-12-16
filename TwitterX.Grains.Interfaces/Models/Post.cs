using System.Text.RegularExpressions;

namespace TwitterX.Grains.Interfaces.Models;

[GenerateSerializer]
public sealed record Post(
    string Content,
    DateTimeOffset Timestamp,
    string Author)
{
    [Id(0)]
    public Guid Id { get; init; } = Guid.NewGuid();
    public List<string> Tags => RecognizeTags();

    private List<string> RecognizeTags()
    {
        string hashtagPattern = @"#\w+";
        MatchCollection matches = Regex.Matches(Content, hashtagPattern);
        List<string> hashtags = [];
        hashtags.AddRange(from Match match in matches
                          select match.Value);
        return hashtags;
    }
}

[GenerateSerializer]
public sealed record Comment(string CommentText, string Author, DateTimeOffset Timestamp)
{
    [Id(0)]
    public Guid Id { get; init; } = Guid.NewGuid();
}


[GenerateSerializer] public sealed record PostResponse(Guid Id, string Content, DateTimeOffset Timestamp, string Author, int CommentCount, int LikeCount, bool IsLikedPost);

[GenerateSerializer]
public sealed record CommentResponse(Guid Id, string CommentText, string Author, DateTimeOffset Timestamp);