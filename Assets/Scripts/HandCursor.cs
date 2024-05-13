using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandCursor : MonoBehaviour
{
    public Sprite defaultSprite;
    public Sprite newSprite;

    private int handIndex;
    private SpriteRenderer spriteRenderer;
    public BodySourceView kinectBody;

    private Collider  collider;
    public bool handClosed = false;

    void Start(){
        // Get the SpriteRenderer component attached to the GameObject
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Collider 
        collider = GetComponent<Collider>();;
        
        // Set the default sprite
        spriteRenderer.sprite = defaultSprite;

        // Check Right and left hand
        handIndex = 0;
        if (gameObject.name == "CursorRight") handIndex = 1; // Right hand


    }

    bool checkHandState(){
        if (kinectBody.handsState == null || kinectBody.handsState.Count < 2) return false;
        
        // Debug.Log("Hand State " + handIndex + ": " + kinectBody.handsState[handIndex]);
        handClosed = kinectBody.handsState[handIndex];
        return kinectBody.handsState[handIndex];
    }

    Vector3 getHandCoords(){
        if (kinectBody.handsCoords == null || kinectBody.handsCoords.Count < 2){
            if (handIndex == 0) return new Vector3(-5, 3, 0);
            else return new Vector3(5, 3, 0);

        }
        return kinectBody.handsCoords[handIndex];
    }

    void Update() {
        
        if (kinectBody == null){
            Debug.Log("Kinect Body NULL: " + kinectBody);
            return;
        }

        bool flag = checkHandState();

        if (handClosed) spriteRenderer.sprite = newSprite;
        else spriteRenderer.sprite = defaultSprite;


        // move the sprite to handCoords
        Vector3 handCoords = getHandCoords();

        handCoords[0] *= 2.5f;
        handCoords[1] *= 1.5f;
        handCoords[2] = 0.0f;

        transform.position = handCoords;

        // PrintCoordinates();

    }


    void PrintCoordinates() {
        // Get the bounds of the Collider2D
        Bounds bounds = collider.bounds;

        // Print the x and y coordinates of the hitbox
        Debug.Log("HAND Min X: " + bounds.min.x + " Max X: " + bounds.max.x + " Min Y: " + bounds.min.y + " Max Y: " + bounds.max.y);
    }


    

}


