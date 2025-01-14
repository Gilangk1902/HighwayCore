using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class PauseMenu : MonoBehaviour
{
    bool isPaused = false;
    public GameObject PauseUI;
    void Update()
    {
        if(!Player.ActivePlayer.dead && Input.GetKeyDown(KeyCode.Escape)){
            if(isPaused){
                Resume();
            }
            else if(!isPaused){
                Pause();
            }
        }
    }

    public void Pause(){
        Time.timeScale = 0f;
        PauseUI.SetActive(true);
        isPaused = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        AudioPlayer.PauseAll(true);
        Player.ActivePlayer.EnableInput(false);
    }

    public void Resume(){
        Time.timeScale = 1f;
        PauseUI.SetActive(false);
        isPaused = false;
        Cursor.lockState = CursorLockMode.Locked;

        AudioPlayer.PauseAll(false);
        Player.ActivePlayer.EnableInput(true);
    }

    public void QuitToMainMenu(){
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene("MainMenu");
    }
}
