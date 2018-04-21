using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.StashApi.DataTypes;

namespace PoeShared.StashApi
{
    public interface IPoeStashClient : IPoeLeagueApiClient
    {
        string AccountName { [NotNull] get; }

        string Email { get; }

        string SessionId { get; }

        bool IsAuthenticated { get; }

        void Authenticate();
        
        Task AuthenticateAsync();
        
        [NotNull] 
        ICharacter[] GetCharacters();

        [NotNull] 
        Task<ICharacter[]> GetCharactersAsync();

        [NotNull] 
        IInventory GetInventory(string characterName);
        
        [NotNull] 
        Task<IInventory> GetInventoryAsync(string characterName);

        [NotNull] 
        IStash GetStash(int index, string league);
        
        [NotNull] 
        Task<IStash> GetStashAsync(int stashIdx, string league);
    }
}