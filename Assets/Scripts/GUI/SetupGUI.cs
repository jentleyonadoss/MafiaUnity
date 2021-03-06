﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MafiaUnity;
using System.Globalization;
using System.Threading.Tasks;

public class SetupGUI : MonoBehaviour {

    public GameObject pathSelection;
    public GameObject modManager;
    public GameObject mainMenu;
    public Text gameVersion;
    public Text buildTime;
    public GameObject buildBadge;
    public GameObject copyrightDisclaimer;
    public GameObject modErrorWarning;

    public AudioSource bgMusic;

    public List<Transform> pointsOfInterest = new List<Transform>();
    public int currentPOI = 0;
    bool gameWasLoaded = false;

    Transform mainCamera = null;

    public void StartGame()
    {
        // Revert settings back to default.
        RenderSettings.ambientLight = new Color32(54, 58, 66, 1);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

        if (bgMusic != null)
            bgMusic.Stop();

        mainMenu.SetActive(false);
        buildBadge.SetActive(false);
        copyrightDisclaimer.SetActive(false);
        GameAPI.instance.missionManager.DestroyMission();
        
        var modManager = GameAPI.instance.modManager;
        var mods = GetComponent<ModManagerGUI>();

        foreach (var mod in mods.modEntries)
        {
            if (mod.status == ModEntryStatus.Active)
            {
                modManager.LoadMod(mod.modName);
            }
        }

        mainCamera.position = Vector3.zero;
        mainCamera.rotation = Quaternion.identity;

        modManager.InitializeMods();

        GameAPI.instance.gameInstance = new GameObject("Game Instance");
        GameAPI.instance.gameInstance.AddComponent<GameMain>();

        gameWasLoaded = true;
    }

    public void PathSelectionMenu()
    {
        mainMenu.SetActive(false);
        pathSelection.SetActive(true);
    }

    public void ModManagerMenu()
    {
        mainMenu.SetActive(false);
        modManager.SetActive(true);
    }

    bool lateStart = false;

    async Task LateStart() 
    {
        await Task.Delay(5000);
        GameAPI.ResetGameAPI();

        bgMusic = GetComponent<AudioSource>();

        mainCamera = GameObject.Find("Main Camera")?.transform;

        if (PlayerPrefs.HasKey("gamePath"))
        {
            Debug.Log("Game path was detected: " + PlayerPrefs.GetString("gamePath"));

            if (!GameAPI.instance.SetGamePath(PlayerPrefs.GetString("gamePath")))
                PathSelectionMenu();
            else
                SetupDefaultBackground();
        }
        else
            PathSelectionMenu();

        CommandTerminal.Terminal.Shell.AddCommand("rgpnow", (CommandTerminal.CommandArg[] args) => {
            PlayerPrefs.DeleteKey("gamePath");
            PlayerPrefs.Save();
            Debug.Log("Game path was removed from PlayerPrefs!");
        }, 0, 0, "Resets the game path in PlayerPrefs");

        gameVersion.text = GameAPI.GAME_VERSION;
        buildTime.text = string.Format("Build Time: {0}", BuildInfo.BuildTime());
    }

    void SetupPOIs()
    {
        pointsOfInterest.Add(GameObject.Find("Group01")?.transform);
        pointsOfInterest.Add(GameObject.Find("fg")?.transform);
        pointsOfInterest.Add(GameObject.Find("Line03cv")?.transform);
        pointsOfInterest.Add(GameObject.Find("foto")?.transform);
        pointsOfInterest.Add(GameObject.Find("Group01")?.transform);
        pointsOfInterest.Add(GameObject.Find("Plane03")?.transform);
        pointsOfInterest.Add(GameObject.Find("Obr1")?.transform);
        pointsOfInterest.Add(GameObject.Find("bedna 02")?.transform);
        pointsOfInterest.Add(GameObject.Find("Doutnik")?.transform);

        pointsOfInterest.Shuffle();
    }

    async private void FixedUpdate()
    {
        if (!lateStart)
        {
            await LateStart();
            lateStart = true;
        }

        if (GameAPI.instance.GetModErrorStatus())
            modErrorWarning.SetActive(true);

        if (gameWasLoaded)
            return;

        if (pointsOfInterest.Count > 0 && mainCamera != null)
        {
            var poi = pointsOfInterest[currentPOI];
            
            if (poi == null)
            {
                currentPOI = (currentPOI == pointsOfInterest.Count-1) ? 0 : currentPOI + 1;
            }
            else
            {
                var rot = Quaternion.LookRotation(poi.position - mainCamera.position);
                mainCamera.rotation = Quaternion.Slerp(mainCamera.rotation, rot, 0.05f * Time.deltaTime);

                if (Quaternion.Angle(mainCamera.rotation, rot) < 35f)
                {
                    currentPOI = (currentPOI == pointsOfInterest.Count-1) ? 0 : currentPOI+1;
                }   
            }
        }

        if (bgMusic != null && bgMusic.isPlaying)
        {
            float volume = float.Parse(GameAPI.instance.cvarManager.Get("musicVolume", "0.35"), CultureInfo.InvariantCulture);

            bgMusic.volume = volume;
        }
    }

    bool bgWasSetup = false;

    public void SetupDefaultBackground()
    {
        if (bgWasSetup)
            return;

        if (GameAPI.instance.GetInitialized())
        {
            bgWasSetup = true;

            GameAPI.instance.missionManager.LoadMission("00menu");

            SetupPOIs();

            bgMusic.clip = MafiaUnity.MafiaFormats.OGGLoader.ToAudioClip("music/Lake of Fire");
            bgMusic.Play();
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
