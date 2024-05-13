using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Button : MonoBehaviour
{

    public Sprite defaultButton;
    public Sprite hoverButton;
    private SpriteRenderer spriteRenderer;

    private Collider  collider;
    private GameObject cursorHover = null;

    public string sceneName;

    // Start is called before the first frame update
    void Start() {
        collider = GetComponent<Collider>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = defaultButton;
    }

    // Update is called once per frame
    void Update(){

        // PrintCoordinates();

    }

    void PrintCoordinates() {
        // Get the bounds of the Collider2D
        Bounds bounds = collider.bounds;

        // Print the x and y coordinates of the hitbox
        Debug.Log("Button Min X: " + bounds.min.x + " Max X: " + bounds.max.x + " Min Y: " + bounds.min.y + " Max Y: " + bounds.max.y);
    }


    private void OnTriggerEnter(Collider collision){
        // Check if the colliding object has a specific tag (optional)
        Debug.Log("Button collided with " + collision.gameObject.name);

        if (true){ // Check if gameObject is a cursor
            spriteRenderer.sprite = hoverButton;
        }
    }

    private void OnTriggerStay(Collider collision){
        // Check if the colliding object has a specific tag (optional)
        Debug.Log("Button STAY with " + collision.gameObject.name);

         HandCursor handCursor = collision.GetComponent<HandCursor>();
        if (handCursor != null) {
            // Access the handClosed attribute of the HandCursor
            if (handCursor.handClosed) {
                // Change scene
                Debug.Log("Button PRESSED!!");
                SceneManager.LoadScene(sceneName);
            }
        }
    }

    private void OnTriggerExit(Collider collision){
        // Check if the colliding object has a specific tag (optional)
        Debug.Log("Button (exit) with " + collision.gameObject.name);

        if (true){ // Check if gameObject is a cursor
            spriteRenderer.sprite = defaultButton;
        }
    }


}
