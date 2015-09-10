using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Card : MonoBehaviour {

    public int value;
    public int suit;
    public bool display;

    string[] suitChars = { "♠", "♣", "♥", "♦" };

    public GameObject valueText;
    public GameObject suitText;

	// Use this for initialization
	void Start () {
        value = Random.Range(0, 13);
        suit = Random.Range(0, 4);
	}
	
	// Update is called once per frame
	void Update () {
        if(display)
        {
            valueText.GetComponent<Text>().text = getValueString(value);
            suitText.GetComponent<Text>().text = suitChars[suit];

            if(suit >= 2)
            {
                valueText.GetComponent<Text>().color = Color.red;
                suitText.GetComponent<Text>().color = Color.red;
            }
            else
            {
                valueText.GetComponent<Text>().color = Color.black;
                suitText.GetComponent<Text>().color = Color.black;
            }
        }
        else
        {
            valueText.GetComponent<Text>().text = "";
            suitText.GetComponent<Text>().text = "";
        }
	}

    public void setCard(int n)
    {
        if(n < 0)
        {
            display = false;
            return;
        }
        display = true;
        suit = n & 3;
        value = n >> 2;
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
