using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CambiarEscenas : MonoBehaviour
{
    [SerializeField] TMP_Dropdown scenesDropdown;
    [SerializeField] string[] sceneNames;

    public void ChangeScene(){
        SceneManager.LoadScene(sceneNames[scenesDropdown.value]);
    }
}
