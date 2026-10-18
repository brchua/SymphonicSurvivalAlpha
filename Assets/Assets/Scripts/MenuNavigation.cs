using UnityEngine;

public class MenuNavigation : MonoBehaviour
{
    public GameObject MainMenu;
    public GameObject SettingsMenu;
    public GameObject CreditsMenu;

    public void OpenSettings()
    {
        MainMenu.SetActive(false);
        SettingsMenu.SetActive(true);
    }

    public void CloseSettings()
    {
        SettingsMenu.SetActive(false);
        MainMenu.SetActive(true);
    }

    public void OpenCredits()
    {
        MainMenu.SetActive(false);
        CreditsMenu.SetActive(true);
    }

    public void CloseCredits()
    {
        CreditsMenu.SetActive(false);
        MainMenu.SetActive(true);
    }
}
