using UnityEngine;
using System.Collections.Generic;

public class ProjectStart : MonoBehaviour
{
    bool isLoaded = false;
    List<GameSystem> gameSystemsForStart;
    List<GameSystem> readySystems;


    void Awake()
    {
        Debug.Log("Project Start");
        gameSystemsForStart = new List<GameSystem>();
        readySystems = new List<GameSystem>();
    }
    void Start()
    {
     //   LoadSystems(gameSystemsForStart);
    }

    private void LoadSystems(List<GameSystem> gameSystemForLoad)
    {
        gameSystemsForStart = gameSystemForLoad;
    }

    void Update()
    {
        Debug.Log("Update");

        if (gameSystemsForStart.Count == 0)
        {
            isLoaded = true;

        }
        if (isLoaded)
            LoadStartScene();

        foreach (GameSystem gm in gameSystemsForStart)
        {
            if (gm.isReady == true)
            {
                readySystems.Add(gm);
                gameSystemsForStart.Remove(gm);
            }


        }
    }

    private void LoadStartScene()
    {
     //   SceneController.Instance.LoadSceneById(1);
         SceneController.Instance.LoadSceneByName("MainGameScene");
        //SceneController.Instance.LoadSceneByName("MainMenuScene");


    }
}

public class GameSystem
{
    public bool isReady;
}
