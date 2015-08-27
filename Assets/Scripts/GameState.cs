using UnityEngine;
using System.Collections;

public class GameState : MonoBehaviour {

    public GameObject PlayerCard1;
    public GameObject PlayerCard2;
    public GameObject FlopCard1;
    public GameObject FlopCard2;
    public GameObject FlopCard3;
    public GameObject TurnCard;
    public GameObject RiverCard;

    public int state;

    private Deck deck;

	// Use this for initialization
	void Start () {
        PlayerCard1.GetComponent<Card>().display = false;
        PlayerCard2.GetComponent<Card>().display = false;
        FlopCard1.GetComponent<Card>().display = false;
        FlopCard2.GetComponent<Card>().display = false;
        FlopCard3.GetComponent<Card>().display = false;
        TurnCard.GetComponent<Card>().display = false;
        RiverCard.GetComponent<Card>().display = false;
        state = 0;
        deck = new Deck();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void advanceState()
    {
        if(state == 0)
        {
            deck.drawNext(PlayerCard1.GetComponent<Card>());
            deck.drawNext(PlayerCard2.GetComponent<Card>());
            PlayerCard1.GetComponent<Card>().display = true;
            PlayerCard2.GetComponent<Card>().display = true;
            state = 1;
        }
        else if(state == 1)
        {
            deck.drawNext(FlopCard1.GetComponent<Card>());
            deck.drawNext(FlopCard2.GetComponent<Card>());
            deck.drawNext(FlopCard3.GetComponent<Card>());
            FlopCard1.GetComponent<Card>().display = true;
            FlopCard2.GetComponent<Card>().display = true;
            FlopCard3.GetComponent<Card>().display = true;
            state = 2;
        }
        else if (state == 2)
        {
            deck.drawNext(TurnCard.GetComponent<Card>());
            TurnCard.GetComponent<Card>().display = true;
            state = 3;
        }
        else if (state == 3)
        {
            deck.drawNext(RiverCard.GetComponent<Card>());
            RiverCard.GetComponent<Card>().display = true;
            state = 4;
        }
        else if (state == 4)
        {
            Hand bestHand = null;
            Card[] allCards = new Card[7];
            allCards[0] = PlayerCard1.GetComponent<Card>();
            allCards[1] = PlayerCard2.GetComponent<Card>();
            allCards[2] = FlopCard1.GetComponent<Card>();
            allCards[3] = FlopCard2.GetComponent<Card>();
            allCards[4] = FlopCard3.GetComponent<Card>();
            allCards[5] = TurnCard.GetComponent<Card>();
            allCards[6] = RiverCard.GetComponent<Card>();
            for (int i = 0; i < 7; i ++ )
            {
                for(int j = 0; j < i; j ++)
                {
                    Card[] handCards = new Card[5];
                    int ix = 0;
                    for(int k = 0; k < 7; k ++)
                    {
                        if(k != i && k != j)
                        {
                            handCards[ix] = allCards[k];
                            ix++;
                        }
                    }
                    Hand newHand = new Hand(handCards);
                    if(bestHand == null || newHand.isBetter(bestHand) > 0)
                    {
                        bestHand = newHand;
                    }
                }
            }
            Debug.Log(bestHand.print());
            state = 5;
        }
        else
        {
            PlayerCard1.GetComponent<Card>().display = false;
            PlayerCard2.GetComponent<Card>().display = false;
            FlopCard1.GetComponent<Card>().display = false;
            FlopCard2.GetComponent<Card>().display = false;
            FlopCard3.GetComponent<Card>().display = false;
            TurnCard.GetComponent<Card>().display = false;
            RiverCard.GetComponent<Card>().display = false;
            state = 0;
            deck = new Deck();
        }
    }

    private class Deck
    {
        int[] cards;
        int currIndex;

        public Deck()
        {
            cards = new int[52];
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
                    int r = Random.Range(0, i + 1);
                    cards[i] = cards[r];
                    cards[r] = c;
                    i++;
                }
            }
        }

        public void drawNext(Card c)
        {
            int n = cards[currIndex];
            currIndex++;
            c.suit = n & 3;
            c.value = n >> 2;
        }
    }

    private class Hand
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
            for(int i = 0; i < 5; i ++)
            {
                cards[i] = c[i];
                suit = suit & (1 << c[i].suit);
                valueCounts[c[i].value]++;
                for(int j = 0; j < i; j ++)
                {
                    if (c[i].value == c[j].value) pair++;
                    if (Mathf.Abs(c[i].value-c[j].value) == 1)
                    {
                        if(c[i].value == 12 || c[j].value == 12)
                        {
                            consec++;
                        }
                        else
                        {
                            consec += 2;
                        }
                    }
                    if (Mathf.Abs(c[i].value - c[j].value) == 12) consec++;
                }
            }
            if(pair == 1)
            {
                handType = "One Pair";
                testVals = new int[5];
                testVals[0] = 1;
                for(int i = 12; i >= 0; i --)
                {
                    if(valueCounts[i] == 2)
                    {
                        testVals[1] = i;
                        break;
                    }
                }
                int j = 2;
                for (int i = 12; i >= 0; i--)
                {
                    if (valueCounts[i] == 1)
                    {
                        testVals[j] = i;
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
                        j++;
                    }
                }
                for (int i = 12; i >= 0; i--)
                {
                    if (valueCounts[i] == 1)
                    {
                        testVals[j] = i;
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
                        break;
                    }
                }
                int j = 2;
                for (int i = 12; i >= 0; i--)
                {
                    if (valueCounts[i] == 1)
                    {
                        testVals[j] = i;
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
                        break;
                    }
                }
                for (int i = 12; i >= 0; i--)
                {
                    if (valueCounts[i] == 2)
                    {
                        testVals[2] = i;
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
                        break;
                    }
                }
                for (int i = 12; i >= 0; i--)
                {
                    if (valueCounts[i] == 1)
                    {
                        testVals[2] = i;
                        break;
                    }
                }
            }
            else if (consec >= 7 && suit != 0)
            {
                handType = "Straight Flush";
                testVals = new int[2];
                testVals[0] = 8;
                for(int i = 12; i >= 0; i --)
                {
                    if (valueCounts[i] == 1)
                    {
                        testVals[1] = i;
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
                        j++;
                    }
                }
            }
        }

        public int isBetter(Hand h)
        {
            for (int i = 0; i < testVals.Length; i ++)
            {
                if (testVals[i] > h.testVals[i]) return 1;
                if (testVals[i] < h.testVals[i]) return -1;
            }
            return 0;
        }

        public string print()
        {
            string res = handType + "(" + testVals[0] + ")";

            for (int i = 0; i < 5; i ++)
            {
                res += " " + cards[i].print();
            }
            return res;
        }
    }
}
