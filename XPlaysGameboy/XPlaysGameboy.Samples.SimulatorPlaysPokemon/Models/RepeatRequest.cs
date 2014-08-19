using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XPlaysGameboy.Samples.SimulatorPlaysPokemon.Models
{
    class RepeatRequest
    {
        public string RequestAuthor { get; set; }
        public int Amount { get; set; }
        public int CommandIndex { get; set; }
    }
}
