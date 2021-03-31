using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class Interaction : MonoBehaviour
{

    private GameObject mainCamera = null;
    private GameObject rightHandController;
    private XRController rightXRController;

    public float translationFactor = 1.0f;
    public float rotationFactor = 1.0f;

    private Vector3 startPosition = Vector3.zero;
    private Quaternion startRotation = Quaternion.identity;

    private bool primaryButtonLF = false;
    private bool secondaryButtonLF = false;

    public MonoBehaviour activeTechnique;
    public List<MonoBehaviour> techniques = new List<MonoBehaviour>();
    private int currentTechniqueIdx = 0;

    // Start is called before the first frame update
    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        mainCamera = GameObject.Find("Main Camera");
        rightHandController = GameObject.Find("RightHand Controller");

        if (rightHandController != null) // guard
        {
            rightXRController = rightHandController.GetComponent<XRController>();
        }

        //techniques.Add(GetComponent<DefaultRay>());
        //techniques.Add(GetComponent<DepthRay>());
        //techniques.Add(GetComponent<GoGoScript>());
        //techniques.Add(GetComponent<Argelaguet>());

        foreach (var t in techniques)
        {
            t.enabled = false;
        }
        activeTechnique = techniques[currentTechniqueIdx];
        activeTechnique.enabled = true;

        Debug.Log("Active Technique: " + activeTechnique);
    }

    // Update is called once per frame
    void Update()
    {
        if (rightHandController != null) // guard
        {

            // ----------------- Steering stuff -----------------

            // mapping: trigger (index finger)
            float trigger = 0.0f;
            rightXRController.inputDevice.TryGetFeatureValue(CommonUsages.trigger, out trigger);
            //Debug.Log("index finger rocker: " + trigger);

            Steering(0.0f, 0.0f, trigger, 0.0f, 0.0f, 0.0f); // map as forward steering input


            // ----------------- Technique toggle -----------------

            // mapping: primary button (A)
            bool primaryButton = false;
            rightXRController.inputDevice.TryGetFeatureValue(CommonUsages.primaryButton, out primaryButton);
            if (primaryButton != primaryButtonLF) // state changed
            {
                if (primaryButton) // up (0->1)
                {
                    techniques[currentTechniqueIdx].enabled = false; // disable previous technique
                    currentTechniqueIdx++;
                    if (currentTechniqueIdx == techniques.Count) currentTechniqueIdx = 0;
                    activeTechnique = techniques[currentTechniqueIdx];
                    activeTechnique.enabled = true; // enable current technique
                    //techniques[currentTechniqueIdx].enabled = true; // enable current technique
                    Debug.Log("Active Technique: " + activeTechnique);
                }
            }
            primaryButtonLF = primaryButton;

            // mapping: secondary button (B)
            bool secondaryButton = false;
            rightXRController.inputDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryButton);
            //Debug.Log("secondary button: " + secondaryButton);

            if (secondaryButton != secondaryButtonLF) // state changed
            {
                if (secondaryButton) // up (0->1)
                {
                    ResetXRRig();
                }
            }
            secondaryButtonLF = secondaryButton;

        }
    }

    private void Steering(float X, float Y, float Z, float RX, float RY, float RZ)
    {
        // translation in absolute controller direction
        Matrix4x4 rotmat = Matrix4x4.Rotate(rightHandController.transform.rotation);
        Matrix4x4 rotmatRig = Matrix4x4.Rotate(transform.rotation);
        Vector4 moveVec = new Vector4(X, Y, Z, 0.0f);
        float length = moveVec.magnitude;
        moveVec.Normalize();
        moveVec = moveVec * Mathf.Pow(length, 3); // exponential transfer function

        moveVec = rotmatRig.inverse * rotmat * moveVec;
        transform.Translate(moveVec * Time.deltaTime * translationFactor); // accumulate translation input


        // Head rotation
        RY = Mathf.Pow(RY, 3); // exponential transfer function
        //transform.Rotate(0.0f, RY * Time.deltaTime * rotationFactor, 0.0f, Space.Self); // rotate around platform center
        transform.RotateAround(mainCamera.transform.position, Vector3.up, RY * Time.deltaTime * rotationFactor); // rotate arround camera position (on platform)

        // Pitch rotation
        RX = Mathf.Pow(RX, 3); // exponential transfer function
        //transform.Rotate(RX * Time.deltaTime * rotationFactor, 0.0f, 0.0f, Space.Self); // rotate around platform center
        //transform.RotateAround(mainCamera.transform.position, Vector3.right, RX * Time.deltaTime * rotationFactor); // rotate arround camera position (on platform)
        Vector3 rightLocal = transform.TransformDirection(Vector3.right);
        transform.RotateAround(mainCamera.transform.position, rightLocal, RX * Time.deltaTime * rotationFactor); // rotate arround camera position (on platform) in local platform coordinate system

        // Roll rotation not mapped
    }

    private void ResetXRRig()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;
    }
}
