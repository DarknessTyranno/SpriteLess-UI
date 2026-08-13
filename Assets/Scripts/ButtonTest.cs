using UnityEngine;
using UnityEngine.UI;

public class ButtonTest : MonoBehaviour
{

    [SerializeField] private Button button;

    void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick_Test);
    }

    void OnClick_Test()
    {
        Debug.Log($"call OnClick_Test", gameObject);
    }    
    

}
