using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialInteractionScript : MonoBehaviour
{

    public LayerMask interactiveLayer;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        { // if left button pressed...
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, interactiveLayer))
            {
                TutorialRPC interactiveScript = hit.transform.gameObject.GetComponent<TutorialRPC>();
                if (interactiveScript) // guard to make sure object has Tutorial RPC component
                    interactiveScript.ChangeColor();
            }
        }
    }
}
