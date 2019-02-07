using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Assets.Scripts.Game_Scripts
{
    public class MessageWindow : MonoBehaviour
    {
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Message;

        public static MessageWindow Instance { get; set; }
        private float _timer = 0f;

        private void Awake()
        {
            Instance = this;
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            Instance = this;
            _timer = 0f;
        }

        private void OnDisable()
        {
            Title.text = string.Empty;
            Message.text = string.Empty;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= 20f)
            {
                this.gameObject.SetActive(false);
                _timer = 0f;
            }
        }
    }
}
