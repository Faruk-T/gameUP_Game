//The implementation is based on this article:http://rbarraza.com/html5-canvas-pageflip/
//As the rbarraza.com website is not live anymore you can get an archived version from web archive 
//or check an archived version that I uploaded on my website: https://dandarawy.com/html5-canvas-pageflip/

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
public enum FlipMode
{
    RightToLeft,
    LeftToRight
}
[ExecuteInEditMode]
public class Book : MonoBehaviour {
    [System.Serializable]
    public class DailyPages
    {
        [Min(1)] public int day = 1;
        public Sprite element1;
        public Sprite element2;
    }

    public Canvas canvas;
    [SerializeField]
    RectTransform BookPanel;
    public Sprite background;
    public Sprite[] bookPages;
    [Header("Daily shift pages (Book Pages elements 1 and 2)")]
    [SerializeField, Min(1)] int currentDay = 1;
    [SerializeField] DailyPages[] dailyPages = new DailyPages[0];
    public int CurrentDay { get { return currentDay; } }
    DailyPages pendingDay;
    public bool interactable=true;
    public bool enableShadowEffect=true;
    //represent the index of the sprite shown in the right page
    public int currentPage = 0;
    public int TotalPageCount
    {
        get { return bookPages.Length; }
    }
    public Vector3 EndBottomLeft
    {
        get { return ebl; }
    }
    public Vector3 EndBottomRight
    {
        get { return ebr; }
    }
    public float Height
    {
        get
        {
            return BookPanel.rect.height ; 
        }
    }
    public Image ClippingPlane;
    public Image NextPageClip;
    public Image Shadow;
    public Image ShadowLTR;
    public Image Left;
    public Image LeftNext;
    public Image Right;
    public Image RightNext;
    public UnityEvent OnFlip;
    float radius1, radius2;
    //Spine Bottom
    Vector3 sb;
    //Spine Top
    Vector3 st;
    //corner of the page
    Vector3 c;
    //Edge Bottom Right
    Vector3 ebr;
    //Edge Bottom Left
    Vector3 ebl;
    //follow point 
    Vector3 f;
    bool pageDragging = false;
    // Keep the initiating pointer so another finger/device cannot take over the curl.
    PointerEventData dragPointer;
    //current flip mode
    FlipMode mode;
    PageTransformState[] initialPageTransforms;
    Transform[] initialPanelChildren;
    bool visualResetPending;

    // Curling reparents and rotates these UI elements. Keep their resting layout
    // so closing the book halfway through a drag also restores it correctly.
    class PageTransformState
    {
        readonly RectTransform rect;
        readonly Transform parent;
        readonly int siblingIndex;
        readonly Vector2 anchorMin, anchorMax, pivot, sizeDelta;
        readonly Vector3 anchoredPosition, localScale;
        readonly Quaternion localRotation;

        public PageTransformState(RectTransform rect)
        {
            this.rect = rect;
            parent = rect.parent;
            siblingIndex = rect.GetSiblingIndex();
            anchorMin = rect.anchorMin;
            anchorMax = rect.anchorMax;
            pivot = rect.pivot;
            sizeDelta = rect.sizeDelta;
            anchoredPosition = rect.anchoredPosition3D;
            localScale = rect.localScale;
            localRotation = rect.localRotation;
        }

        public void Restore()
        {
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition3D = anchoredPosition;
            rect.localScale = localScale;
            rect.localRotation = localRotation;
        }

        public void RestoreSiblingIndex()
        {
            rect.SetSiblingIndex(siblingIndex);
        }
    }

    void OnEnable()
    {
        if (!Application.isPlaying) return;
        ResetBook();
        RefreshDayFromManager();
    }

    void OnDisable()
    {
        if (!Application.isPlaying) return;
        ResetBook();
    }

    /// <summary>Read the latest game day whenever the book is opened.</summary>
    void RefreshDayFromManager()
    {
        // PSEUDO KOD: Burayi oyunun gun manager'i hazir oldugunda bagla.
        // int oyunGunu = GameManager.Instance.CurrentDay;
        // SetDay(oyunGunu);
        // return; // Asagidaki Inspector yedegini calistirma.

        // Manager baglanana kadar Inspector'daki gunu kullan.
        if (dailyPages != null && dailyPages.Length > 0)
            SetDay(currentDay);
    }

    /// <summary>Cancel a flip and return to the first page without firing OnFlip.</summary>
    public void ResetBook()
    {
        StopAllCoroutines();
        currentCoroutine = null;
        pageDragging = false;
        dragPointer = null;
        pendingDay = null;
        currentPage = 0;
        f = c = Vector3.zero;
        mode = FlipMode.RightToLeft;

        // OnEnable/OnDisable can run while Unity is traversing this hierarchy.
        // Reparenting, sibling changes and child activation wait until LateUpdate.
        visualResetPending = true;
    }

    void LateUpdate()
    {
        if (!Application.isPlaying || !visualResetPending) return;
        RestoreFirstPageVisuals();
    }

    void RestoreFirstPageVisuals()
    {
        visualResetPending = false;

        if (initialPageTransforms != null)
        {
            foreach (var state in initialPageTransforms) state.Restore();
            foreach (var state in initialPageTransforms) state.RestoreSiblingIndex();
            // Hotspots are not curl images, but must retain their original order too.
            for (int i = 0; i < initialPanelChildren.Length; i++)
                initialPanelChildren[i].SetSiblingIndex(i);
        }
        if (Left != null) Left.gameObject.SetActive(false);
        if (Right != null) Right.gameObject.SetActive(false);
        if (Shadow != null) Shadow.gameObject.SetActive(false);
        if (ShadowLTR != null) ShadowLTR.gameObject.SetActive(false);
        if (LeftNext != null && RightNext != null && bookPages != null)
            UpdateSprites();
    }

    void Start()
    {
        if (!canvas) canvas=GetComponentInParent<Canvas>();
        if (!canvas) Debug.LogError("Book should be a child to canvas");

        Left.gameObject.SetActive(false);
        Right.gameObject.SetActive(false);
        UpdateSprites();
        CalcCurlCriticalPoints();

        float pageWidth = BookPanel.rect.width / 2.0f;
        float pageHeight = BookPanel.rect.height;
        NextPageClip.rectTransform.sizeDelta = new Vector2(pageWidth, pageHeight + pageHeight * 2);


        ClippingPlane.rectTransform.sizeDelta = new Vector2(pageWidth * 2 + pageHeight, pageHeight + pageHeight * 2);

        //hypotenous (diagonal) page length
        float hyp = Mathf.Sqrt(pageWidth * pageWidth + pageHeight * pageHeight);
        float shadowPageHeight = pageWidth / 2 + hyp;

        Shadow.rectTransform.sizeDelta = new Vector2(pageWidth, shadowPageHeight);
        Shadow.rectTransform.pivot = new Vector2(1, (pageWidth / 2) / shadowPageHeight);

        ShadowLTR.rectTransform.sizeDelta = new Vector2(pageWidth, shadowPageHeight);
        ShadowLTR.rectTransform.pivot = new Vector2(0, (pageWidth / 2) / shadowPageHeight);

        if (Application.isPlaying)
        {
            initialPanelChildren = new Transform[BookPanel.childCount];
            for (int i = 0; i < initialPanelChildren.Length; i++)
                initialPanelChildren[i] = BookPanel.GetChild(i);
            var images = new[] { Left, Right, LeftNext, RightNext, ClippingPlane, NextPageClip, Shadow, ShadowLTR };
            // Restore sibling indices in their original order, after all parents.
            System.Array.Sort(images, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            initialPageTransforms = System.Array.ConvertAll(images, image => new PageTransformState(image.rectTransform));
            ResetBook();
        }

    }

    private void CalcCurlCriticalPoints()
    {
        sb = new Vector3(0, -BookPanel.rect.height / 2);
        ebr = new Vector3(BookPanel.rect.width / 2, -BookPanel.rect.height / 2);
        ebl = new Vector3(-BookPanel.rect.width / 2, -BookPanel.rect.height / 2);
        st = new Vector3(0, BookPanel.rect.height / 2);
        radius1 = Vector2.Distance(sb, ebr);
        float pageWidth = BookPanel.rect.width / 2.0f;
        float pageHeight = BookPanel.rect.height;
        radius2 = Mathf.Sqrt(pageWidth * pageWidth + pageHeight * pageHeight);
    }

    public Vector3 transformPoint(Vector3 mouseScreenPos)
    {
        Camera eventCamera = null;
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = dragPointer != null ? dragPointer.pressEventCamera : canvas.worldCamera;
        if (eventCamera == null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = Camera.main;

        Vector2 localPos;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            BookPanel, mouseScreenPos, eventCamera, out localPos) ? (Vector3)localPos : f;
    }
    void Update()
    {
        if (visualResetPending) return;
        if (pendingDay != null && !pageDragging && currentCoroutine == null)
            ApplyDay(pendingDay);

        if (pageDragging && interactable)
        {
            UpdateBook();
        }
    }

    /// <summary>Called by the game's day manager or a UnityEvent. Days start at 1.</summary>
    public void SetDay(int day)
    {
        TrySetDay(day);
    }

    /// <summary>Returns false without changing the book if the day is not configured.</summary>
    public bool TrySetDay(int day)
    {
        DailyPages selected = null;
        if (dailyPages != null)
        {
            foreach (var entry in dailyPages)
            {
                if (entry == null || entry.day != day) continue;
                if (selected != null)
                {
                    Debug.LogWarning("Book: duplicate configuration for day " + day, this);
                    return false;
                }
                selected = entry;
            }
        }
        if (day < 1 || selected == null || selected.element1 == null || selected.element2 == null ||
            bookPages == null || bookPages.Length < 3)
        {
            Debug.LogWarning("Book: day " + day + " requires two sprites and at least three Book Pages.", this);
            return false;
        }

        // Keep both sides of an ongoing flip on the same day's content.
        // Multiple requests during a flip resolve to the most recently requested day.
        pendingDay = selected;
        if (!pageDragging && currentCoroutine == null)
            ApplyDay(selected);
        return true;
    }

    void ApplyDay(DailyPages selected)
    {
        bookPages[1] = selected.element1;
        bookPages[2] = selected.element2;
        currentDay = selected.day;
        pendingDay = null;
        if (LeftNext != null && RightNext != null)
            UpdateSprites();
    }
    public void UpdateBook()
    {
        Vector2 screenPosition;
        if (dragPointer != null)
            screenPosition = dragPointer.position;
        else if (!TryGetPointerPosition(out screenPosition))
            return;

        f = Vector3.Lerp(f, transformPoint(screenPosition), Time.deltaTime * 10);
        if (mode == FlipMode.RightToLeft)
            UpdateBookRTLToPoint(f);
        else
            UpdateBookLTRToPoint(f);
    }
    public void UpdateBookLTRToPoint(Vector3 followLocation)
    {
        mode = FlipMode.LeftToRight;
        f = followLocation;
        ShadowLTR.transform.SetParent(ClippingPlane.transform, true);
        ShadowLTR.transform.localPosition = new Vector3(0, 0, 0);
        ShadowLTR.transform.localEulerAngles = new Vector3(0, 0, 0);
        Left.transform.SetParent(ClippingPlane.transform, true);

        Right.transform.SetParent(BookPanel.transform, true);
        Right.transform.localEulerAngles = Vector3.zero;
        LeftNext.transform.SetParent(BookPanel.transform, true);

        c = Calc_C_Position(followLocation);
        Vector3 t1;
        float clipAngle = CalcClipAngle(c, ebl, out t1);
        //0 < T0_T1_Angle < 180
        clipAngle = (clipAngle + 180) % 180;

        ClippingPlane.transform.localEulerAngles = new Vector3(0, 0, clipAngle - 90);
        ClippingPlane.transform.position = BookPanel.TransformPoint(t1);

        //page position and angle
        Left.transform.position = BookPanel.TransformPoint(c);
        float C_T1_dy = t1.y - c.y;
        float C_T1_dx = t1.x - c.x;
        float C_T1_Angle = Mathf.Atan2(C_T1_dy, C_T1_dx) * Mathf.Rad2Deg;
        Left.transform.localEulerAngles = new Vector3(0, 0, C_T1_Angle - 90 - clipAngle);

        NextPageClip.transform.localEulerAngles = new Vector3(0, 0, clipAngle - 90);
        NextPageClip.transform.position = BookPanel.TransformPoint(t1);
        LeftNext.transform.SetParent(NextPageClip.transform, true);
        Right.transform.SetParent(ClippingPlane.transform, true);
        Right.transform.SetAsFirstSibling();

        ShadowLTR.rectTransform.SetParent(Left.rectTransform, true);
    }
    public void UpdateBookRTLToPoint(Vector3 followLocation)
    {
        mode = FlipMode.RightToLeft;
        f = followLocation;
        Shadow.transform.SetParent(ClippingPlane.transform, true);
        Shadow.transform.localPosition = Vector3.zero;
        Shadow.transform.localEulerAngles = Vector3.zero;
        Right.transform.SetParent(ClippingPlane.transform, true);

        Left.transform.SetParent(BookPanel.transform, true);
        Left.transform.localEulerAngles = Vector3.zero;
        RightNext.transform.SetParent(BookPanel.transform, true);
        c = Calc_C_Position(followLocation);
        Vector3 t1;
        float clipAngle = CalcClipAngle(c, ebr, out t1);
        if (clipAngle > -90) clipAngle += 180;

        ClippingPlane.rectTransform.pivot = new Vector2(1, 0.35f);
        ClippingPlane.transform.localEulerAngles = new Vector3(0, 0, clipAngle + 90);
        ClippingPlane.transform.position = BookPanel.TransformPoint(t1);

        //page position and angle
        Right.transform.position = BookPanel.TransformPoint(c);
        float C_T1_dy = t1.y - c.y;
        float C_T1_dx = t1.x - c.x;
        float C_T1_Angle = Mathf.Atan2(C_T1_dy, C_T1_dx) * Mathf.Rad2Deg;
        Right.transform.localEulerAngles = new Vector3(0, 0, C_T1_Angle - (clipAngle + 90));

        NextPageClip.transform.localEulerAngles = new Vector3(0, 0, clipAngle + 90);
        NextPageClip.transform.position = BookPanel.TransformPoint(t1);
        RightNext.transform.SetParent(NextPageClip.transform, true);
        Left.transform.SetParent(ClippingPlane.transform, true);
        Left.transform.SetAsFirstSibling();

        Shadow.rectTransform.SetParent(Right.rectTransform, true);
    }
    private float CalcClipAngle(Vector3 c,Vector3 bookCorner,out  Vector3 t1)
    {
        Vector3 t0 = (c + bookCorner) / 2;
        float T0_CORNER_dy = bookCorner.y - t0.y;
        float T0_CORNER_dx = bookCorner.x - t0.x;
        float T0_CORNER_Angle = Mathf.Atan2(T0_CORNER_dy, T0_CORNER_dx);
        float T0_T1_Angle = 90 - T0_CORNER_Angle;
        
        float T1_X = t0.x - T0_CORNER_dy * Mathf.Tan(T0_CORNER_Angle);
        T1_X = normalizeT1X(T1_X, bookCorner, sb);
        t1 = new Vector3(T1_X, sb.y, 0);
        
        //clipping plane angle=T0_T1_Angle
        float T0_T1_dy = t1.y - t0.y;
        float T0_T1_dx = t1.x - t0.x;
        T0_T1_Angle = Mathf.Atan2(T0_T1_dy, T0_T1_dx) * Mathf.Rad2Deg;
        return T0_T1_Angle;
    }
    private float normalizeT1X(float t1,Vector3 corner,Vector3 sb)
    {
        if (t1 > sb.x && sb.x > corner.x)
            return sb.x;
        if (t1 < sb.x && sb.x < corner.x)
            return sb.x;
        return t1;
    }
    private Vector3 Calc_C_Position(Vector3 followLocation)
    {
        Vector3 c;
        f = followLocation;
        float F_SB_dy = f.y - sb.y;
        float F_SB_dx = f.x - sb.x;
        float F_SB_Angle = Mathf.Atan2(F_SB_dy, F_SB_dx);
        Vector3 r1 = new Vector3(radius1 * Mathf.Cos(F_SB_Angle),radius1 * Mathf.Sin(F_SB_Angle), 0) + sb;

        float F_SB_distance = Vector2.Distance(f, sb);
        if (F_SB_distance < radius1)
            c = f;
        else
            c = r1;
        float F_ST_dy = c.y - st.y;
        float F_ST_dx = c.x - st.x;
        float F_ST_Angle = Mathf.Atan2(F_ST_dy, F_ST_dx);
        Vector3 r2 = new Vector3(radius2 * Mathf.Cos(F_ST_Angle),
           radius2 * Mathf.Sin(F_ST_Angle), 0) + st;
        float C_ST_distance = Vector2.Distance(c, st);
        if (C_ST_distance > radius2)
            c = r2;
        return c;
    }
    public void DragRightPageToPoint(Vector3 point)
    {
        if (currentPage >= bookPages.Length) return;
        pageDragging = true;
        mode = FlipMode.RightToLeft;
        f = point;


        NextPageClip.rectTransform.pivot = new Vector2(0, 0.12f);
        ClippingPlane.rectTransform.pivot = new Vector2(1, 0.35f);

        Left.gameObject.SetActive(true);
        Left.rectTransform.pivot = new Vector2(0, 0);
        Left.transform.position = RightNext.transform.position;
        Left.transform.eulerAngles = new Vector3(0, 0, 0);
        Left.sprite = (currentPage < bookPages.Length) ? bookPages[currentPage] : background;
        Left.transform.SetAsFirstSibling();
        
        Right.gameObject.SetActive(true);
        Right.transform.position = RightNext.transform.position;
        Right.transform.eulerAngles = new Vector3(0, 0, 0);
        Right.sprite = (currentPage < bookPages.Length - 1) ? bookPages[currentPage + 1] : background;

        RightNext.sprite = (currentPage < bookPages.Length - 2) ? bookPages[currentPage + 2] : background;

        LeftNext.transform.SetAsFirstSibling();
        if (enableShadowEffect) Shadow.gameObject.SetActive(true);
        UpdateBookRTLToPoint(f);
    }
    public void OnMouseDragRightPage()
    {
        Vector2 screenPosition;
        if (interactable && TryGetPointerPosition(out screenPosition))
            DragRightPageToPoint(transformPoint(screenPosition));
        
    }
    public void DragLeftPageToPoint(Vector3 point)
    {
        if (currentPage <= 0) return;
        pageDragging = true;
        mode = FlipMode.LeftToRight;
        f = point;

        NextPageClip.rectTransform.pivot = new Vector2(1, 0.12f);
        ClippingPlane.rectTransform.pivot = new Vector2(0, 0.35f);

        Right.gameObject.SetActive(true);
        Right.transform.position = LeftNext.transform.position;
        Right.sprite = bookPages[currentPage - 1];
        Right.transform.eulerAngles = new Vector3(0, 0, 0);
        Right.transform.SetAsFirstSibling();

        Left.gameObject.SetActive(true);
        Left.rectTransform.pivot = new Vector2(1, 0);
        Left.transform.position = LeftNext.transform.position;
        Left.transform.eulerAngles = new Vector3(0, 0, 0);
        Left.sprite = (currentPage >= 2) ? bookPages[currentPage - 2] : background;

        LeftNext.sprite = (currentPage >= 3) ? bookPages[currentPage - 3] : background;

        RightNext.transform.SetAsFirstSibling();
        if (enableShadowEffect) ShadowLTR.gameObject.SetActive(true);
        UpdateBookLTRToPoint(f);
    }
    public void OnMouseDragLeftPage()
    {
        Vector2 screenPosition;
        if (interactable && TryGetPointerPosition(out screenPosition))
            DragLeftPageToPoint(transformPoint(screenPosition));
        
    }
    public void OnMouseRelease()
    {
        if (interactable)
            ReleasePage();
    }

    // Dynamic EventTrigger callbacks supplied by InputSystemUIInputModule.
    public void OnBeginDragRightPage(BaseEventData eventData)
    {
        BeginPointerDrag(eventData, FlipMode.RightToLeft);
    }

    public void OnBeginDragLeftPage(BaseEventData eventData)
    {
        BeginPointerDrag(eventData, FlipMode.LeftToRight);
    }

    void BeginPointerDrag(BaseEventData eventData, FlipMode flipMode)
    {
        var pointer = eventData as PointerEventData;
        if (!interactable || pageDragging || pointer == null ||
            pointer.button != PointerEventData.InputButton.Left)
            return;
        if (flipMode == FlipMode.RightToLeft ? currentPage >= bookPages.Length : currentPage <= 0)
            return;

        dragPointer = pointer;
        if (flipMode == FlipMode.RightToLeft)
            DragRightPageToPoint(transformPoint(pointer.position));
        else
            DragLeftPageToPoint(transformPoint(pointer.position));
    }

    public void OnEndDragPage(BaseEventData eventData)
    {
        var pointer = eventData as PointerEventData;
        if (dragPointer == null || pointer == null || pointer.pointerId != dragPointer.pointerId ||
            pointer.button != PointerEventData.InputButton.Left)
            return;

        // Use the release position even when the pointer crossed the book in one frame.
        if (mode == FlipMode.RightToLeft)
            UpdateBookRTLToPoint(transformPoint(pointer.position));
        else
            UpdateBookLTRToPoint(transformPoint(pointer.position));
        ReleasePage();
    }

    static bool TryGetPointerPosition(out Vector2 position)
    {
        // Retain the public, parameterless callbacks for existing custom UnityEvents.
        var pointer = Pointer.current;
        position = pointer != null ? pointer.position.ReadValue() : Vector2.zero;
        return pointer != null;
    }

    public void ReleasePage()
    {
        dragPointer = null;
        if (pageDragging)
        {
            pageDragging = false;
            float distanceToLeft = Vector2.Distance(c, ebl);
            float distanceToRight = Vector2.Distance(c, ebr);
            if (distanceToRight < distanceToLeft && mode == FlipMode.RightToLeft)
                TweenBack();
            else if (distanceToRight > distanceToLeft && mode == FlipMode.LeftToRight)
                TweenBack();
            else
                TweenForward();
        }
    }
    Coroutine currentCoroutine;
    void UpdateSprites()
    {
        LeftNext.sprite= (currentPage > 0 && currentPage <= bookPages.Length) ? bookPages[currentPage-1] : background;
        RightNext.sprite=(currentPage>=0 &&currentPage<bookPages.Length) ? bookPages[currentPage] : background;
    }
    public void TweenForward()
    {
        if(mode== FlipMode.RightToLeft)
        currentCoroutine = StartCoroutine(TweenTo(ebl, 0.15f, () => { Flip(); }));
        else
        currentCoroutine = StartCoroutine(TweenTo(ebr, 0.15f, () => { Flip(); }));
    }
    void Flip()
    {
        if (mode == FlipMode.RightToLeft)
            currentPage += 2;
        else
            currentPage -= 2;
        LeftNext.transform.SetParent(BookPanel.transform, true);
        Left.transform.SetParent(BookPanel.transform, true);
        LeftNext.transform.SetParent(BookPanel.transform, true);
        Left.gameObject.SetActive(false);
        Right.gameObject.SetActive(false);
        Right.transform.SetParent(BookPanel.transform, true);
        RightNext.transform.SetParent(BookPanel.transform, true);
        UpdateSprites();
        Shadow.gameObject.SetActive(false);
        ShadowLTR.gameObject.SetActive(false);
        if (OnFlip != null)
            OnFlip.Invoke();
    }
    public void TweenBack()
    {
        if (mode == FlipMode.RightToLeft)
        {
            currentCoroutine = StartCoroutine(TweenTo(ebr,0.15f,
                () =>
                {
                    UpdateSprites();
                    RightNext.transform.SetParent(BookPanel.transform);
                    Right.transform.SetParent(BookPanel.transform);

                    Left.gameObject.SetActive(false);
                    Right.gameObject.SetActive(false);
                    pageDragging = false;
                }
                ));
        }
        else
        {
            currentCoroutine = StartCoroutine(TweenTo(ebl, 0.15f,
                () =>
                {
                    UpdateSprites();

                    LeftNext.transform.SetParent(BookPanel.transform);
                    Left.transform.SetParent(BookPanel.transform);

                    Left.gameObject.SetActive(false);
                    Right.gameObject.SetActive(false);
                    pageDragging = false;
                }
                ));
        }
    }
    public IEnumerator TweenTo(Vector3 to, float duration, System.Action onFinish)
    {
        int steps = (int)(duration / 0.025f);
        Vector3 displacement = (to - f) / steps;
        for (int i = 0; i < steps-1; i++)
        {
            if(mode== FlipMode.RightToLeft)
            UpdateBookRTLToPoint( f + displacement);
            else
                UpdateBookLTRToPoint(f + displacement);

            yield return new WaitForSeconds(0.025f);
        }
        if (onFinish != null)
            onFinish();
        currentCoroutine = null;
    }
}
