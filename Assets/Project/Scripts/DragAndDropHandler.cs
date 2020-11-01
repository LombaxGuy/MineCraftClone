using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DragAndDropHandler : MonoBehaviour
{
    [SerializeField] private UIItemSlot cursorSlot = null;
    private ItemSlot cursorItemSlot;

    [SerializeField] private GraphicRaycaster raycaster = null;
    private PointerEventData pointerEventData;
    [SerializeField] private EventSystem eventSystem = null;

    private World world;

    private void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();

        cursorItemSlot = new ItemSlot(cursorSlot);
    }

    private void Update()
    {
        // if we are not in the UI, return
        if (!world.InUI)
            return;

        cursorSlot.transform.position = Input.mousePosition;

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            HandleSlotClick(CheckForSlot());
        }
    }

    private void HandleSlotClick(UIItemSlot clickedSlot)
    {
        // if we didn't click on any slot, return
        if (clickedSlot == null)
            return;

        // if we don't have any items in the cursor slot and there is nothing in the clicked slot, return
        if (!cursorSlot.HasItem && !clickedSlot.HasItem)
            return;

        if (clickedSlot.itemSlot.isCreative)
        {
            cursorItemSlot.ClearSlot();
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.stack);
            return;
        }

        // grab item stack into cursor slot, then return
        if (!cursorSlot.HasItem && clickedSlot.HasItem)
        {
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.TakeAll());
            return;
        }

        // place item stack into empty inv slot, then return
        if (cursorSlot.HasItem && !clickedSlot.HasItem)
        {
            clickedSlot.itemSlot.InsertStack(cursorItemSlot.TakeAll());
            return;
        }

        // if there is something in both inv slot and cursor slot
        if (cursorSlot.HasItem && clickedSlot.HasItem)
        {
            // if item id's are different, swap stacks
            if (cursorSlot.itemSlot.stack.id != clickedSlot.itemSlot.stack.id)
            {
                ItemStack oldCursorSlot = cursorSlot.itemSlot.TakeAll();
                ItemStack oldInvSlot = clickedSlot.itemSlot.TakeAll();

                clickedSlot.itemSlot.InsertStack(oldCursorSlot);
                cursorSlot.itemSlot.InsertStack(oldInvSlot);
            }

            // if item id is identical, merge stacks then return
            // NEEDS IMPLEMENTATION
            // REQUIRES STACK SIZE VARIABLE
        }
    }

    private UIItemSlot CheckForSlot()
    {
        pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerEventData, results);

        foreach (var res in results)
        {
            if (res.gameObject.CompareTag("UIItemSlot"))
            {
                return res.gameObject.GetComponent<UIItemSlot>();
            }
        }

        return null;
    }
}
