using UnityEngine;
using UnityEngine.InputSystem.UI;

namespace LastLantern
{
    /// <summary>
    /// Гарантирует, что у UI-модуля новой Input System назначены стандартные действия
    /// (мышь, клавиатура, геймпад). Ставится на EventSystem перед InputSystemUIInputModule.
    /// </summary>
    public class UIInputFix : MonoBehaviour
    {
        void Awake()
        {
            var module = GetComponent<InputSystemUIInputModule>();
            if (module && module.actionsAsset == null) module.AssignDefaultActions();
        }
    }
}
