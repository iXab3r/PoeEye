using JetBrains.Annotations;
using PoeBud.OfficialApi.DataTypes;

namespace PoeBud.OfficialApi
{
    internal interface IPoeClient
    {
        string AccountName { [NotNull] get; }

        string Email { get; }

        string SessionId { get; }

        bool IsAuthenticated { get; }

        void Authenticate();

        [NotNull] 
        ICharacter[] GetCharacters();

        [NotNull] 
        IInventory GetInventory(string characterName);

        [NotNull] 
        IStash GetStash(int index, string league);

        [NotNull] 
        ILeague[] GetLeagues();
    }
}