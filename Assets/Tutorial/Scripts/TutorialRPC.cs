using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TutorialRPC : MonoBehaviourPun
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    
    }

    [PunRPC]
    void DistributeChangeColor(float r, float g, float b)
    {
        GetComponent<Renderer>().material.color = new Color(r, g, b, 1f);
    }

    public void ChangeColor()
    {
        float r = Random.Range(0f, 1f);
        float g = Random.Range(0f, 1f);
        float b = Random.Range(0f, 1f);

        print(r + " " + g + " " + b);
        GetComponent<Renderer>().material.color = new Color(r, g, b, 1f);

        photonView.RPC("DistributeChangeColor", RpcTarget.Others, r, g, b);
    }
}
