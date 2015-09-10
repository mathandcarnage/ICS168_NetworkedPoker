using UnityEngine;
using System.Collections;

public class LoginMenuBehavior : MonoBehaviour {

    string ip;
    string un;
    string pw;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void setIP(string s)
    {
        Debug.Log("<IP>" + s);
        ip = s;
    }

    public void setUserName(string s)
    {
        Debug.Log("<UN>" + s);
        un = s;
    }

    public void setPassword(string s)
    {
        Debug.Log("<PW>" + s);
        pw = s;
    }

    public void tryLogin()
    {
        GameObject.Find("NetworkManager").GetComponent<NetworkConnection>().NewClient(un,pw,ip);
    }
}
