using System;

namespace PoeBud.OfficialApi.DataTypes
{
    internal interface ITab
    {
        Colour colour { get; set; }

        bool hidden { get; set; }

        int Idx { get; set; }

        string StashTypeName { get; set; }

        string Name { get; set; }

        string srcC { get; set; }

        string srcL { get; set; }

        string srcR { get; set; }
    }
}