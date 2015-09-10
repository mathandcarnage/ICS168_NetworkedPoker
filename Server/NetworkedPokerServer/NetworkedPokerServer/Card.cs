using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkedPokerServer
{
    class Card
    {
        string[] suitChars = { "♠", "♣", "♥", "♦" };
        public int value;
        public int suit;
        public int cardID;

        public Card(int n)
        {
            suit = n & 3;
            value = n >> 2;
            cardID = n;
        }
        string getValueString(int v)
        {
            if (v <= 8)
            {
                return "" + (value + 2);
            }
            else if (v == 9)
            {
                return "J";
            }
            else if (v == 10)
            {
                return "Q";
            }
            else if (v == 11)
            {
                return "K";
            }
            else
            {
                return "A";
            }
        }

        public string print()
        {
            return getValueString(value) + suitChars[suit];
        }
    }
}
