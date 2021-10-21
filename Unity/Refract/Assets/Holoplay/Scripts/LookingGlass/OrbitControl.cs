using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LookingGlass {
	public class OrbitControl : MonoBehaviour {

		private static OrbitControl instance;
		public static OrbitControl Instance {
			get {
				if (instance != null) return instance;
				instance = FindObjectOfType<OrbitControl>();
				return instance;
			}
		}

		[System.NonSerialized]
		public bool touchInputThisFrame = false;

		Vector2 rMomentum;
		Vector3 rLastPos;

		[Header("Rotation variables")]
		[SerializeField] [Range(0, 2)] float rotateSpeed = 0.5f;
		[SerializeField] [Range(0, 1)] float rotateDrag = 0.1f;

		[SerializeField] [Range(0, 90)] float yMax = 80;
		[SerializeField] [Range(-90, 0)] float yMin = -80;

		Vector2 pMomentum;
		Vector3 pLastPos;

		[Header("Pan")]
		[SerializeField] [Range(0, 2)] float panSpeed = 0.7f;
		[SerializeField] [Range(0, 1)] float panDrag = 0.1f;

		float zMomentum;
		float zLastYPos;

		[Header("Zoom")]
		[SerializeField] [Range(0, 10)] float mouseZoomSpeed = 1f;
		[SerializeField] [Range(0, .2f)] float multitouchZoomSpeed = .1f;
		[SerializeField] [Range(0, 1)] float zoomDrag = 0.2f;
		[SerializeField] float multitouchZoomThreshold = 20f;
		[SerializeField] float multitouchMaxJump = 100f;
		[SerializeField] float minHoloplaySize = 0.01f;
		[SerializeField] float maxHoloplaySize = 500f;
		bool validRotationStart = false;
		bool refocusingToPosition = false;
		Vector3 refocusToPosition;
		Vector3 startRefocusPosition;

		[Header("Double Click")]
		[SerializeField] AnimationCurve refocusToPointCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
		[SerializeField] float refocusToTime = 0.5f;
		[SerializeField] float maxDistBetweenClicksMouse = 10f;
		[SerializeField] float maxDistBetweenClicksTouch = 20f;
		[SerializeField] float doubleClickTime = 0.5f;
		[SerializeField] float doubleClickTimeTouch = 1f;
		float doubleClickTimer;
		float refocusToTimer;
		bool oneClick = false;
		bool oneTouch = false;
		Vector3 oneClickPos;

		[Header("UI Event System")]
		[SerializeField] UnityEngine.EventSystems.EventSystem eventSys = null;

		[System.NonSerialized]
		public bool canInteract = true;

		[Header("Multitouch Remaps")]
		public Vector2 multitouchRemapCursorY = new Vector2(0.05f, 0.95f);
		public Vector2 multitouchRemapCursorX = new Vector2(0.05f, 0.95f);

		Vector2[] lastTouchPositions;

		[SerializeField] Renderer cursorRend;

		void Start() {
			lastTouchPositions = new Vector2[2];
			if (cursorRend == null) {
				cursorRend = FindObjectOfType<Cursor3D>().GetComponentInChildren<Renderer>();
			}
		}

		Vector3 mousePosOnTouchEnd;

		// Update is called once per frame
		void Update() {
			bool touchLastFrame = touchInputThisFrame;

			touchInputThisFrame = Input.touchCount > 0;

			if (touchInputThisFrame) {
				if (!touchLastFrame && cursorRend.enabled)
					cursorRend.enabled = false;
			} else if (touchLastFrame) {
				mousePosOnTouchEnd = Input.mousePosition;
			} else if (!cursorRend.enabled && Vector3.SqrMagnitude(mousePosOnTouchEnd - Input.mousePosition) > 0 && !Cursor3D.Instance.uiCursorMode) {
				cursorRend.enabled = true;
			}

			bool overUI = false;
			if (eventSys != null) {
				overUI = eventSys.IsPointerOverGameObject();
			}

			if (refocusingToPosition) {
				refocusToTimer += Time.deltaTime;
				refocusToTimer = Mathf.Clamp(refocusToTimer, 0, refocusToTime);
				float t = refocusToPointCurve.Evaluate(refocusToTimer / refocusToTime);
				Holoplay.Instance.transform.position = Vector3.Lerp(startRefocusPosition, refocusToPosition, t);
				if (refocusToTimer == refocusToTime) {
					refocusingToPosition = false;
					refocusToTimer = 0f;
					validRotationStart = false;
				} else {
					HandleDoubleClick();
					return;
				}
			}

			if (canInteract && !overUI) {
				RotateInputStart();
				Pan();
				Zoom();
			}

			RotateInputContinue(overUI);
			if (!touchInputThisFrame || Input.touchCount == 1) //This seems sketch
				Rotate();
		}

		void RotateInputStart() {
			if (Input.GetMouseButtonDown(0)) {
				validRotationStart = true;
				if (!touchInputThisFrame)
					rLastPos = Input.mousePosition;
				else
					rLastPos = MultitouchPosition(0);
				rMomentum = Vector2.zero;
			}
		}

		void RotateInputContinue(bool overUI) {
			if (Input.GetMouseButton(0)) {
				if (validRotationStart) {
					Vector3 delta = Input.mousePosition - rLastPos;
					if (touchInputThisFrame)
						delta = (Vector3) MultitouchPosition(0) - rLastPos;
					delta *= Time.deltaTime * rotateSpeed;

					rMomentum += new Vector2(-delta.y, delta.x);

					rLastPos = Input.mousePosition;
					if (touchInputThisFrame)
						rLastPos = MultitouchPosition(0);
				}
			} else if (Input.GetMouseButtonUp(0)) {
				validRotationStart = false;
			}
		}

		void Rotate() {
			float newY = transform.eulerAngles.x + rMomentum.x;

			if (newY < yMin || (newY < 360 + yMin && newY > 120)) {
				rMomentum = new Vector2(0, rMomentum.y);
				transform.eulerAngles = new Vector3(yMin, transform.eulerAngles.y, 0);
			} else if (newY > yMax && newY < 360 + yMin) {
				rMomentum = new Vector2(0, rMomentum.y);
				transform.eulerAngles = new Vector3(yMax, transform.eulerAngles.y, 0);
			} else {
				transform.RotateAround(transform.position, transform.right, rMomentum.x);
			}

			transform.RotateAround(transform.position, Vector3.up, rMomentum.y);
		}

		void Zoom() {
			if (!touchInputThisFrame) {
				SimpleZoom();
			} else if (Input.touchCount > 1) {
				MultitouchZoom();
			}
			HandleDoubleClick();
		}

		void SimpleZoom() {
			float mouseScrollChange = Input.GetAxis("Mouse ScrollWheel");
			mouseScrollChange = Mathf.Clamp(mouseScrollChange, -1f, 1f);

			if (mouseScrollChange != 0) {
				float newSize = Holoplay.Instance.size - mouseScrollChange * mouseZoomSpeed * Holoplay.Instance.size;
				Holoplay.Instance.size = Mathf.Clamp(newSize, minHoloplaySize, maxHoloplaySize);
			}
		}

		float Remap(float value, float from1, float to1, float from2, float to2) {
			return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
		}

		Vector2 MultitouchPosition(int touchIndex) {
			float minXVal = Holoplay.Instance.cal.screenWidth * multitouchRemapCursorX.x;
			float minYVal = Holoplay.Instance.cal.screenHeight * multitouchRemapCursorY.x;

			float x = Input.GetTouch(touchIndex).position.x * multitouchRemapCursorX.y + minXVal;
			float y = Input.GetTouch(touchIndex).position.y * multitouchRemapCursorY.y + minYVal;

			return new Vector2(x, y);
		}

		void MultitouchZoom() {
			if (Input.GetTouch(1).phase == TouchPhase.Began) {
				lastTouchPositions[0] = MultitouchPosition(0);
				lastTouchPositions[1] = MultitouchPosition(1);
				return;
			}
			float distLastFrame = Vector2.Distance(lastTouchPositions[0], lastTouchPositions[1]);
			float distThisFrame = Vector2.Distance(MultitouchPosition(0), MultitouchPosition(1));

			float delta = distLastFrame - distThisFrame;

			if (Mathf.Abs(delta) < multitouchZoomThreshold)
				return;

			delta -= multitouchZoomThreshold * Mathf.Sign(delta);

			if (Mathf.Abs(delta) > multitouchMaxJump) {
				delta = multitouchMaxJump * Mathf.Sign(delta);
			}

			float newSize = Holoplay.Instance.size + delta * multitouchZoomSpeed * Holoplay.Instance.size;
			Holoplay.Instance.size = Mathf.Clamp(newSize, minHoloplaySize, maxHoloplaySize);

			lastTouchPositions[0] = MultitouchPosition(0);
			lastTouchPositions[1] = MultitouchPosition(1);
		}

		void HandleDoubleClick() {
			if (oneClick) {
				doubleClickTimer += Time.deltaTime;
				if (doubleClickTimer >= doubleClickTime) {
					oneClick = false;
					doubleClickTimer = 0;
				}
			} else if (oneTouch) {
				doubleClickTimer += Time.deltaTime;
				if (doubleClickTimer >= doubleClickTimeTouch) {
					oneTouch = false;
					doubleClickTimer = 0;
				}
			}

			if (touchInputThisFrame && Input.GetTouch(0).phase == TouchPhase.Began) {
				if (oneTouch) {
					if (Vector3.Distance(oneClickPos, MultitouchPosition(0)) < maxDistBetweenClicksTouch)
						StartRaycastRefocus(Cursor3D.Instance.GetOverObject());
				} else {
					oneTouch = true;
					oneClickPos = MultitouchPosition(0);
					doubleClickTimer = 0;
				}
			}

			if (Input.GetMouseButtonDown(0)) {
				if (oneClick) {
					if (Vector3.Distance(oneClickPos, Input.mousePosition) < maxDistBetweenClicksMouse)
						StartRaycastRefocus(Cursor3D.Instance.GetOverObject());
				} else {
					oneClick = true;
					oneClickPos = Input.mousePosition;
					doubleClickTimer = 0;
				}
			}
		}

		void StartRaycastRefocus(bool overObject) {
			Vector3 vec = Vector3.zero;

			if (overObject) {
				vec = Cursor3D.Instance.GetWorldPos();
			}

			//Debug.Log(overObject);

			refocusingToPosition = true;
			refocusToPosition = vec;
			refocusToTimer = 0f;
			startRefocusPosition = Holoplay.Instance.transform.position;
			oneClick = false;
			oneTouch = false;
			doubleClickTimer = 0;
			rMomentum = Vector2.zero;
			pMomentum = Vector2.zero;
			zMomentum = 0f;
		}

		void Pan() {
			if (!touchInputThisFrame) {
				if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) {
					pLastPos = Input.mousePosition;
					pMomentum = Vector2.zero;
				} else if (Input.GetMouseButton(1) || Input.GetMouseButton(2)) {
					Vector3 delta = Input.mousePosition - pLastPos;
					float adjustedSpeed = panSpeed / 100f;
					delta *= Time.deltaTime * adjustedSpeed * Holoplay.Instance.size;

					pMomentum -= new Vector2(delta.x, delta.y);

					pLastPos = Input.mousePosition;
				}
			} else {
				if (Input.touchCount > 1 && Input.GetTouch(1).phase == TouchPhase.Began) {
					pLastPos = (MultitouchPosition(0) + MultitouchPosition(1)) / 2.0f;
					pMomentum = Vector2.zero;
				} else if (Input.touchCount > 1) {
					Vector3 currentPos = (MultitouchPosition(0) + MultitouchPosition(1)) / 2.0f;
					Vector3 delta = currentPos - pLastPos;
					float adjustedSpeed = panSpeed / 100f;
					delta *= Time.deltaTime * adjustedSpeed * Holoplay.Instance.size;

					pMomentum -= new Vector2(delta.x, delta.y);

					pLastPos = currentPos;
				}
			}

			transform.position += transform.right * pMomentum.x;
			transform.position += transform.up * pMomentum.y;
		}

		void FixedUpdate() {
			rMomentum *= 1 - rotateDrag;
			pMomentum *= 1 - panDrag;
			zMomentum *= 1 - zoomDrag;
		}
	}
}