using UnityEngine;

namespace vwds.twinalign
{
    public class MainMenuManager : MonoBehaviour
    {
        // Start is called before the first frame update
        public GameObject MenuPanel;
        private bool isDebugCanvasToggled;
        private Animator menuPanelAnimator;
        void Start()
        {
            isDebugCanvasToggled = false;
            menuPanelAnimator = MenuPanel.GetComponent<Animator>();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void GoToScene(string sceneName)
        {
            SceneSystem.Instance.LoadScene(sceneName);
            ToggleMenuPanel();
        }

        public void ToggleDebugCanvas()
        {
            isDebugCanvasToggled = !isDebugCanvasToggled;
        }

        public void ToggleMenuPanel()
        {
            menuPanelAnimator.SetTrigger("ToggleMenu");
        }
    }
}