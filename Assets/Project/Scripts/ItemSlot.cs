using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

[System.Serializable]
public class ItemSlot
{
    public ItemStack stack = null;
    private UIItemSlot uiItemSlot = null;

    public bool isCreative;

    public ItemSlot (UIItemSlot uiItemSlot)
    {
        stack = null;
        this.uiItemSlot = uiItemSlot;
        this.uiItemSlot.Link(this);
    }

    public ItemSlot(UIItemSlot uiItemSlot, ItemStack stack)
    {
        this.stack = stack;
        this.uiItemSlot = uiItemSlot;
        this.uiItemSlot.Link(this);
    }

    public void LinkUISlot(UIItemSlot uiSlot)
    {
        uiItemSlot = uiSlot;
    }

    public void UnlinkUISlot()
    {
        uiItemSlot = null;
    }

    public void ClearSlot()
    {
        stack = null;

        if (uiItemSlot != null)
            uiItemSlot.UpdateSlot();
    }

    public int Take(int amount)
    {
        if (amount > stack.amount)
        {
            int stackAmount = stack.amount;
            ClearSlot();

            return stackAmount;
        }
        else if (amount < stack.amount)
        {
            stack.amount -= amount;
            uiItemSlot.UpdateSlot();
            return amount;
        }
        else
        {
            ClearSlot();
            return amount;
        }
    }

    public ItemStack TakeAll()
    {
        ItemStack takenStack = new ItemStack(stack.id, stack.amount);
        ClearSlot();

        return takenStack;
    }

    public void InsertStack(ItemStack stack)
    {
        this.stack = stack;
        uiItemSlot.UpdateSlot();
    }

    public bool HasItem
    {
        get
        {
            if (stack != null)
                return true;
            else
                return false;
        }
    }
}
