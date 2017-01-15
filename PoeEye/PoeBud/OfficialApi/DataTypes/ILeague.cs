using System;

namespace PoeBud.OfficialApi.DataTypes
{
    public interface ILeague
    {
        string Description { get; set; }

        DateTime EndAt { get; set; }

        string Id { get; set; }

        DateTime StartAt { get; set; }
    }
}