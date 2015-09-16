using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class GameButtonManager : MonoBehaviour {
    string raiseAmt = null;

    void Start()
    {
        for (int i = 0; i < 8; i ++)
        {
            GameObject.Find("Canvas").transform.FindChild("PlayerInfo" + i).gameObject.SetActive(false);
        }
        disable();
    }

    public void FoldButton()
    {
        GameObject.Find("NetworkManager").GetComponent<NetworkConnection>().Send("Fold\n<EOF>");
        disable();
    }

    public void CallButton()
    {
        GameObject.Find("NetworkManager").GetComponent<NetworkConnection>().Send("Call\n<EOF>");
        disable();
    }
    
    public void RaiseButton()
    {
        if (raiseAmt == null || Convert.ToInt32(raiseAmt) <= 0) return;
        GameObject.Find("NetworkManager").GetComponent<NetworkConnection>().Send("Raise\n" + raiseAmt + "\n<EOF>");
        disable();
    }

    public void ExitButton()
    {
        GameObject.Find("NetworkManager").GetComponent<NetworkConnection>().Send("Leave\n<EOF>");
        Application.LoadLevel("LobbyScene");
    }

    public void SetRaise(string i)
    {
        raiseAmt = i;
    }

    public void enable()
    {
        GameObject.Find("Canvas").transform.FindChild("BetRaiseButton").transform.FindChild("Text").GetComponent<Text>().enabled = true;
        GameObject.Find("Canvas").transform.FindChild("CheckCallButton").transform.FindChild("Text").GetComponent<Text>().enabled = true;
        GameObject.Find("Canvas").transform.FindChild("FoldButton").transform.FindChild("Text").GetComponent<Text>().enabled = true;
        GameObject.Find("Canvas").transform.FindChild("CallAmount").GetComponent<Text>().enabled = true;
        GameObject.Find("Canvas").transform.FindChild("CallLabel").GetComponent<Text>().enabled = true;
    }

    private void disable()
    {
        GameObject.Find("Canvas").transform.FindChild("BetRaiseButton").transform.FindChild("Text").GetComponent<Text>().enabled = false;
        GameObject.Find("Canvas").transform.FindChild("CheckCallButton").transform.FindChild("Text").GetComponent<Text>().enabled = false;
        GameObject.Find("Canvas").transform.FindChild("FoldButton").transform.FindChild("Text").GetComponent<Text>().enabled = false;
        GameObject.Find("Canvas").transform.FindChild("CallAmount").GetComponent<Text>().enabled = false;
        GameObject.Find("Canvas").transform.FindChild("CallLabel").GetComponent<Text>().enabled = false;
        GameObject.Find("Canvas").transform.FindChild("InputField").GetComponent<InputField>().text = string.Empty;
        raiseAmt = null;
    }
}
