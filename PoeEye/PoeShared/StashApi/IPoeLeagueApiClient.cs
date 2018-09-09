using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.StashApi.DataTypes;

namespace PoeShared.StashApi
{
    public interface IPoeLeagueApiClient
    {
        [NotNull]
        Task<ILeague[]> GetLeaguesAsync();
    }
}