using UnityEngine;

namespace LookingGlassExamples {

public class Rotater : MonoBehaviour
{
    public Vector3 rotate;

    void Update() {
        transform.localRotation *= Quaternion.Euler(rotate * Time.deltaTime);
    }
}

}