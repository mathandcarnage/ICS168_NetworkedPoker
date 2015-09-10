using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace NetworkedPokerServer
{
    class GameState
    {
        int state;
        Deck deck;
        Player[] players;
        Card[] dealtCards;
        int currentPlayer;

        public GameState()
        {
            state = 0;
            deck = new Deck();
            players = new Player[] { null, null, null, null };
            dealtCards = new Card[] { null, null, null, null, null };
            currentPlayer = -1;
        }

        private void Broadcast(string data)
        {
            for(int i = 0; i < 4; i ++)
            {
                if(players[i] != null)
                {
                    ServerSocket.Send(players[i].mySocket, data);
                }
            }
        }

        private string printCardsInfo()
        {
            string res = "CardsInfo\n";
            for (int i = 0; i < 5; i ++)
            {
                if(dealtCards[i] == null)
                {
                    res += "-1\n";
                }
                else
                {
                    res += dealtCards[i].cardID + "\n";
                }
            }
            return res + "<EOF>";
        }

        private string printPotInfo()
        {
            int pot = 0;
            int max = 0;
            for(int i = 0; i < 4; i ++)
            {
                if (players[i] != null)
                {
                    pot += players[i].betSoFar;
                    max = Math.Max(max, players[i].betSoFar);
                }
            }
            return "PotInfo\n" + pot + "\n" + max + "\n<EOF>";
        }

        public void Fold(int ix)
        {

        }

        public void Call(int ix)
        {

        }

        public void Raise(int ix, int amt)
        {

        }

        public int Join(Socket s, string n, int c)
        {
            for(int i = 0; i < 4; i ++)
            {
                if(players[i] == null)
                {
                    players[i] = new Player(s, n, c);
                    for (int j = 0; j < 4; j ++)
                    {
                        if(players[j] != null)ServerSocket.Send(s, players[j].printInfo(j));
                    }
                    ServerSocket.Send(s, printCardsInfo());
                    ServerSocket.Send(s, printPotInfo());
                    ServerSocket.Send(s, players[i].printChips());
                    Broadcast(players[i].printInfo(i));
                    return i;
                }
            }
            return -1;
        }
    }

    class Player
    {
        public Socket mySocket;
        public string name;
        public int chips;
        public string status;
        public Card card1;
        public Card card2;
        public int betSoFar;
        public bool isFolded;
        public bool hasActed;
        public int inPot;

        public Player(Socket s, string n, int c)
        {
            mySocket = s;
            name = n;
            chips = c;
            status = "New Player!";
            card1 = null;
            card2 = null;
            betSoFar = 0;
            isFolded = true;
            hasActed = false;
            inPot = 0;
        }

        public string printInfo(int ix)
        {
            String res = "PlayerInfo\n" + ix + "\n" + name + "\n" + chips + "\n" + status + "\n";
            if(card1 == null)
            {
                res += "-1\n-1\n<EOF>";
            }
            else
            {
                res += card1.cardID + "\n" + card2.cardID + "\n<EOF>";
            }
            return res;
        }

        public string printChips()
        {
            return "ChipInfo\n" + chips + "\n<EOF>";
        }

        public string printCall(int max)
        {
            int leftover = max - betSoFar;
            return "CallInfo\n" + leftover + "\n<EOF>";
        }

        public string printHand()
        {
            String res = "HandInfo\n";
            if (card1 == null)
            {
                res += "-1\n-1\n<EOF>";
            }
            else
            {
                res += card1.cardID + "\n" + card2.cardID + "\n<EOF>";
            }
            return res;
        }
    }
    class Deck
    {
        int[] cards;
        int currIndex;
        Random rand;

        public Deck()
        {
            cards = new int[52];
            rand = new Random();
            shuffle();
        }

        public void shuffle()
        {
            currIndex = 0;
            int i = 0;

            for(int s = 0; s < 4; s ++)
            {
                for(int v = 0; v < 13; v ++)
                {
                    int c = (v << 2) + s;
                    int r = rand.Next(i + 1);
                    cards[i] = cards[r];
                    cards[r] = c;
                    i++;
                }
            }
        }

        public Card drawNext()
        {
            int n = cards[currIndex];
            currIndex++;
            return new Card(n);
        }
    }

    class Hand
    {
        Card[] cards;
        string handType;
        int[] testVals;

        public Hand(Card[] c)
        {
            int pair = 0;
            int consec = 0;
            int suit = 15;
            cards = new Card[5];
            int[] valueCounts = new int[13];
            for (int i = 0; i < 5; i++)
            {
                suit = suit & (1 << c[i].suit);
                valueCounts[c[i].value]++;
                for (int j = 0; j < i; j++)
                {
                    if (c[i].value == c[j].value) pair++;
                    if (Math.Abs(c[i].value - c[j].value) == 1)
                    {
                        if (c[i].value == 12 || c[j].value == 12)
                        {
                            consec++;
                        }
                        else
                        {
                            consec += 2;
                        }
                    }
                    if (Math.Abs(c[i].value - c[j].value) == 12) consec++;
                }
            }
            int cix = 0;
            if (pair == 1)
            {
                handType = "One Pair";
                testVals = new int[5];
                testVals[0] = 1;
                for (int i = 12; i >= 0; i--)
                {
                    if (valueCounts[i] == 2)
                    {
                        testVals[1] = i;
                        for (int n = 0; n < 5; n++)
                        {
                            if (c[n].value == i)
                            {
                                cards[cix] = c[n];
                                cix++;
                            }
                        }
                        break;
                    }
                }
                int j = 2;
                for (int i = 12; i >= 0; i--)
                {
                    if (valueCounts[i] == 1)
                    {
                        testVals[j] = i;
                        for (int n = 0; n < 5; n++)
                        {
                            if (c[n].value == i)
                            {
                                cards[cix] = c[n];
                                cix++;
                            }
                        }
                        j++;
                    }
                }
            }
            else if (pair == 2)
            {
                handType = "Two Pair";
                testVals = new int[4];
                testVals[0] = 2;
                int j = 1;
                for (int i = 12; i >= 0; i--)
                {
                    if (valueCounts[i] == 2)
                    {
                        testVals[j] = i;
                        for (int n = 0; n < 5; n++)
                        {
                            if (c[n].value == i)
                            {
                                cards[cix] = c[n];
                                cix++;
                            }
                        }
                        j++;
                    }
                }
                for (int i = 12; i >= 0; i--)
                {
                    if (valueCounts[i] == 1)
                    {
                        testVals[j] = i;
                        for (int n = 0; n < 5; n++)
                        {
                            if (c[n].value == i)
                            {
                                cards[cix] = c[n];
                                cix++;
                            }
                        }
                        j++;
                    }
                }
            }
            else if (pair == 3)
            {
                handType = "Three of a Kind";
                testVals = new int[4];
                testVals[0] = 3;
                for (int i = 12; i >= 0; i--)
                {
                    if (valueCounts[i] == 3)
                    {
                        testVals[1] = i;
                        for (int n = 0; n < 5; n++)
                        {
                            if (c[n].value == i)
                            {
                                cards[cix] = c[n];
                                cix++;
                            }
                        }
                        break;
                    }
                }
                int j = 2;
                for (int i = 12; i >= 0; i--)
                {
                    if (valueCounts[i] == 1)
                    {
                        testVals[j] = i;
                        for (int n = 0; n < 5; n++)
                        {
                            if (c[n].value == i)
                            {
                                cards[cix] = c[n];
                                cix++;
                            }
                        }
                        j++;
                    }
                }
            }
            else if (pair == 4)
            {
                handType = "Full House";
                testVals = new int[3];
                testVals[0] = 6;
                for (int i = 12; i >= 0; i--)
                {
                    if (valueCounts[i] == 3)
                    {
                        testVals[1] = i;
                        for (int n = 0; n < 5; n++)
                        {
                            if (c[n].value == i)
                            {
                                cards[cix] = c[n];
                                cix++;
                            }
                        }
                        break;
                    }
                }
                for (int i = 12; i >= 0; i--)
                {
                    if (valueCounts[i] == 2)
                    {
                        testVals[2] = i;
                        for (int n = 0; n < 5; n++)
                        {
                            if (c[n].value == i)
                            {
                                cards[cix] = c[n];
                                cix++;
                            }
                        }
                        break;
                    }
                }
            }
            else if (pair == 6)
            {
                handType = "Four of a Kind";
                testVals = new int[3];
                testVals[0] = 7;
                for (int i = 12; i >= 0; i--)
                {
                    if (valueCounts[i] == 4)
                    {
                        testVals[1] = i;
                        for (int n = 0; n < 5; n++)
                        {
                            if (c[n].value == i)
                            {
                                cards[cix] = c[n];
                                cix++;
                            }
                        }
                        break;
                    }
                }
                for (int i = 12; i >= 0; i--)
                {
                    if (valueCounts[i] == 1)
                    {
                        testVals[2] = i;
                        for (int n = 0; n < 5; n++)
                        {
                            if (c[n].value == i)
                            {
                                cards[cix] = c[n];
                                cix++;
                            }
                        }
                        break;
                    }
                }
            }
            else if (consec >= 7 && suit != 0)
            {
                handType = "Straight Flush";
                testVals = new int[2];
                testVals[0] = 8;
                for (int i = 12; i >= 0; i--)
                {
                    if (valueCounts[i] == 1)
                    {
                        testVals[1] = i;
                        for (int n = 0; n < 5; n++)
                        {
                            if (c[n].value == i)
                            {
                                cards[cix] = c[n];
                                cix++;
                            }
                        }
                        if (i == 12 && testVals[11] == 0) testVals[1] = 5;
                    }
                }
            }
            else if (consec >= 7)
            {
                handType = "Straight";
                testVals = new int[2];
                testVals[0] = 4;
                for (int i = 12; i >= 0; i--)
                {
                    if (valueCounts[i] == 1)
                    {
                        testVals[1] = i;
                        for (int n = 0; n < 5; n++)
                        {
                            if (c[n].value == i)
                            {
                                cards[cix] = c[n];
                                cix++;
                            }
                        }
                        if (i == 12 && valueCounts[11] == 0) testVals[1] = 5;
                    }
                }
            }
            else if (suit != 0)
            {
                handType = "Flush";
                testVals = new int[6];
                testVals[0] = 5;
                int j = 1;
                for (int i = 12; i >= 0; i--)
                {
                    if (valueCounts[i] == 1)
                    {
                        testVals[j] = i;
                        for (int n = 0; n < 5; n++)
                        {
                            if (c[n].value == i)
                            {
                                cards[cix] = c[n];
                                cix++;
                            }
                        }
                        j++;
                    }
                }
            }
            else
            {
                handType = "High Card";
                testVals = new int[6];
                testVals[0] = 0;
                int j = 1;
                for (int i = 12; i >= 0; i--)
                {
                    if (valueCounts[i] == 1)
                    {
                        testVals[j] = i;
                        for (int n = 0; n < 5; n++)
                        {
                            if (c[n].value == i)
                            {
                                cards[cix] = c[n];
                                cix++;
                            }
                        }
                        j++;
                    }
                }
            }
        }

        public int isBetter(Hand h)
        {
            for (int i = 0; i < testVals.Length; i++)
            {
                if (testVals[i] > h.testVals[i]) return 1;
                if (testVals[i] < h.testVals[i]) return -1;
            }
            return 0;
        }
        public string print()
        {
            string res = handType;

            for (int i = 0; i < 5; i++)
            {
                res += " " + cards[i].print();
            }
            return res;
        }
    }
}
