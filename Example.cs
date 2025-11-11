using Newtonsoft.Json;
using Unity.Android.Gradle.Manifest;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Example : MonoBehaviour, ISaveable
{
    public float moveSpeed = 5f;
    private CharacterController controller;
    public float gravity = -9.81f;
    private Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
       
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 move = new Vector3(horizontal, 0, vertical);
        move = transform.TransformDirection(move);
        controller.Move(move * moveSpeed * Time.deltaTime);
        float speed = move.magnitude; 

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    public object CaptureState()
    {
        return new SaveData
        {
            position = new float[] { transform.position.x, transform.position.y, transform.position.z },
            rotation = new float[] { transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z }
        };
    }

    public void RestoreState(object state)
    {
        SaveData data = JsonConvert.DeserializeObject<SaveData>(state.ToString());

        Vector3 pos = new Vector3(data.position[0], data.position[1], data.position[2]);
        Vector3 rot = new Vector3(data.rotation[0], data.rotation[1], data.rotation[2]);

        controller.enabled = false;
        transform.position = pos;
        transform.rotation = Quaternion.Euler(rot);
        controller.enabled = true;
    }


    [System.Serializable]
    private struct SaveData
    {
        public float[] position;
        public float[] rotation;
        public string saveDateTime;
    }
    public string GetUniqueIdentifier()
    {
        return SceneManager.GetActiveScene().name + "_Player";

    }
}
