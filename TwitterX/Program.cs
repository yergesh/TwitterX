using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Orleans.Serialization.Invocation;
using System.Collections.Immutable;
using TwitterX.Client.Middleware;
using TwitterX.Client.Services;
using TwitterX.Grains.Interfaces;
using TwitterX.Grains.Interfaces.Models;
using static TwitterX.Client.Services.JwtServicesExt;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleansClient(builder => builder.UseLocalhostClustering());
builder.Services.AddTransient<IJwtService, JwtService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TwitterX MVP",
        Version = "v1"
    });

    // Add security definition for JWT Bearer authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });

    // Add security requirement for JWT Bearer authentication
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddOptions<JwtConfiguration>().BindConfiguration("Jwt").ValidateDataAnnotations().ValidateOnStart();
var jwtConfig = builder.Configuration.GetSection("Jwt").Get<JwtConfiguration>();
builder.Services.AddAuthenticationJwtBearer(jwtConfig);
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RefreshTokenMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/auth", ([FromServices] IJwtService _jwtService, [FromBody] Mobile mobile) =>
{
    (string token, string expire) = _jwtService.GenerateToken(mobile.CountryCode, mobile.PhoneNumber);
    return token;
});

app.MapGet("/", () => "Hello World!").RequireAuthorization();

app.MapPost("/follow", async Task<Results<Ok, BadRequest<string>, UnauthorizedHttpResult>> (IClusterClient client, HttpContext httpContext, string username) =>
{
    if (httpContext.GetTwitterXClaimUser() is { } twitterXUser)
    {
        var follower = twitterXUser.Mobile;
        if (follower == username) return TypedResults.BadRequest($"You can't subscribe to yourself. User = {username}");
        var accountGrain = client.GetGrain<ITwitterXAccount>(follower);
        await accountGrain.FollowUserAsync(username);
        return TypedResults.Ok();
    }
    return TypedResults.Unauthorized();
}).RequireAuthorization();

app.MapPost("/unfollow", async Task<Results<Ok, UnauthorizedHttpResult>> (IClusterClient client, HttpContext httpContext, string username) =>
{
    if (httpContext.GetTwitterXClaimUser() is { } twitterXUser)
    {
        var follower = twitterXUser.Mobile;
        var accountGrain = client.GetGrain<ITwitterXAccount>(follower);
        await accountGrain.UnfollowUserAsync(username);
        return TypedResults.Ok();
    }
    return TypedResults.Unauthorized();
}).RequireAuthorization();

app.MapPost("/post", async Task<Results<Ok, UnauthorizedHttpResult>> (IClusterClient client, HttpContext httpContext, string message) =>
{
    if (httpContext.GetTwitterXClaimUser() is { } twitterXUser)
    {
        var username = twitterXUser.Mobile;
        var bloggerAccount = client.GetGrain<ITwitterXAccount>(username);
        await bloggerAccount.PublishPostAsync(message);
        return TypedResults.Ok();
    }
    return TypedResults.Unauthorized();
}).RequireAuthorization();

app.MapGet("/posts/{number}/{start}", async Task<Results<Ok<ImmutableList<PostResponse>>, UnauthorizedHttpResult>> (IClusterClient client, HttpContext httpContext, int number = 10, int start = 0) =>
{
    if (httpContext.GetTwitterXClaimUser() is { } twitterXUser)
    {
        var uid = twitterXUser.Mobile;
        var accountGrain = client.GetGrain<ITwitterXAccount>(uid);
        IImmutableList<Guid> posts = await accountGrain.GetPublishedPostsAsync(number, start);
        var result = new List<PostResponse>();
        foreach (var postId in posts)
        {
            var post = await client.GetGrain<ITwitterXPost>(postId).GetPostAsync();
            var comments = await client.GetGrain<ITwitterXPost>(postId).GetPostCommentsAsync();
            var likes = await client.GetGrain<ITwitterXPost>(postId).GetPostLikesAsync();

            var temp = new PostResponse(
                Id: postId,
                Content: post.Content,
                Timestamp: post.Timestamp,
                Author: post.Author,
                CommentCount: comments is not null ? comments.Count : 0,
                LikeCount: likes is not null ? likes.Count : 0,
                IsLikedPost: likes is not null && likes.Contains(uid));
            result.Add(temp);
        }
        return TypedResults.Ok(result.ToImmutableList());
    }
    return TypedResults.Unauthorized();
});

app.MapPost("/like", async Task<Results<Ok, UnauthorizedHttpResult>> (IClusterClient client, HttpContext httpContext, Guid postId) =>
{
    if (httpContext.GetTwitterXClaimUser() is { } twitterXUser)
    {
        var username = twitterXUser.Mobile;
        var bloggerAccount = client.GetGrain<ITwitterXPost>(postId);
        await bloggerAccount.LikePostAsync(postId, username);
        return TypedResults.Ok();
    }
    return TypedResults.Unauthorized();
}).RequireAuthorization();

app.MapPost("/unlike", async Task<Results<Ok, UnauthorizedHttpResult>> (IClusterClient client, HttpContext httpContext, Guid postId) =>
{
    if (httpContext.GetTwitterXClaimUser() is { } twitterXUser)
    {
        var username = twitterXUser.Mobile;
        var bloggerAccount = client.GetGrain<ITwitterXPost>(postId);
        await bloggerAccount.UnlikePostAsync(postId, username);
        return TypedResults.Ok();
    }
    return TypedResults.Unauthorized();
}).RequireAuthorization();

app.MapGet("/post-likes/{postId}", async Task<Results<Ok<ImmutableList<string>>, UnauthorizedHttpResult>> (IClusterClient client, HttpContext httpContext, Guid postId) =>
{
    if (httpContext.GetTwitterXClaimUser() is { } twitterXUser)
    {
        var bloggerAccount = client.GetGrain<ITwitterXPost>(postId);
        var lst = await bloggerAccount.GetPostLikesAsync();
        return TypedResults.Ok(lst);
    }
    return TypedResults.Unauthorized();
});

app.MapPost("/comment", async Task<Results<Ok, UnauthorizedHttpResult>> (IClusterClient client, HttpContext httpContext, Guid postId, string comment) =>
{
    if (httpContext.GetTwitterXClaimUser() is { } twitterXUser)
    {
        var username = twitterXUser.Mobile;
        var bloggerAccount = client.GetGrain<ITwitterXPost>(postId);
        await bloggerAccount.CommentOnPostAsync(username, postId, comment);
        return TypedResults.Ok();
    }
    return TypedResults.Unauthorized();
}).RequireAuthorization();

app.MapGet("/post-comments/{postId}", async Task<Results<Ok<ImmutableList<CommentResponse>>, UnauthorizedHttpResult>> (IClusterClient client, HttpContext httpContext, Guid postId) =>
{
    if (httpContext.GetTwitterXClaimUser() is { } twitterXUser)
    {
        var postGrain = client.GetGrain<ITwitterXPost>(postId);
        var comments = await postGrain.GetPostCommentsAsync();
        var result = new List<CommentResponse>();
        foreach (var comment in comments)
        {
            result.Add(new CommentResponse(comment.Id, comment.CommentText, comment.Author, comment.Timestamp));
        }
        return TypedResults.Ok(result.ToImmutableList());
    }
    return TypedResults.Unauthorized();
});

app.Run();

sealed record Mobile(string CountryCode, string PhoneNumber);
