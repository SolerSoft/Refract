using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LookingGlass.Extras {
	public class EscQuit : MonoBehaviour {

		public ButtonType lkgButton;

		void Update() {
			if (Input.GetKeyDown(KeyCode.Escape))
				Application.Quit();
		}
	}
}