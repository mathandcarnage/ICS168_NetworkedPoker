using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LobbyMenuBehavior : MonoBehaviour {

    public GameObject[] serverInfos;
    private string[] allNames;
    private int[] allCurrentPlayers;
    private int[] allMaxPlayers;
    private int[] allBuyIn;
    private int displayIndex;

    public GameObject upButton;
    public GameObject downButton;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void updateServers(string[] sn, int[] cp, int[] mp, int[] bi)
    {
        allNames = sn;
        allCurrentPlayers = cp;
        allMaxPlayers = mp;
        allBuyIn = bi;
        displayIndex = 0;
        setUpDisplay();
    }

    private void setUpDisplay()
    {
        upButton.SetActive(displayIndex > 0);
        downButton.SetActive(displayIndex + serverInfos.Length < allNames.Length);
        for(int i = 0; i < serverInfos.Length; i ++)
        {
            int j = i + displayIndex;
            if (j < allNames.Length)
            {
                serverInfos[i].GetComponent<JoinButtonBehavior>().updateServerName(allNames[j],allCurrentPlayers[j],allMaxPlayers[j],allBuyIn[j]);
            }
            else
            {
                serverInfos[i].GetComponent<JoinButtonBehavior>().clearServerName();
            }
        }
    }

    public void upPressed()
    {
        displayIndex--;
        setUpDisplay();
    }

    public void downPressed()
    {
        displayIndex++;
        setUpDisplay();
    }

    public void refreshPressed()
    {
        GameObject.Find("NetworkManager").GetComponent<NetworkConnection>().Send("GetServers\n<EOF>");
    }

    public void getMoreChipsPressed()
    {
        string numChips = GameObject.Find("Canvas").transform.FindChild("GetChipsField").GetComponent<InputField>().text;
        if (numChips == null) return;
        GameObject.Find("NetworkManager").GetComponent<NetworkConnection>().Send("AddChips\n" + numChips + "\n<EOF>");
    }

    public void hostPressed()
    {
        string s = "Host\n";
        string name = GameObject.Find("Canvas").transform.FindChild("ServerNameField").GetComponent<InputField>().text;
        string max = GameObject.Find("Canvas").transform.FindChild("MaxPlayersField").GetComponent<InputField>().text;
        string buy = GameObject.Find("Canvas").transform.FindChild("BuyInField").GetComponent<InputField>().text;
        if (name == null || max == null || buy == null) return;
        s += name + "\n";
        s += max + "\n";
        s += buy + "\n";
        GameObject.Find("NetworkManager").GetComponent<NetworkConnection>().Send(s + "<EOF>");
    }

    public void ExitPressed()
    {
        GameObject.Find("NetworkManager").GetComponent<NetworkConnection>().Send("Disconnect\n<EOF>");
        GameObject.Find("NetworkManager").GetComponent<NetworkConnection>().CloseClient();
        Application.LoadLevel("LoginScene");
    }
}
