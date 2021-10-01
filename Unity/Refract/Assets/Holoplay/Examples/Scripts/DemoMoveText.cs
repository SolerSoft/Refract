//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LookingGlass.Demos {
    public class DemoMoveText : MonoBehaviour {
        public LookingGlass.Holoplay holoplay;

        public Text label;
        public Text textThatMoves;

        private float whenToStartAutomating;
        private float amountForward;

        void Start() {
            textThatMoves.text = "This text is moving";
            whenToStartAutomating = 0;
        }

        void Update() {
            if (Input.GetKey(KeyCode.UpArrow)) {
                amountForward = amountForward + 0.1f;
                whenToStartAutomating = Time.time + 2;
            } else if (Input.GetKey(KeyCode.DownArrow)) {
                amountForward = amountForward - 0.1f;
                whenToStartAutomating = Time.time + 2;
            }

            if (Time.time > whenToStartAutomating) {
                amountForward = Mathf.Sin(Time.time) * 5f;
            }

            Vector3 textPosition = textThatMoves.transform.localPosition;
            textPosition.z = amountForward;
            textThatMoves.transform.localPosition = textPosition;

            label.text = "Units from plane of convergence: " + amountForward.ToString("F1");

            float nearClipAmount = holoplay.size * holoplay.nearClipFactor * -1;
            float farClipAmount = holoplay.size * holoplay.farClipFactor;

            if (amountForward < nearClipAmount || amountForward > farClipAmount) {
                label.text += "\n(beyond the clipping plane)";
            }
        }
    }
}
