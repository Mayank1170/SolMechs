using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Scene_Change : MonoBehaviour
{
    public int x = 1;
    public void Change_Scene()
    {
        SceneManager.LoadScene(x);
    }
}
