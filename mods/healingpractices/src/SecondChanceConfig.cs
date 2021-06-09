using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace healingpractices.src
{
    public class SecondChanceConfig
    {
        public static SecondChanceConfig Current { get; set; }
        public int MaxLifes { get; set; }
        public int HealWoundTime { get; set; }

        public SecondChanceConfig()
        {
            MaxLifes =  2;
            HealWoundTime = 3;
        }
    }
}
