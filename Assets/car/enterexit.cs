using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class CarEnterExitSystem : MonoBehaviour
{

    public MonoBehaviour CarController;
    public Transform Car;
    public Transform Player;
 
    public GameObject PlayerCam;
    public GameObject CarCam;

    public GameObject DriveUi;

    bool Candrive;



    // Start is called before the first frame update
    void Start()
    {
        CarController.enabled = false;
        DriveUi.gameObject.SetActive(false);
        setCarFalse();
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.E) && Candrive)
        {
            CarController.enabled = true;

            // Restart car audio
            AudioSource[] carAudioSources = Car.GetComponents<AudioSource>();
            foreach (AudioSource audio in carAudioSources)
            {
                if (!audio.isPlaying)
                    audio.Play();
            }

            DriveUi.gameObject.SetActive(false);

            Player.transform.SetParent(Car);
            Player.gameObject.SetActive(false);

            PlayerCam.gameObject.SetActive(false);
            CarCam.gameObject.SetActive(true);
        }


        if (Input.GetKeyDown(KeyCode.F))
        {
            setCarFalse();  
        }
    }


    void OnTriggerStay(Collider col)
    {
        if (col.gameObject.tag == "Player")
        {
            DriveUi.gameObject.SetActive(true);
            Candrive = true;
        }
    }

    void OnTriggerExit(Collider col)
    {
        if (col.gameObject.tag == "Player")
        {
            DriveUi.gameObject.SetActive(false);
            Candrive = false;
        }
    }

    void setCarFalse()
    {
        // Stop car audio when exiting
        AudioSource[] carAudioSources = Car.GetComponents<AudioSource>();
        foreach (AudioSource audio in carAudioSources)
        {
            audio.Stop();
        }

        CarController.enabled = false;

        Player.transform.SetParent(null);
        Player.gameObject.SetActive(true);

        PlayerCam.gameObject.SetActive(true);
        CarCam.gameObject.SetActive(false);
    }

}