using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float movementSpeed = 0.1f;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.A))
        {
            transform.position += new Vector3(-movementSpeed, 0, 0);
        }
        else if (Input.GetKey(KeyCode.D))
        {
            transform.position += new Vector3(movementSpeed, 0, 0);
        }
        else if (Input.GetKey(KeyCode.W))
        {
            transform.position += new Vector3(0, 0, movementSpeed);
        }
        else if (Input.GetKey(KeyCode.S))
        {
            transform.position += new Vector3(0, 0, -movementSpeed);
        }
    }
}
