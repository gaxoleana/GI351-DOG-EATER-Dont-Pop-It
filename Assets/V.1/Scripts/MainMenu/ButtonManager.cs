using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonManager : MonoBehaviour
{
    // เรียกใช้จาก OnClick() ของ Button เพื่อโหลด Scene ที่ต้องการ
    public void LoadGameScene()
    {
        SceneManager.LoadScene("GameSceneV.1");
    }

    /* โหลด Scene โดยใช้ build index แทนชื่อ
    public void LoadSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }
    */

    /* โหลด Scene ปัจจุบันซ้ำ (ใช้สำหรับปุ่ม "Retry")
    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    */

    /* โหลด Scene ถัดไปตามลำดับใน Build Settings
    public void LoadNextScene()
    {
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            Debug.LogWarning("No next scene in Build Settings.");
        }
    }
    */

    // ปิดเกม (ทำงานเฉพาะตอน build จริง ใน Editor จะไม่ปิด) 
    public void QuitGame()
    {
        Debug.Log("Quit Requested.");
        Application.Quit();

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}