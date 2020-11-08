﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public class Player : MonoBehaviour
{
    [Header("Settings")]
    public float gravity = -10f;

    public float moveSpeed = 3f;
    public float sprintSpeed = 6f;
    public float jumpForce = 5f;

    public float playerWidth = 0.3f;
    public float playerHeight = 1.8f;

    public bool isGrounded;
    public bool isSprinting;

    private Transform playerTransform;
    private Transform cameraTransform;

    private World world;

    private float horizontal;
    private float vertical;

    private float mouseHorizontal;
    private float mouseVertical;

    private Vector3 velocity;
    private float verticalMomentum;
    private bool jumpRequest;

    private float yRotation = 0;

    public Transform highlightBlock;
    public Transform placeBlock;
    public float checkIncrement = 0.1f;
    public float reach = 8f;

    public Toolbar toolbar;

    public bool FrontBlocked
    {
        get
        {
            if (world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z + playerWidth)) ||
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z + playerWidth)))
                return true;
            else
                return false;
        }
    }
    public bool BackBlocked
    {
        get
        {
            if (world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z - playerWidth)) ||
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z - playerWidth)))
                return true;
            else
                return false;
        }
    }
    public bool LeftBlocked
    {
        get
        {
            if (world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y, transform.position.z)) ||
                world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 1f, transform.position.z)))
                return true;
            else
                return false;
        }
    }
    public bool RightBlocked
    {
        get
        {
            if (world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y, transform.position.z)) ||
                world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 1f, transform.position.z)))
                return true;
            else
                return false;
        }
    }

    private void Awake()
    {
        playerTransform = transform;
        cameraTransform = Camera.main.transform;

        world = GameObject.Find("World").GetComponent<World>();

        world.InUI = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            world.InUI = !world.InUI;
        }

        if (!world.InUI)
        {
            GetPlayerInput();

            PlaceCursorBlock();

            RotatePlayer();
            RotateCamera();
        }
    }

    private void FixedUpdate()
    {
        if (!world.InUI)
        {
            CalculateVelocity();

            if (jumpRequest)
                Jump();

            transform.Translate(velocity, Space.World);
        }
    }

    private void CalculateVelocity()
    {
        // add vertical momentum
        if (verticalMomentum > gravity)
        {
            verticalMomentum += Time.fixedDeltaTime * gravity;
        }

        // set velocity, sprinting or normal
        velocity = ((transform.forward * vertical) + (transform.right * horizontal)).normalized * (isSprinting ? sprintSpeed : moveSpeed) * Time.deltaTime;

        // apply vertical momentum
        velocity += Vector3.up * verticalMomentum * Time.fixedDeltaTime;

        // check if the player is moving forward or bacwards and if something is in the way
        if ((velocity.z > 0 && FrontBlocked) || (velocity.z < 0 && BackBlocked))
        {
            // something was in the way stop the movement
            velocity.z = 0;
        }
        // check if the player is moving left or right and if someting is in the way
        if ((velocity.x > 0 && RightBlocked) || (velocity.x < 0 && LeftBlocked))
        {
            velocity.x = 0;
        }

        // is the player falling?
        if (velocity.y < 0)
        {
            // check if the player has hit the ground and set y velocity accordingly
            velocity.y = CheckDownSpeed(velocity.y);
        }
        // is the player jumping up?
        else if (velocity.y > 0)
        {
            velocity.y = CheckUpSpeed(velocity.y);
        }
    }

    private void Jump()
    {
        verticalMomentum = jumpForce;
        isGrounded = true;
        jumpRequest = false;
    }

    private void GetPlayerInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();

        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        mouseHorizontal = Input.GetAxis("Mouse X");
        mouseVertical = Input.GetAxis("Mouse Y");

        if (Input.GetButton("Sprint"))
            isSprinting = true;
        else
            isSprinting = false;

        if (isGrounded && Input.GetButtonDown("Jump"))
            jumpRequest = true;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (highlightBlock.gameObject.activeSelf)
        {
            // destroying blocks
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                world.GetChunkFromPosition(highlightBlock.position).EditVoxel(highlightBlock.position, 0);
            }

            // place blocks
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                if (toolbar.slots[toolbar.slotIndex].HasItem)
                {
                    world.GetChunkFromPosition(highlightBlock.position).EditVoxel(placeBlock.position, toolbar.slots[toolbar.slotIndex].itemSlot.stack.id);
                    toolbar.slots[toolbar.slotIndex].itemSlot.Take(1);
                }
            }
        }
    }

    private void PlaceCursorBlock()
    {
        float step = checkIncrement;
        Vector3 lastPos = new Vector3();

        while (step < reach)
        {
            Vector3 position = cameraTransform.position + (cameraTransform.forward * step);

            if (world.CheckForVoxel(position))
            {
                highlightBlock.position = new Vector3(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y), Mathf.FloorToInt(position.z));
                placeBlock.position = lastPos;

                highlightBlock.gameObject.SetActive(true);
                placeBlock.gameObject.SetActive(true);

                return;
            }

            lastPos = new Vector3(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y), Mathf.FloorToInt(position.z));
            step += checkIncrement;
        }

        highlightBlock.gameObject.SetActive(false);
        placeBlock.gameObject.SetActive(false);
    }

    private float CheckDownSpeed(float speed)
    {
        if ((world.CheckForVoxel(new Vector3(playerTransform.position.x - playerWidth, playerTransform.position.y + speed, playerTransform.position.z - playerWidth)) && (!LeftBlocked && !BackBlocked)) ||
            (world.CheckForVoxel(new Vector3(playerTransform.position.x - playerWidth, playerTransform.position.y + speed, playerTransform.position.z + playerWidth)) && (!LeftBlocked && !FrontBlocked)) ||
            (world.CheckForVoxel(new Vector3(playerTransform.position.x + playerWidth, playerTransform.position.y + speed, playerTransform.position.z - playerWidth)) && (!RightBlocked && !BackBlocked)) ||
            (world.CheckForVoxel(new Vector3(playerTransform.position.x + playerWidth, playerTransform.position.y + speed, playerTransform.position.z + playerWidth)) && (!RightBlocked && !FrontBlocked)))
        {
            isGrounded = true;

            return 0;
        }
        else
        {
            isGrounded = false;

            return speed;
        }
    }

    private float CheckUpSpeed(float speed)
    {
        if ((world.CheckForVoxel(new Vector3(playerTransform.position.x - playerWidth, playerTransform.position.y + playerHeight + speed, playerTransform.position.z - playerWidth)) && (!LeftBlocked && !BackBlocked)) ||
            (world.CheckForVoxel(new Vector3(playerTransform.position.x - playerWidth, playerTransform.position.y + playerHeight + speed, playerTransform.position.z + playerWidth)) && (!LeftBlocked && !FrontBlocked)) ||
            (world.CheckForVoxel(new Vector3(playerTransform.position.x + playerWidth, playerTransform.position.y + playerHeight + speed, playerTransform.position.z - playerWidth)) && (!RightBlocked && !BackBlocked)) ||
            (world.CheckForVoxel(new Vector3(playerTransform.position.x + playerWidth, playerTransform.position.y + playerHeight + speed, playerTransform.position.z + playerWidth)) && (!RightBlocked && !FrontBlocked)))
        {
            return 0;
        }
        else
        {
            return speed;
        }
    }

    private void RotatePlayer()
    {
        playerTransform.Rotate(Vector3.up * mouseHorizontal * world.settings.mouseSensitivity);
    }

    private void RotateCamera()
    {
        yRotation += mouseVertical * world.settings.mouseSensitivity;

        yRotation = Mathf.Clamp(yRotation, -90, 90);

        cameraTransform.localRotation = Quaternion.Euler(-yRotation, 0, 0);
    }
}
