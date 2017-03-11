using System;

namespace PoeShared.StashApi.DataTypes
{
    public interface ILeague
    {
        string Description { get; set; }

        DateTime EndAt { get; set; }

        string Id { get; set; }

        DateTime StartAt { get; set; }
    }
}