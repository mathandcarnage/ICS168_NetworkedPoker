using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class JoinButtonBehavior : MonoBehaviour
{

    public GameObject button;
    public GameObject serverName;
    public GameObject maxPlayers;
    public GameObject buyIn;

    // Use this for initialization
    void Start()
    {
        clearServerName();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void clearServerName()
    {
        button.SetActive(false);
        serverName.SetActive(false);
        maxPlayers.SetActive(false);
        buyIn.SetActive(false);
    }

    public void updateServerName(string s, int cP, int mP, int bI)
    {
        button.SetActive(true);
        serverName.SetActive(true);
        maxPlayers.SetActive(true);
        buyIn.SetActive(true);
        serverName.GetComponent<Text>().text = s;
        maxPlayers.GetComponent<Text>().text = cP + "/" + mP;
        buyIn.GetComponent<Text>().text = ""+bI;
    }

    public void joinButtonPress()
    {
        GameObject.Find("NetworkManager").GetComponent<NetworkConnection>().Send("Join\n" + serverName.GetComponent<Text>().text + "\n<EOF>");
    }
}
