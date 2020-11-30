using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPosition : MonoBehaviour {
    public float offset = 1.0f;

    public void Init(int graphWidth, int graphHeight) {
        transform.position = new Vector3(graphWidth / 2, transform.position.y, graphHeight / 2);
        GetComponent<Camera>().orthographicSize = (graphHeight / 2) + offset;
    }
}
