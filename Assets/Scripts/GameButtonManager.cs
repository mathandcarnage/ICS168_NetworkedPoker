using UnityEngine;
using System.Collections;

public class GameButtonManager : MonoBehaviour {
    string raiseAmt = null;

    public void FoldButton()
    {
        GameObject.Find("NetworkManager").GetComponent<NetworkConnection>().Send("Fold\n<EOF>");
    }

    public void CallButton()
    {
        GameObject.Find("NetworkManager").GetComponent<NetworkConnection>().Send("Call\n<EOF>");
    }
    
    public void RaiseButton()
    {
        GameObject.Find("NetworkManager").GetComponent<NetworkConnection>().Send("Raise\n" + raiseAmt + "\n<EOF>");
    }

    public void SetRaise(string i)
    {
        raiseAmt = i;
    }
}
