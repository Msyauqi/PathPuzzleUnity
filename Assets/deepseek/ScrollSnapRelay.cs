using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class ScrollSnapRelay : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IPointerUpHandler, ICancelHandler
{
    public HomeMenuPager pager;
    public bool blockSwipeWhenDraggingSlider = true;

    private bool isBlockingSwipeThisDrag;
    private bool cachedHorizontalState = true;

    void Awake()
    {
        if (pager != null) return;

        pager = GetComponentInParent<HomeMenuPager>();
        if (pager != null) return;

        pager = FindObjectOfType<HomeMenuPager>(true);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (ShouldBlockSwipe(eventData))
        {
            BeginBlockSwipe();
            return;
        }

        if (pager != null)
            pager.OnBeginDrag();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isBlockingSwipeThisDrag)
        {
            EndBlockSwipe();
            return;
        }

        if (pager != null)
            pager.OnEndDrag();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        EndBlockSwipe();
    }

    public void OnCancel(BaseEventData eventData)
    {
        EndBlockSwipe();
    }

    void OnDisable()
    {
        EndBlockSwipe();
    }

    bool ShouldBlockSwipe(PointerEventData eventData)
    {
        if (!blockSwipeWhenDraggingSlider || eventData == null) return false;
        if (pager == null || pager.scrollRect == null) return false;

        // Strong check: raycast every UI under pointer position.
        // This catches cases where ScrollRect receives drag event first.
        if (EventSystem.current != null)
        {
            var raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);
            for (int i = 0; i < raycastResults.Count; i++)
            {
                GameObject hit = raycastResults[i].gameObject;
                if (hit == null) continue;

                if (hit.GetComponentInParent<Slider>() != null)
                    return true;

                if (hit.GetComponentInParent<Scrollbar>() != null)
                    return true;
            }
        }

        // Fallback check: detect pointer against slider rects directly.
        Slider[] sliders = pager.scrollRect.GetComponentsInChildren<Slider>(true);
        Camera eventCamera = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
        for (int i = 0; i < sliders.Length; i++)
        {
            Slider slider = sliders[i];
            if (slider == null || !slider.gameObject.activeInHierarchy || !slider.interactable) continue;

            RectTransform sliderRect = slider.transform as RectTransform;
            if (sliderRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(sliderRect, eventData.position, eventCamera))
                return true;

            if (slider.handleRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(slider.handleRect, eventData.position, eventCamera))
                return true;
        }

        GameObject target =
            eventData.pointerPressRaycast.gameObject ??
            eventData.pointerCurrentRaycast.gameObject ??
            eventData.pointerPress ??
            eventData.pointerEnter;

        if (target == null) return false;

        if (target.GetComponentInParent<Slider>() != null)
            return true;

        if (target.GetComponentInParent<Scrollbar>() != null)
            return true;

        return false;
    }

    void BeginBlockSwipe()
    {
        if (isBlockingSwipeThisDrag) return;
        if (pager == null || pager.scrollRect == null) return;

        isBlockingSwipeThisDrag = true;
        cachedHorizontalState = pager.scrollRect.horizontal;
        pager.scrollRect.horizontal = false;
        pager.scrollRect.StopMovement();
    }

    void EndBlockSwipe()
    {
        if (!isBlockingSwipeThisDrag) return;

        if (pager != null && pager.scrollRect != null)
            pager.scrollRect.horizontal = cachedHorizontalState;

        isBlockingSwipeThisDrag = false;
    }
}
