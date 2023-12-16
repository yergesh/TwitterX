using TwitterX.Grains.Interfaces.Models;

namespace TwitterX.Grains.Interfaces;
public interface ITwitterXSubscriber : IGrainWithStringKey
{
    Task NewChirpAsync(Post chirp);
}
