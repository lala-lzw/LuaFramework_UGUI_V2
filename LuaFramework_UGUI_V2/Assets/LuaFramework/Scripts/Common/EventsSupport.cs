using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/*  其它事件可根据需要在此类中实现
8     IPointerEnterHandler - OnPointerEnter - Called when a pointer enters the object
9     IPointerExitHandler - OnPointerExit - Called when a pointer exits the object
10     IPointerDownHandler - OnPointerDown - Called when a pointer is pressed on the object
11     IPointerUpHandler - OnPointerUp - Called when a pointer is released (called on the original the pressed object)
12     IPointerClickHandler - OnPointerClick - Called when a pointer is pressed and released on the same object
13     IInitializePotentialDragHandler - OnInitializePotentialDrag - Called when a drag target is found, can be used to initialise values
14     IBeginDragHandler - OnBeginDrag - Called on the drag object when dragging is about to begin
15     IDragHandler - OnDrag - Called on the drag object when a drag is happening
16     IEndDragHandler - OnEndDrag - Called on the drag object when a drag finishes
17     IDropHandler - OnDrop - Called on the object where a drag finishes
18     IScrollHandler - OnScroll - Called when a mouse wheel scrolls
19     IUpdateSelectedHandler - OnUpdateSelected - Called on the selected object each tick
20     ISelectHandler - OnSelect - Called when the object becomes the selected object
21     IDeselectHandler - OnDeselect - Called on the selected object becomes deselected
22     IMoveHandler - OnMove - Called when a move event occurs (left, right, up, down, ect)
23     ISubmitHandler - OnSubmit - Called when the submit button is pressed
24     ICancelHandler - OnCancel - Called when the cancel button is pressed
25  */

/// <summary>
/// unity事件支持（本类用于实现Unity中的各种事件，借给Lua调用）
/// </summary>
public class EventsSupport : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    Action<PointerEventData> onPointerDownHandler = null;
    Action<PointerEventData> onPointerUpHandler = null;

    public void InitDownUpHandler(Action<PointerEventData> downHandler, Action<PointerEventData> upHandler)
    {
        onPointerDownHandler = downHandler;
        onPointerUpHandler = upHandler;
    }

    public void OnPointerDown(PointerEventData pointerEventData)
    {
        //Output the name of the GameObject that is being clicked
        //Debug.Log("[" + name + "] Game Object Click in Progress");

        if (onPointerDownHandler != null)
        {
            onPointerDownHandler(pointerEventData);
        }
    }

    //Detect if clicks are no longer registering
    public void OnPointerUp(PointerEventData pointerEventData)
    {
        //Debug.Log("[" +　name + "] No longer being clicked");
        if (onPointerUpHandler != null)
        {
            onPointerUpHandler(pointerEventData);
        }
    }
}
