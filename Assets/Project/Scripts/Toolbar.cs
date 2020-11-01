using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Toolbar : MonoBehaviour
{
    //private World world;
    public Player player;

    public RectTransform highlight;
    public UIItemSlot[] slots;

    public int slotIndex = 0;

    public void Start()
    {
        byte index = 1;

        foreach (var s in slots)
        {
            ItemStack stack = new ItemStack(index, Random.Range(1, 65));
            ItemSlot slot = new ItemSlot(s, stack);
            index++;
        }


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

            if (slotIndex > slots.Length - 1)
                slotIndex = 0;
            else if (slotIndex < 0)
                slotIndex = slots.Length - 1;

            // might be inefficient as it calls .transform on icon
            highlight.position = slots[slotIndex].slotIcon.transform.position;
        }
    }
}
