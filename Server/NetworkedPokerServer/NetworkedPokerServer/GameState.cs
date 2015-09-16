using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        int dealer;
        int smallBlind;
        int numActivePlayers;
        int numAllIn;
        int leftoverPot;

        public int numPlayers;
        public int maxPlayers;
        public int buyIn;
        public int connectedPlayers;
        public string serverName;

        public GameState(int mP, int bI, string name)
        {
            state = 0;
            deck = new Deck();
            players = new Player[mP];
            dealtCards = new Card[5];
            currentPlayer = -1;
            dealer = -1;
            smallBlind = bI/100;
            numActivePlayers = 0;
            numAllIn = 0;
            leftoverPot = 0;
            maxPlayers = mP;
            buyIn = bI;
            numPlayers = 0;
            serverName = name;
        }

        private int advancePlayer(int n)
        {
            n++;
            if (n < 0 || n >= maxPlayers) n = 0;
            int orig = n;
            do
            {
                if (players[n] != null && !players[n].isFolded && players[n].chips > 0) return n;
                n++;
                if (n == maxPlayers) n = 0;
            }
            while (n != orig);
            return -1;
        }

        private void advanceState()
        {
            if (state == 0)
            {
                for (int i = 0; i < maxPlayers; i++)
                {
                    if (players[i] != null && players[i].leftGame)
                    {
                        players[i] = null;
                        numPlayers--;
                    }
                }
                numActivePlayers = 0;
                numAllIn = 0;
                deck.shuffle();
                for (int i = 0; i < maxPlayers; i++)
                {
                    if (players[i] != null)
                    {
                        if(players[i].setUpNewGame(deck.drawNext(), deck.drawNext()))
                        {
                            numActivePlayers++;
                        }
                        else
                        {
                            connectedPlayers--;
                            numPlayers--;
                            ServerSocket.Send(players[i].mySocket, "Kick\n<EOF>");
                            ServerSocket.Send(players[i].mySocket, ServerSocket.printServers());
                            ServerSocket.Send(players[i].mySocket, "StoredChips\n" + ServerSocket.database.getNumberOfChips(players[i].name) + "\n<EOF>");
                            players[i] = null;
                        }
                    }
                }
                if (numActivePlayers <= 1)
                {
                    BroadcastPublicInfo();
                    return;
                }
                dealtCards = new Card[5];
                dealer = advancePlayer(dealer);
                players[dealer].status = "Dealer";
                currentPlayer = advancePlayer(dealer);
                if(players[currentPlayer].bet(smallBlind, "Small Blind", smallBlind))numAllIn++;
                players[currentPlayer].hasActed = false;
                currentPlayer = advancePlayer(currentPlayer);
                if (players[currentPlayer].bet(smallBlind * 2, "Big Blind", smallBlind * 2)) numAllIn++;
                players[currentPlayer].hasActed = false;
                currentPlayer = advancePlayer(currentPlayer);
                players[currentPlayer].status = "Waiting";
                BroadcastPublicInfo();
                SendPrivateInfo();
                ServerSocket.Send(players[currentPlayer].mySocket, players[currentPlayer].printCall(getMaxBet()));
                state = 1;
            }
            else if (state == 1)
            {
                for (int j = 0; j < 3; j++)
                {
                    dealtCards[j] = deck.drawNext();
                }
                state = 2;
                if(numAllIn + 1 >= numActivePlayers)
                {
                    advanceState();
                    return;
                }
                for (int i = 0; i < maxPlayers; i++)
                {
                    if (players[i] != null)
                    {
                        players[i].setUpNewBet();
                    }
                }
                currentPlayer = advancePlayer(dealer);
                players[currentPlayer].status = "Waiting";
                BroadcastPublicInfo();
                SendPrivateInfo();
                ServerSocket.Send(players[currentPlayer].mySocket, players[currentPlayer].printCall(getMaxBet()));
            }
            else if (state == 2)
            {
                state = 3;
                dealtCards[3] = deck.drawNext();
                if (numAllIn + 1 >= numActivePlayers)
                {
                    advanceState();
                    return;
                }
                for (int i = 0; i < maxPlayers; i++)
                {
                    if (players[i] != null)
                    {
                        players[i].setUpNewBet();
                    }
                }
                currentPlayer = advancePlayer(dealer);
                players[currentPlayer].status = "Waiting";
                BroadcastPublicInfo();
                SendPrivateInfo();
                ServerSocket.Send(players[currentPlayer].mySocket, players[currentPlayer].printCall(getMaxBet()));
            }
            else if (state == 3)
            {
                dealtCards[4] = deck.drawNext();
                state = 4;
                if (numAllIn + 1 >= numActivePlayers)
                {
                    advanceState();
                    return;
                }
                for (int i = 0; i < maxPlayers; i++)
                {
                    if (players[i] != null)
                    {
                        players[i].setUpNewBet();
                    }
                }
                currentPlayer = advancePlayer(dealer);
                players[currentPlayer].status = "Waiting";
                BroadcastPublicInfo();
                SendPrivateInfo();
                ServerSocket.Send(players[currentPlayer].mySocket, players[currentPlayer].printCall(getMaxBet()));
            }
            else if (state == 4)
            {
                currentPlayer = -1;
                numActivePlayers = 0;
                Hand[] bestHands = new Hand[maxPlayers];
                for (int i = 0; i < maxPlayers; i++)
                {
                    if (players[i] != null && !players[i].isFolded)
                    {
                        bestHands[i] = players[i].getBestHand(dealtCards);
                        players[i].status = bestHands[i].print();
                        players[i].displayCards = true;
                    }
                }
                int newLeftovers = 0;
                while(getPot() > 0)
                {
                    Hand bestHand = null;
                    bool[] isBest = new bool[maxPlayers];
                    int bestCount = 0;
                    for (int i = 0; i < maxPlayers; i++)
                    {
                        if(bestHands[i] != null)
                        {
                            if(bestHand == null || bestHands[i].isBetter(bestHand) > 0)
                            {
                                bestHand = bestHands[i];
                                isBest = new bool[maxPlayers];
                                isBest[i] = true;
                                bestCount = 1;
                            }
                            else if(bestHands[i].isBetter(bestHand) == 0)
                            {
                                isBest[i] = true;
                                bestCount++;
                            }
                        }
                    }
                    int lowBet = int.MaxValue;
                    for (int i = 0; i < maxPlayers; i++)
                    {
                        if(isBest[i])
                        {
                            lowBet = Math.Min(lowBet, players[i].inPot);
                        }
                    }
                    int totalPot = leftoverPot;
                    leftoverPot = 0;
                    for (int i = 0; i < maxPlayers; i++)
                    {
                        if(players[i] != null)
                        {
                            totalPot += players[i].payout(lowBet);
                        }
                    }
                    int finalPayout = totalPot/bestCount;
                    for (int i = 0; i < maxPlayers; i++)
                    {
                        if (isBest[i])
                        {
                            players[i].chips += finalPayout;
                            totalPot -= finalPayout;
                            bestHands[i] = null;
                        }
                    }
                    newLeftovers += totalPot;
                }
                leftoverPot = newLeftovers;
                BroadcastPublicInfo();
                SendPrivateInfo();
                state = 5;
                advanceState();
            }
            else if (state == 5)
            {
                currentPlayer = -1;
                state = 0;
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(10000);
                    advanceState();
                });
            }
        }

        private void BroadcastPublicInfo()
        {
            for (int j = 0; j < maxPlayers; j++)
            {
                if (players[j] == null)
                {
                    Broadcast("PlayerClear\n" + j + "\n<EOF>");
                }
                else
                {
                    Broadcast(players[j].printInfo(j));
                }
            }
            Broadcast(printCardsInfo());
            Broadcast(printPotInfo());
        }

        private void SendPrivateInfo()
        {
            for (int i = 0; i < maxPlayers; i++)
            {
                if (players[i] != null && !players[i].leftGame)
                {
                    ServerSocket.Send(players[i].mySocket, players[i].printChips());
                    ServerSocket.Send(players[i].mySocket, players[i].printHand());
                }
            }
        }

        private void Broadcast(string data)
        {
            for (int i = 0; i < maxPlayers; i++)
            {
                if (players[i] != null && !players[i].leftGame)
                {
                    ServerSocket.Send(players[i].mySocket, data);
                }
            }
        }

        private string printCardsInfo()
        {
            string res = "CardsInfo\n";
            for (int i = 0; i < 5; i++)
            {
                if (dealtCards[i] == null)
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

        private int getPot()
        {
            int pot = leftoverPot;
            for (int i = 0; i < maxPlayers; i++)
            {
                if (players[i] != null)
                {
                    pot += players[i].inPot;
                }
            }
            return pot;
        }

        private int getMaxBet()
        {
            int max = 0;
            for (int i = 0; i < maxPlayers; i++)
            {
                if (players[i] != null)
                {
                    max = Math.Max(max, players[i].betSoFar);
                }
            }
            return max;
        }

        private string printPotInfo()
        {
            return "PotInfo\n" + getPot() + "\n" + getMaxBet() + "\n<EOF>";
        }

        public void Fold(int ix)
        {
            if (ix != currentPlayer) return;
            players[ix].isFolded = true;
            players[ix].hasActed = true;
            if(!players[ix].leftGame)players[ix].status = "Fold";
            players[ix].card1 = null;
            players[ix].card2 = null;
            numActivePlayers--;
            if(numActivePlayers == numAllIn)
            {
                advanceState();
                return;
            }
            currentPlayer = advancePlayer(currentPlayer);
            if(numActivePlayers == 1)
            {
                players[currentPlayer].chips += getPot();
                players[currentPlayer].status = "Winner";
                for (int i = 0; i < maxPlayers; i++)
                {
                    if (players[i] != null)
                    {
                        players[i].inPot = 0;
                    }
                }
                BroadcastPublicInfo();
                SendPrivateInfo();
                state = 5;
                advanceState();
                return;
            }
            else if(players[currentPlayer].leftGame)
            {
                Fold(currentPlayer);
                return;
            }
            else if(players[currentPlayer].hasActed &&
                (players[currentPlayer].chips== 0 ||
                players[currentPlayer].betSoFar == getMaxBet()))
            {
                advanceState();
                return;
            }
            players[currentPlayer].status = "Waiting";
            BroadcastPublicInfo();
            SendPrivateInfo();
            ServerSocket.Send(players[currentPlayer].mySocket, players[currentPlayer].printCall(getMaxBet()));
        }

        public void Call(int ix)
        {
            if (ix != currentPlayer) return;
            int amt = getMaxBet()-players[ix].betSoFar;
            if(amt == 0)
            {
                players[ix].status = "Check";
            }
            else
            {
                if(players[ix].bet(amt,"Call",-1)) numAllIn++;
            }
            players[ix].hasActed = true;
            if (numActivePlayers == numAllIn)
            {
                advanceState();
                return;
            }
            currentPlayer = advancePlayer(currentPlayer);
            if (players[currentPlayer].leftGame)
            {
                Fold(currentPlayer);
                return;
            }
            else if (players[currentPlayer].hasActed &&
                (players[currentPlayer].chips == 0 ||
                players[currentPlayer].betSoFar == getMaxBet()))
            {
                advanceState();
                return;
            }
            players[currentPlayer].status = "Waiting";
            BroadcastPublicInfo();
            SendPrivateInfo();
            ServerSocket.Send(players[currentPlayer].mySocket, players[currentPlayer].printCall(getMaxBet()));
        }

        public void Raise(int ix, int amt)
        {
            if (ix != currentPlayer) return;
            int bamt = getMaxBet() - players[ix].betSoFar + amt;
            if (getMaxBet() == 0)
            {
                if (players[ix].bet(amt, "Bet", amt)) numAllIn++;
            }
            else
            {
                if (players[ix].bet(bamt, "Raise", amt)) numAllIn++;
            }
            players[ix].hasActed = true;
            if (numActivePlayers == numAllIn)
            {
                advanceState();
                return;
            }
            currentPlayer = advancePlayer(currentPlayer);
            if (players[currentPlayer].leftGame)
            {
                Fold(currentPlayer);
                return;
            }
            else if (players[currentPlayer].hasActed &&
                (players[currentPlayer].chips == 0 ||
                players[currentPlayer].betSoFar == getMaxBet()))
            {
                advanceState();
                return;
            }
            players[currentPlayer].status = "Waiting";
            BroadcastPublicInfo();
            SendPrivateInfo();
            ServerSocket.Send(players[currentPlayer].mySocket, players[currentPlayer].printCall(getMaxBet()));
        }

        public int Join(Socket s, string n, int c)
        {
            if (c < buyIn) return -1;
            for (int i = 0; i < maxPlayers; i++)
            {
                if (players[i] == null)
                {
                    players[i] = new Player(s, n, buyIn);
                    numPlayers++;
                    connectedPlayers++;
                    ServerSocket.Send(s, "Join\nSuccess\n<EOF>");
                    BroadcastPublicInfo();
                    ServerSocket.Send(s, players[i].printChips());
                    if (state == 0) advanceState();
                    return i;
                }
            }
            return -1;
        }

        public int Leave(int ix)
        {
            if (players[ix] == null) return 0;
            connectedPlayers--;
            players[ix].status = "Left Game";
            players[ix].leftGame = true;
            if (!players[ix].isFolded)
            {
                if (ix == currentPlayer)
                {
                    Fold(ix);
                }
                else if (numActivePlayers == 2)
                {
                    players[currentPlayer].chips += getPot();
                    players[currentPlayer].status = "Winner";
                    for (int i = 0; i < maxPlayers; i++)
                    {
                        if (players[i] != null)
                        {
                            players[i].inPot = 0;
                        }
                    }
                    BroadcastPublicInfo();
                    SendPrivateInfo();
                    state = 5;
                    advanceState();
                }
            }
            BroadcastPublicInfo();
            return players[ix].chips;
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
        public bool displayCards;
        public bool leftGame;

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
            displayCards = false;
            leftGame = false;
        }

        public bool setUpNewGame(Card a, Card b)
        {
            inPot = 0;
            betSoFar = 0;
            displayCards = false;
            if (chips == 0)
            {
                card1 = null;
                card2 = null;
                isFolded = true;
                hasActed = true;
                status = "Out of Chips";
                return false;
            }
            isFolded = false;
            hasActed = false;
            card1 = a;
            card2 = b;
            status = string.Empty;
            return true;
        }

        public void setUpNewBet()
        {
            betSoFar = 0;
            if (isFolded || chips == 0 || leftGame) return;
            hasActed = false;
            status = string.Empty;
        }

        public bool bet(int amt, string type, int displayAmt)
        {
            if (amt >= chips)
            {
                betSoFar += chips;
                status = "All In";
                hasActed = true;
                inPot += chips;
                chips = 0;
                return true;
            }
            else
            {
                betSoFar += amt;
                chips -= amt;
                status = type;
                if (displayAmt >= 0)
                {
                    status += ": " + displayAmt;
                }
                hasActed = true;
                inPot += amt;
                return false;
            }
        }

        public int payout(int n)
        {
            if(inPot >= n)
            {
                inPot -= n;
                return n;
            }
            else
            {
                n = inPot;
                inPot = 0;
                return n;
            }
        }

        public Hand getBestHand(Card[] c)
        {
            Hand bestHand = null;
            Card[] allCards = new Card[7];
            allCards[0] = card1;
            allCards[1] = card2;
            for(int i = 0; i < 5; i ++)
            {
                allCards[i + 2] = c[i];
            }
            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    Card[] handCards = new Card[5];
                    int ix = 0;
                    for (int k = 0; k < 7; k++)
                    {
                        if (k != i && k != j)
                        {
                            handCards[ix] = allCards[k];
                            ix++;
                        }
                    }
                    Hand newHand = new Hand(handCards);
                    if (bestHand == null || newHand.isBetter(bestHand) > 0)
                    {
                        bestHand = newHand;
                    }
                }
            }
            return bestHand;
        }

        public string printInfo(int ix)
        {
            String res = "PlayerInfo\n" + ix + "\n" + name + "\n" + chips + "\n" + status + "\n";
            if (card1 == null || !displayCards)
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

            for (int s = 0; s < 4; s++)
            {
                for (int v = 0; v < 13; v++)
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
            return handType;
            /*string res = handType;

            for (int i = 0; i < 5; i++)
            {
                res += " " + cards[i].print();
            }
            return res;*/
        }
    }
}
