using JetBrains.Annotations;

namespace PoeBud.Models
{
    public interface IStashUpdaterParameters
    {
        string LoginEmail { [CanBeNull] get; }
        
        string SessionId { [CanBeNull] get; }
        
        string LeagueId { [CanBeNull] get; }

        string[] StashesToProcess { [CanBeNull] get; }
    }   
}