using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    public class BlockchainTransaction
    {
        public string Hash { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Value { get; set; } 
        public int Gas { get; set; }
        public long GasPrice { get; set; }
        public int V { get; set; }
        public BigInteger R { get; set; } 
        public BigInteger S { get; set; } 
        public int Nonce { get; set; }
        public string Input { get; set; }

    }
}
