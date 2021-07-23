using UnityEngine;
using UnityEngine.UI;

using UnityEngine.XR.ARFoundation;

namespace ARKitCollaborator.Samples
{
    public class CanvasTextManager : MonoBehaviour
    {
        [SerializeField]
        Text m_TextElement;
        [SerializeField]
        Text m_TextElementID;

        public Text textElement
        {
            get => m_TextElement;
            set => m_TextElement = value;
        }

        public Text textElementID {
            get => m_TextElementID;
            set => m_TextElementID = value;
        }

        public string text
        {
            get => m_TextElement ? m_TextElement.text : null;
            set
            {
                if (m_TextElement)
                {
                    m_TextElement.text = value;
                }
            }
        }

        public string textID {
            get => m_TextElementID ? m_TextElementID.text : null;
            set {
                if(m_TextElementID){
                    m_TextElementID.text = value;
                }
            }
        }

        void OnEnable()
        {
            // Hook up the canvas's world space camera
            if (m_TextElement)
            {
                var canvas = m_TextElement.GetComponentInParent<Canvas>();
                if (canvas)
                {
                    var sessionOrigin = FindObjectOfType<ARSessionOrigin>();
                    if (sessionOrigin && sessionOrigin.camera)
                    {
                        canvas.worldCamera = sessionOrigin.camera;
                    }
                }
            }
            if (m_TextElementID)
            {
                var canvas = m_TextElementID.GetComponentInParent<Canvas>();
                if (canvas)
                {
                    var sessionOrigin = FindObjectOfType<ARSessionOrigin>();
                    if (sessionOrigin && sessionOrigin.camera)
                    {
                        canvas.worldCamera = sessionOrigin.camera;
                    }
                }
            }
        }
    }
}
