using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Toolbar : MonoBehaviour
{
    private World world;
    public Player player;

    public RectTransform highlight;
    public ItemSlot[] itemSlots;

    private int slotIndex = 0;

    private void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();

        foreach (var slot in itemSlots)
        {
            slot.icon.sprite = world.blockTypes[slot.itemID].icon;
            slot.icon.enabled = true;
        }

        player.selectedBlockIndex = itemSlots[slotIndex].itemID;
    }

    public void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            if (scroll > 0)
                slotIndex--;
            else
                slotIndex++;

            if (slotIndex > itemSlots.Length - 1)
                slotIndex = 0;
            else if (slotIndex < 0)
                slotIndex = itemSlots.Length - 1;

            // might be inefficient as it calls .transform on icon
            highlight.position = itemSlots[slotIndex].icon.transform.position;
            player.selectedBlockIndex = itemSlots[slotIndex].itemID;
        }
    }
}
