using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace KidzDev.Unity.ScrollSnap.Demo
{
    public class DemoPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject[] panels;
        [SerializeField] private string[]     sectionNames;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;

        private int _current;

        private void Start()
        {
            if (prevButton) prevButton.onClick.AddListener(Prev);
            if (nextButton) nextButton.onClick.AddListener(Next);
            ShowPanel(0);
        }

        public void Prev() => ShowPanel((_current - 1 + panels.Length) % panels.Length);
        public void Next() => ShowPanel((_current + 1) % panels.Length);

        private void ShowPanel(int index)
        {
            if (panels == null || panels.Length == 0) return;
            for (int i = 0; i < panels.Length; i++)
                if (panels[i] != null) panels[i].SetActive(i == index);
            _current = index;
            if (titleText != null && sectionNames != null && index < sectionNames.Length)
                titleText.text = sectionNames[index];
        }
    }
}
