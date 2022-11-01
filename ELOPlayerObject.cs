using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ELO
{
    class ELOPlayerObject
    {
        public int Id;
        public string Name;
        public int ELO;
        public int Games;
        public int Wins;
        public int Losses;
        public int Top2;
        public int Top3;
        public int Avgvp;
        public int Position;
        public int VictoryPoints;

        public ELOPlayerObject (string name, int position, int victorypoints)
        {
            Name = name;
            Position = position;
            VictoryPoints = victorypoints;
        }
    }
}
