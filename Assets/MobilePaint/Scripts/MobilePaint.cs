// Optimized Mobile Painter - Unitycoder.com

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace unitycoder_MobilePaint
{
    public enum DrawMode
    {
        Default,
        CustomBrush,
        FloodFill,
        Pattern,
        ShapeLines,
        Eraser
    }

    public enum EraserMode
    {
        Default,
        BackgroundColor
    }

    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class MobilePaint : MonoBehaviour
    {
        [Header("Mouse or Touch")]
        public bool enableTouch = false;

        [Space(10)]
        public LayerMask paintLayerMask = 1 << 0; // to which layer our paint canvas is at (used in raycast)

        public bool createCanvasMesh = true; // default canvas is full screen quad, if disabled existing mesh is used

        public RectTransform referenceArea; // we will match the size of this reference object
        private float canvasScaleFactor = 1; // canvas scaling factor (will be taken from Canvas)

        [Header("Brush Settings")]
        public bool connectBrushStokes = true; // if brush moves too fast, then connect them with line. NOTE! Disable this if you are painting to custom mesh

        //	*** Default settings ***
        public Color32 paintColor = new Color32(255, 0, 0, 255);



        public int brushSize = 24; // default brush size
        public int brushSizeMin = 1; // default min brush size
        public int brushSizeMax = 64; // default max brush size

        // cached calculations
        public bool hiQualityBrush = false; // Draw more brush strokes when moving NOTE: this is slow on mobiles!
        private int brushSizeX1 = 48; // << 1
        private int brushSizeXbrushSize = 576; // x*x
        private int brushSizeX4 = 96; // << 2
        private int brushSizeDiv4 = 6; // >> 2 == /4


        public bool realTimeTexUpdate = true; // if set to true, ignore textureUpdateSpeed, and always update when textureNeedsUpdate gets set to true when drawing
        public float textureUpdateSpeed = 0.1f; // how often texture should be updated (0 = no delay, 1 = every one seconds)
        private float nextTextureUpdate = 0;

        public bool useAdditiveColors = false; // true = alpha adds up slowly, false = 1 click will instantly set alpha to brush or paint color alpha value

        public float brushAlphaStrength = 0.01f; // multiplier to soften brush additive alpha, 0.1f is nice & smooth, 1 = faster
        private float brushAlphaStrengthVal = 0.01f; // cached calculation
        private float alphaLerpVal = 0.1f;
        private float brushAlphaLerpVal = 0.1f;

        //		public bool DontPaintOverBlack = true; // so that outlines are reserved when painting

        [Header("Options")]
        public DrawMode drawMode = DrawMode.Default; // drawing modes: 0 = Default, 1 = custom brush, 2 = floodfill
        public bool useLockArea = false; // locking mask: only paint in area of the color that your click first
        public bool useMaskLayerOnly = false; // if true, only check pixels from mask layer, not from the painted texture
        public bool smoothenMaskEdges = false; // less white edges with mask
        public bool useThreshold = false;
        public byte paintThreshold = 128; // 0 = only exact match, 255 = match anything

        // ERASER
        [Space(10)]
        public EraserMode eraserMode = EraserMode.BackgroundColor;
        //private int defaultEraserSize = 32; // default fixed size for eraser


        // AREA FILL CALCULATIONS
        [Space(10)]
        public bool getAreaSize = false; // NOTE: to use this, someone has to listen the event AreaWasPainted (see scene "scene_MobilePaint_LockingMaskWithAreaCalculation")
        int initialX = 0;
        int initialY = 0;
        public delegate void AreaWasPainted(int fullArea, int filledArea, float percentageFilled, Vector3 point);
        public event AreaWasPainted AreaPaintedEvent;


        //private bool lockMaskCreated=false; //is lockmask already created for this click, not used yet
        private byte[] lockMaskPixels; // locking mask pixels


        public bool canDrawOnBlack = true; // to stop filling on mask black lines, FIXME: not working if its not pure black..


        //public bool drawAfterFill = true; // TODO: return to drawing mode after first fill?

        public Vector2 canvasSizeAdjust = new Vector2(0, 0); // this means, "ScreenResolution.xy+screenSizeAdjust.xy" (use only minus values, to add un-drawable border on right or bottom)
        public string targetTexture = "_MainTex"; // target texture for this material shader (usually _MainTex)
        public FilterMode filterMode = FilterMode.Point;

        // canvas clear color
        public Color32 clearColor = new Color32(255, 255, 255, 255);

        [Header("Mask/Overlay")]
        public bool useMaskImage = false;
        public Texture2D maskTex;

        [Header("Custom Brushes")]
        public bool useCustomBrushes = false;
        public Texture2D[] customBrushes;
        public bool overrideCustomBrushColor = false; // uses paint color instead of brush texture color
        public bool useCustomBrushAlpha = true; // true = use alpha from brush, false = use alpha from current paint color
        public int selectedBrush = 0; // currently selected brush index

        //private Color[] customBrushPixels;
        private byte[] customBrushBytes;
        private int customBrushWidth;
        private int customBrushHeight;
        private int customBrushWidthHalf;
        //		private int customBrushHeightHalf;
        private int texWidthMinusCustomBrushWidth;
        private int texHeightMinusCustomBrushHeight;

        [Header("Custom Patterns")]
        public bool useCustomPatterns = false;
        public Texture2D[] customPatterns;
        private byte[] patternBrushBytes;
        private int customPatternWidth;
        private int customPatternHeight;
        public int selectedPattern = 0;

        [Header("Line Drawing")]
        public Transform previewLineCircle;
        Transform previewLineCircleStart; // clone for start of circle
        Transform previewLineCircleEnd; // clone for end of circle
        bool haveStartedLine = false;
        int firstClickX = 0;
        int firstClickY = 0;
        LineRenderer lineRenderer;
        public bool snapLinesToGrid = false; // while drawing lines
        public int gridResolution = 128;
        int gridSize = 10;

        // for old GUIScaling
        private float scaleAdjust = 1.0f;
        private const float BASE_WIDTH = 800;
        private const float BASE_HEIGHT = 480;

        //	*** private variables, no need to touch ***
        private byte[] pixels; // byte array for texture painting, this is the image that we paint into.
        private byte[] maskPixels; // byte array for mask texture
        private byte[] clearPixels; // byte array for clearing texture

        private Texture2D drawingTexture; // texture that we paint into (it gets updated from pixels[] array when painted)

        [Header("Overrides")]
        public float resolutionScaler = 1.0f; // 1 means screen resolution, 0.5f means half the screen resolution
        public bool overrideResolution = false;
        public int overrideWidth = 1024;
        public int overrideHeight = 768;

        private int texWidth;
        private int texHeight;
        private Touch touch; // touch reference
        private bool wasTouching = false; // in previous frame we had touch
        private Camera cam; // main camera reference
        private Renderer myRenderer;

        private RaycastHit hit;
        private bool wentOutside = false;

        private bool usingClearingImage = false; // did we have initial texture as maintexture, then use it as clear pixels array

        private Vector2 pixelUV; // with mouse
        private Vector2 pixelUVOld; // with mouse

        private Vector2[] pixelUVs; // mobiles
        private Vector2[] pixelUVOlds; // mobiles

        [HideInInspector]
        public bool textureNeedsUpdate = false; // if we have modified texture

        [Header("Misc")]
        public bool undoEnabled = false;
        private List<byte[]> undoPixels; // undo buffer(s)
        private int maxUndoBuffers = 10; // how many undo buffers are kept in memory
        public GameObject userInterface;
        public bool hideUIWhilePainting = false;
        private bool isUIVisible = true;

        // Debug mode, outputs debug info when used
        public bool debugMode = false;

        // for checking if UI element is clicked, then dont paint under it
        EventSystem eventSystem;

        // zoom pan
        private bool isZoomingOrPanning = false;

        void Awake()
        {
            // cache components
            cam = Camera.main;
            myRenderer = GetComponent<Renderer>();


            GameObject go = GameObject.Find("EventSystem");
            if (go == null)
            {
                Debug.LogError("GameObject EventSystem is missing from scene, will have problems with the UI", gameObject);
            }
            else {
                eventSystem = go.GetComponent<EventSystem>();
            }

            StartupValidation();
            InitializeEverything();
        }



        // all startup validations will be moved here
        void StartupValidation()
        {
            if (cam == null) Debug.LogError("MainCamera not founded, you must have 1 camera active & tagged as MainCamera", gameObject);

            if (userInterface == null)
            {
                if (hideUIWhilePainting)
                {
                    Debug.LogWarning("UI Canvas / userInterface not assigned - disabling hideUIWhilePainting", gameObject);
                    hideUIWhilePainting = false;
                }
            }

            // Custom brushes validation
            if (useCustomBrushes && (customBrushes == null || customBrushes.Length < 1))
            {
                Debug.LogWarning("useCustomBrushes is enabled, but no custombrushes assigned to array, disabling customBrushes", gameObject);
                useCustomBrushes = false;
            }

            // Custom patterns validation
            if (useCustomPatterns && (customPatterns == null || customPatterns.Length < 1))
            {
                Debug.LogWarning("useCustomPatterns is enabled, but no customPatterns assigned to array, disabling useCustomPatterns", gameObject);
                useCustomPatterns = false;
            }


            // MASK validation
            if (useMaskImage)
            {
                if (maskTex == null)
                {
                    Debug.LogWarning("maskImage is not assigned. Setting 'useMaskImage' to false", gameObject);
                    useMaskImage = false;
                    if (overrideResolution) Debug.LogWarning("overrideResolution cannot be used, when useMaskImage is true", gameObject);
                }
            }

            if ((!useMaskImage || maskTex == null) && useMaskLayerOnly)
            {
                //				Debug.LogWarning("useMaskImage is not enabled, or maskImage is not assigned. Setting 'useMaskLayerOnly' to false",gameObject);
                //				useMaskLayerOnly = false;
            }


            // check if target texture exists
            if (!myRenderer.material.HasProperty(targetTexture)) Debug.LogError("Fatal error: Current shader doesn't have a property: '" + targetTexture + "'", gameObject);

            if (getAreaSize)
            {
                if (!useThreshold || !useMaskLayerOnly)
                {
                    Debug.LogWarning("getAreaSize is enabled, but both useThreshold or useMaskLayerOnly are not enabled, getAreaSize might not work", gameObject);
                }
            }

            if (paintLayerMask.value == 0)
            {
                Debug.LogWarning("paintLayerMask is set to 'nothing', assign same layer where the drawing canvas is", gameObject);
            }

            // validate & clamp override resolution
            if (overrideResolution)
            {
                if (overrideWidth < 0 || overrideWidth > 8192) { Debug.LogWarning("Invalid overrideWidth:" + overrideWidth, gameObject); overrideWidth = Mathf.Clamp(0, 1, 8192); }
                if (overrideHeight < 0 || overrideHeight > 8192) { Debug.LogWarning("Invalid overrideHeight:" + overrideHeight, gameObject); overrideHeight = Mathf.Clamp(0, 1, 8192); }

                if (resolutionScaler != 1)
                {
                    Debug.LogWarning("Cannot use resolutionScaler with OverrideResolution, setting resolutionScaler to default (1)");
                    resolutionScaler = 1;
                }

            }

            if (GetComponent<LineRenderer>() == null)
            {
                //				Debug.LogWarning("No linerenderer component added. LineDrawing mode preview requires it",gameObject);
            }

            /*
			if (previewLineCircle==null)
			{
				Debug.LogWarning("previewLineCircleStart not assigned, its required for linedrawing (you can copy it from scene_MobilePaint_DrawLines.scene. Gameobject 'linePreviewCircle')",gameObject);
			}*/

            // check eraser modes
            if (myRenderer.material.GetTexture(targetTexture) == null)
            {
                if (eraserMode == EraserMode.Default)
                {
                    Debug.LogError("eraserMode is set to Default, but there is no texture assigned to " + targetTexture + ". Setting eraseMode to BackgroundColor");
                    eraserMode = EraserMode.BackgroundColor;
                }
            }


        } // StartupValidation()

        //bool firstRun = false; // TODO: this could be used to check if InitializeEverything() was called after first run

        // rebuilds everything and reloads masks,textures..
        public void InitializeEverything()
        {

            // for drawing lines preview
            if (GetComponent<LineRenderer>() != null)
            {
                lineRenderer = GetComponent<LineRenderer>();

                // reset pos
                lineRenderer.SetPosition(0, Vector3.one * 99999);
                lineRenderer.SetPosition(1, Vector3.one * 99999);

                if (previewLineCircle)
                {
                    // spawn rounded circles for linedrawing, if not already in scene
                    if (!previewLineCircleStart) previewLineCircleStart = Instantiate(previewLineCircle) as Transform;
                    if (!previewLineCircleEnd) previewLineCircleEnd = Instantiate(previewLineCircle) as Transform;

                    // hide them far away
                    previewLineCircleStart.position = Vector3.one * 99999;
                    previewLineCircleEnd.position = Vector3.one * 99999;
                }
                UpdateLineModePreviewObjects();
            }

            // cached calculations
            brushSizeX1 = brushSize << 1;
            brushSizeXbrushSize = brushSize * brushSize;
            brushSizeX4 = brushSizeXbrushSize << 2;
            //brushAlphaStrengthVal = 255f*brushAlphaStrength;

            SetBrushAlphaStrength(brushAlphaStrength);
            SetPaintColor(paintColor);

            // calculate scaling ratio for different screen resolutions
            float _baseHeightInverted = 1.0f / BASE_HEIGHT;
            float ratio = (Screen.height * _baseHeightInverted) * scaleAdjust;
            canvasSizeAdjust *= ratio;

            // WARNING: fixed maximum amount of touches, is set to 20 here. Not sure if some device supports more?
            pixelUVs = new Vector2[20];
            pixelUVOlds = new Vector2[20];

            if (createCanvasMesh)
            {
                CreateFullScreenQuad();
            }
            else { // using existing mesh
                if (connectBrushStokes) Debug.LogWarning("Custom mesh used, but connectBrushStokes is enabled, it can cause problems on the mesh borders wrapping");

                if (GetComponent<MeshCollider>() == null) Debug.LogError("MeshCollider is missing, won't be able to raycast to canvas object");
                if (GetComponent<MeshFilter>() == null || GetComponent<MeshFilter>().sharedMesh == null) Debug.LogWarning("Mesh or MeshFilter is missing, won't be able to see the canvas object");
            }

            // create texture
            if (useMaskImage)
            {
                SetMaskImage(maskTex);
            }
            else {  // no mask texture

                // overriding will also ignore Resolution Scaler value
                if (overrideResolution)
                {
                    var err = false;
                    if (overrideWidth < 0 || overrideWidth > 4096) err = true;
                    if (overrideHeight < 0 || overrideHeight > 4096) err = true;
                    if (err) Debug.LogError("overrideWidth or overrideWidth is invalid - clamping to 4 or 4096");
                    texWidth = (int)Mathf.Clamp(overrideWidth, 4, 4096);
                    texHeight = (int)Mathf.Clamp(overrideHeight, 4, 4096);

                }
                else { // use screen size as texture size
                    texWidth = (int)(Screen.width * resolutionScaler + canvasSizeAdjust.x);
                    texHeight = (int)(Screen.height * resolutionScaler + canvasSizeAdjust.y);
                }
            }



            // we have no texture set for canvas, FIXME: this returns true if called initialize again, since texture gets created after this
            if (myRenderer.material.GetTexture(targetTexture) == null && !usingClearingImage) // temporary fix by adding && !usingClearingImage
            {
                // create new texture
                if (drawingTexture != null) Texture2D.DestroyImmediate(drawingTexture, true); // cleanup old texture
                drawingTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
                myRenderer.material.SetTexture(targetTexture, drawingTexture);

                // init pixels array
                pixels = new byte[texWidth * texHeight * 4];

            } else { // we have canvas texture, then use that as clearing texture

                usingClearingImage = true;

                if (overrideResolution) Debug.LogWarning("overrideResolution is not used, when canvas texture is assiged to material, we need to use the texture size");

                texWidth = myRenderer.material.GetTexture(targetTexture).width;
                texHeight = myRenderer.material.GetTexture(targetTexture).height;

                // init pixels array
                pixels = new byte[texWidth * texHeight * 4];

                if (drawingTexture != null) Texture2D.DestroyImmediate(drawingTexture, true); // cleanup old texture
                drawingTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);

                // we keep current maintex and read it as "clear pixels array" (so when "clear image" is clicked, original texture is restored
                ReadClearingImage();
                myRenderer.material.SetTexture(targetTexture, drawingTexture);
            }


            // set texture modes
            drawingTexture.filterMode = filterMode;
            drawingTexture.wrapMode = TextureWrapMode.Clamp;


            // locking mask enabled
            if (useLockArea)
            {
                lockMaskPixels = new byte[texWidth * texHeight * 4];
            }

            if (customPatterns != null && customPatterns.Length > 0) ReadCurrentCustomPattern();

            // grid for line shapes
            gridSize = texWidth / gridResolution;

            // init custom brush if used
            if (useCustomBrushes && drawMode == DrawMode.CustomBrush) ReadCurrentCustomBrush();

            // whats our final resolution
            if (debugMode) Debug.Log("Texture resolution:" + texWidth + "x" + texHeight);

            // init undo buffer
            if (undoEnabled)
            {
                undoPixels = new List<byte[]>();
            }

            ClearImage(updateUndoBuffer: false);


        } // InitializeEverything



        // *** MAINLOOP ***
        void Update()
        {
            if (enableTouch)
            {
                TouchPaint();
            }
            else {
                MousePaint();
            }

            if (textureNeedsUpdate && (realTimeTexUpdate || Time.time > nextTextureUpdate))
            {
                nextTextureUpdate = Time.time + textureUpdateSpeed;
                UpdateTexture();
            }
        }

        // handle mouse events
        void MousePaint()
        {
            // TEST: Undo key for desktop
            if (undoEnabled && Input.GetKeyDown("u")) DoUndo();

            // mouse is over UI element? then dont paint
            if (eventSystem.IsPointerOverGameObject()) return;
            if (eventSystem.currentSelectedGameObject != null) return;

            // catch first mousedown
            if (Input.GetMouseButtonDown(0))
            {
                if (hideUIWhilePainting && isUIVisible) HideUI();

                // when starting, grab undo buffer first
                if (undoEnabled) GrabUndoBufferNow();

                // if lock area is used, we need to take full area before painting starts
                if (useLockArea)
                {
                    if (!Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity, paintLayerMask)) return;
                    CreateAreaLockMask((int)(hit.textureCoord.x * texWidth), (int)(hit.textureCoord.y * texHeight));
                }
            }

            // left button is held down, draw
            if (Input.GetMouseButton(0))
            {
                // Only if we hit something, then we continue
                if (!Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity, paintLayerMask)) { wentOutside = true; return; }

                pixelUVOld = pixelUV; // take previous value, so can compare them
                pixelUV = hit.textureCoord;
                pixelUV.x *= texWidth;
                pixelUV.y *= texHeight;

                if (wentOutside) { pixelUVOld = pixelUV; wentOutside = false; }


                // lets paint where we hit
                switch (drawMode)
                {
                    case DrawMode.Default: // brush
                        DrawCircle((int)pixelUV.x, (int)pixelUV.y);
                        break;

                    case DrawMode.Pattern:
                        DrawPatternCircle((int)pixelUV.x, (int)pixelUV.y);
                        break;

                    case DrawMode.CustomBrush:
                        DrawCustomBrush((int)pixelUV.x, (int)pixelUV.y);
                        break;

                    case DrawMode.FloodFill:
                        if (pixelUVOld == pixelUV) break;
                        CallFloodFill((int)pixelUV.x, (int)pixelUV.y);
                        break;

                    case DrawMode.ShapeLines:
                        if (snapLinesToGrid)
                        {
                            DrawShapeLinePreview(SnapToGrid((int)pixelUV.x), SnapToGrid((int)pixelUV.y));
                        }
                        else {

                            DrawShapeLinePreview((int)pixelUV.x, (int)pixelUV.y);
                        }
                        break;

                    case DrawMode.Eraser:
                        if (eraserMode == EraserMode.Default)
                        {
                            EraseWithImage((int)pixelUV.x, (int)pixelUV.y);
                        }
                        else {
                            EraseWithBackgroundColor((int)pixelUV.x, (int)pixelUV.y);
                        }
                        break;


                    default: // unknown DrawMode
                        Debug.LogError("Unknown drawMode");
                        break;
                }

                textureNeedsUpdate = true;
            }


            if (Input.GetMouseButtonDown(0))
            {
                // take this position as start position
                if (!Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity, paintLayerMask)) return;

                pixelUVOld = pixelUV;
            }


            // check distance from previous drawing point and connect them with DrawLine
            //			if (connectBrushStokes && Vector2.Distance(pixelUV,pixelUVOld)>brushSize)
            if (connectBrushStokes && textureNeedsUpdate)
            {
                switch (drawMode)
                {
                    case DrawMode.Default: // drawing
                        DrawLine(pixelUVOld, pixelUV);
                        break;

                    case DrawMode.CustomBrush:
                        DrawLineWithBrush(pixelUVOld, pixelUV);
                        break;

                    case DrawMode.Pattern:
                        DrawLineWithPattern(pixelUVOld, pixelUV);
                        break;

                    case DrawMode.Eraser:
                        if (eraserMode == EraserMode.Default)
                        {
                            EraseWithImageLine(pixelUVOld, pixelUV);
                        }
                        else {
                            EraseWithBackgroundColorLine(pixelUVOld, pixelUV);
                        }
                        break;

                    default: // other modes
                        break;
                }
                pixelUVOld = pixelUV;
                textureNeedsUpdate = true;
            }

            // left mouse button released
            if (Input.GetMouseButtonUp(0))
            {
                // calculate area size
                if (getAreaSize && useLockArea && useMaskLayerOnly && drawMode != DrawMode.FloodFill)
                {
                    LockAreaFillWithThresholdMaskOnlyGetArea(initialX, initialY, true);
                }

                // end shape line here
                if (drawMode == DrawMode.ShapeLines)
                {
                    haveStartedLine = false;

                    // hide preview line
                    lineRenderer.SetPosition(0, Vector3.one * 99999);
                    lineRenderer.SetPosition(1, Vector3.one * 99999);
                    previewLineCircleStart.position = Vector3.one * 99999;
                    previewLineCircleEnd.position = Vector3.one * 99999;

                    // draw actual line from start to current pos
                    if (snapLinesToGrid)
                    {
                        Vector2 extendLine = (pixelUV - new Vector2((float)firstClickX, (float)firstClickY)).normalized * (brushSize * 0.25f);
                        DrawLine(firstClickX - (int)extendLine.x, firstClickY - (int)extendLine.y, SnapToGrid((int)pixelUV.x + (int)extendLine.x), SnapToGrid((int)pixelUV.y + (int)extendLine.y));

                        //DrawLine(firstClickX,firstClickY,SnapToGrid((int)pixelUV.x),SnapToGrid((int)pixelUV.y));
                    }
                    else {

                        // need to extend line to avoid too short start/end
                        Vector2 extendLine = (pixelUV - new Vector2((float)firstClickX, (float)firstClickY)).normalized * (brushSize * 0.25f);
                        DrawLine(firstClickX - (int)extendLine.x, firstClickY - (int)extendLine.y, (int)pixelUV.x + (int)extendLine.x, (int)pixelUV.y + (int)extendLine.y);
                    }
                    textureNeedsUpdate = true;
                }

                if (hideUIWhilePainting && !isUIVisible) ShowUI(); // show UI since we stopped drawing
            }

        }


        // ** Main loop for touch paint **
        int i = 0;
        void TouchPaint()
        {
            // check if any touch is over UI objects, then early exit (dont paint)
            while (i < Input.touchCount)
            {
                touch = Input.GetTouch(i);
                if (eventSystem.IsPointerOverGameObject(touch.fingerId)) return;
                i++;
            }
            if (eventSystem.currentSelectedGameObject != null) return;

            i = 0;
            // loop until all touches are processed
            while (i < Input.touchCount)
            {

                touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    wasTouching = true;

                    if (hideUIWhilePainting && isUIVisible) HideUI();

                    // when starting to draw, grab undo buffer first, FIXME: do this after painting, so it wont slowdown
                    if (undoEnabled)
                    {
                        GrabUndoBufferNow();
                    }

                    if (useLockArea)
                    {
                        if (!Physics.Raycast(cam.ScreenPointToRay(touch.position), out hit, Mathf.Infinity, paintLayerMask)) { wentOutside = true; return; }

                        /*
						pixelUV = hit.textureCoord;
						pixelUV.x *= texWidth;
						pixelUV.y *= texHeight;
						if (wentOutside) {pixelUVOld = pixelUV;wentOutside=false;}
						CreateAreaLockMask((int)pixelUV.x, (int)pixelUV.y);
						*/

                        pixelUVs[touch.fingerId] = hit.textureCoord;
                        pixelUVs[touch.fingerId].x *= texWidth;
                        pixelUVs[touch.fingerId].y *= texHeight;
                        if (wentOutside) { pixelUVOlds[touch.fingerId] = pixelUVs[touch.fingerId]; wentOutside = false; }
                        CreateAreaLockMask((int)pixelUVs[touch.fingerId].x, (int)pixelUVs[touch.fingerId].y);
                    }
                }
                // check state
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Began)
                {

                    // do raycast on touch position
                    if (Physics.Raycast(cam.ScreenPointToRay(touch.position), out hit, Mathf.Infinity, paintLayerMask))
                    {
                        // take previous value, so can compare them
                        pixelUVOlds[touch.fingerId] = pixelUVs[touch.fingerId];
                        // get hit texture coordinate
                        pixelUVs[touch.fingerId] = hit.textureCoord;
                        pixelUVs[touch.fingerId].x *= texWidth;
                        pixelUVs[touch.fingerId].y *= texHeight;
                        // paint where we hit
                        switch (drawMode)
                        {
                            case DrawMode.Default:
                                DrawCircle((int)pixelUVs[touch.fingerId].x, (int)pixelUVs[touch.fingerId].y);
                                break;

                            case DrawMode.CustomBrush:
                                DrawCustomBrush((int)pixelUVs[touch.fingerId].x, (int)pixelUVs[touch.fingerId].y);
                                break;

                            case DrawMode.Pattern:
                                DrawPatternCircle((int)pixelUVs[touch.fingerId].x, (int)pixelUVs[touch.fingerId].y);
                                break;

                            case DrawMode.FloodFill:
                                CallFloodFill((int)pixelUVs[touch.fingerId].x, (int)pixelUVs[touch.fingerId].y);
                                break;

                            case DrawMode.ShapeLines:
                                if (snapLinesToGrid)
                                {
                                    DrawShapeLinePreview(SnapToGrid((int)pixelUVs[touch.fingerId].x), SnapToGrid((int)pixelUVs[touch.fingerId].y));
                                }
                                else {
                                    DrawShapeLinePreview((int)pixelUVs[touch.fingerId].x, (int)pixelUVs[touch.fingerId].y);
                                }
                                break;

                            case DrawMode.Eraser:
                                if (eraserMode == EraserMode.Default)
                                {
                                    EraseWithImage((int)pixelUVs[touch.fingerId].x, (int)pixelUVs[touch.fingerId].y);
                                }
                                else {
                                    EraseWithBackgroundColor((int)pixelUVs[touch.fingerId].x, (int)pixelUVs[touch.fingerId].y);
                                }
                                break;


                            default:
                                // unknown mode
                                break;
                        }
                        // set flag that texture needs to be applied
                        textureNeedsUpdate = true;
                    }
                }
                // if we just touched screen, set this finger id texture paint start position to that place
                if (touch.phase == TouchPhase.Began)
                {
                    pixelUVOlds[touch.fingerId] = pixelUVs[touch.fingerId];
                }
                // check distance from previous drawing point
                //if (connectBrushStokes && Vector2.Distance (pixelUVs[touch.fingerId], pixelUVOlds[touch.fingerId]) > brushSize) 
                if (connectBrushStokes && textureNeedsUpdate)
                {
                    switch (drawMode)
                    {
                        case DrawMode.Default:
                            DrawLine(pixelUVOlds[touch.fingerId], pixelUVs[touch.fingerId]);
                            break;

                        case DrawMode.CustomBrush:
                            DrawLineWithBrush(pixelUVOlds[touch.fingerId], pixelUVs[touch.fingerId]);
                            break;

                        case DrawMode.Pattern:
                            DrawLineWithPattern(pixelUVOlds[touch.fingerId], pixelUVs[touch.fingerId]);
                            break;

                        case DrawMode.Eraser:
                            if (eraserMode == EraserMode.Default)
                            {
                                EraseWithImageLine(pixelUVOlds[touch.fingerId], pixelUVs[touch.fingerId]);
                            }
                            else {
                                EraseWithBackgroundColorLine(pixelUVOlds[touch.fingerId], pixelUVs[touch.fingerId]);
                            }
                            break;

                        default:
                            // unknown mode
                            break;
                    }
                    textureNeedsUpdate = true;

                    pixelUVOlds[touch.fingerId] = pixelUVs[touch.fingerId];

                }
                // loop all touches
                i++;
            }

            // no touches
            if (wasTouching && Input.touchCount == 0)
            {
                wasTouching = false;

                if (useLockArea && useMaskLayerOnly && drawMode == DrawMode.Default)
                {
                    LockAreaFillWithThresholdMaskOnlyGetArea(initialX, initialY, true);
                }

                // end shape line here
                if (drawMode == DrawMode.ShapeLines)
                {
                    // hide preview line
                    lineRenderer.SetPosition(0, Vector3.one * 99999);
                    lineRenderer.SetPosition(1, Vector3.one * 99999);
                    haveStartedLine = false;

                    previewLineCircleStart.position = Vector3.one * 99999;
                    previewLineCircleEnd.position = Vector3.one * 99999;

                    // draw actual line from start to current pos
                    if (snapLinesToGrid)
                    {
                        DrawLine(new Vector2(firstClickX, firstClickY), new Vector2(SnapToGrid((int)pixelUVs[touch.fingerId].x), SnapToGrid((int)pixelUVs[touch.fingerId].y)));
                    }
                    else {
                        DrawLine(new Vector2(firstClickX, firstClickY), pixelUVs[touch.fingerId]);
                    }
                    textureNeedsUpdate = true;
                }

                if (hideUIWhilePainting && !isUIVisible) ShowUI();
            }

        }

        public virtual void HideUI()
        {
            isUIVisible = false;
            userInterface.SetActive(isUIVisible);
        }

        public virtual void ShowUI()
        {
            isUIVisible = true;
            userInterface.SetActive(isUIVisible);
        }


        void UpdateTexture()
        {
            textureNeedsUpdate = false;
            drawingTexture.LoadRawTextureData(pixels);
            drawingTexture.Apply(false);
        }


        void CreateAreaLockMask(int x, int y)
        {

            initialX = x;
            initialY = y;

            if (useThreshold)
            {
                if (useMaskLayerOnly)
                {
                    if (getAreaSize)
                    {
                        LockAreaFillWithThresholdMaskOnlyGetArea(x, y, false);
                    }
                    else {
                        LockAreaFillWithThresholdMaskOnly(x, y);
                    }
                }
                else {
                    LockMaskFillWithThreshold(x, y);
                }
            }
            else { // no threshold
                if (useMaskLayerOnly)
                {
                    LockAreaFillMaskOnly(x, y);
                }
                else {
                    LockAreaFill(x, y);
                }
            }
            //lockMaskCreated = true; // not used yet
        }

        public void DrawShapeLinePreview(int x, int y)
        {
            Vector3 worldPixelPos = PixelToWorld(x, y);

            // just started
            if (!haveStartedLine)
            {
                haveStartedLine = true;


                firstClickX = x;
                firstClickY = y;

                lineRenderer.SetPosition(0, worldPixelPos);
                previewLineCircleStart.position = worldPixelPos;

            }
            else { // button is kept down

                // draw preview from start to current pos
                lineRenderer.SetPosition(1, worldPixelPos);
                previewLineCircleEnd.position = worldPixelPos;
            }

            // add rounded linerenderer ends

        }

        // main painting function, modified from http://stackoverflow.com/b/24453110
        public void DrawCircle(int x, int y)
        {

            int pixel = 0;

            for (int i = 0; i < brushSizeX4; i++)
            {
                int tx = (i % brushSizeX1) - brushSize;
                int ty = (i / brushSizeX1) - brushSize;

                if (tx * tx + ty * ty > brushSizeXbrushSize) continue;
                if (x + tx < 0 || y + ty < 0 || x + tx >= texWidth || y + ty >= texHeight) continue; // temporary fix for corner painting

                pixel = (texWidth * (y + ty) + x + tx) << 2;

                if (useAdditiveColors)
                {
                    if (!useLockArea || (useLockArea && lockMaskPixels[pixel] == 1))
                    {
                        pixels[pixel] = ByteLerp(pixels[pixel], paintColor.r, alphaLerpVal);
                        pixels[pixel + 1] = ByteLerp(pixels[pixel + 1], paintColor.g, alphaLerpVal);
                        pixels[pixel + 2] = ByteLerp(pixels[pixel + 2], paintColor.b, alphaLerpVal);
                        pixels[pixel + 3] = ByteLerp(pixels[pixel + 3], paintColor.a, alphaLerpVal);
                    }
                }
                else { // no additive, just paint my color
                    if (!useLockArea || (useLockArea && lockMaskPixels[pixel] == 1))
                    {
                        pixels[pixel] = paintColor.r;
                        pixels[pixel + 1] = paintColor.g;
                        pixels[pixel + 2] = paintColor.b;
                        pixels[pixel + 3] = paintColor.a;
                    }
                } // if additive
            } // for area
        } // DrawCircle()

        // Temporary basic eraser tool
        public void EraseWithBackgroundColor(int x, int y)
        {
            var origColor = paintColor;
            paintColor = clearColor;
            //var origSize = brushSize; // optional, have fixed eraser brush size temporarily while drawing
            //SetBrushSize(defaultEraserSize);
            DrawCircle(x, y);
            //SetBrushSize(origSize);
            paintColor = origColor;
        }


        public void EraseWithImage(int x, int y)
        {
            int pixel = 0;
            for (int i = 0; i < brushSizeX4; i++)
            {
                int tx = (i % brushSizeX1) - brushSize;
                int ty = (i / brushSizeX1) - brushSize;

                if (tx * tx + ty * ty > brushSizeXbrushSize) continue;
                if (x + tx < 0 || y + ty < 0 || x + tx >= texWidth || y + ty >= texHeight) continue; // temporary fix for corner painting

                pixel = (texWidth * (y + ty) + x + tx) << 2;

                float xx = Mathf.Repeat(y + ty, texHeight);
                float yy = Mathf.Repeat(x + tx, texWidth);
                int pixel2 = (int)Mathf.Repeat((texWidth * xx + yy) * 4, clearPixels.Length);

                pixels[pixel] = clearPixels[pixel2];
                pixels[pixel + 1] = clearPixels[pixel2 + 1];
                pixels[pixel + 2] = clearPixels[pixel2 + 2];
                pixels[pixel + 3] = clearPixels[pixel2 + 3];
            } 
        }



        public void DrawPatternCircle(int x, int y)
        {
            // clamp brush inside texture
            if (createCanvasMesh) // TEMPORARY FIX: with b custom sphere mesh, small gap in paint at the end, so must disable clamp on most custom meshes
            {
                //x = PaintTools.ClampBrushInt(x,brushSize,texWidth-brushSize);
                //y = PaintTools.ClampBrushInt(y,brushSize,texHeight-brushSize);
            }

            if (!canDrawOnBlack)
            {
                //				if (pixels[(texWidth*y+x)*4]==0 && pixels[(texWidth*y+x)*4+1]==0 && pixels[(texWidth*y+x)*4+2]==0 && pixels[(texWidth*y+x)*4+3]!=0) return;
            }

            int pixel = 0;
            for (int i = 0; i < brushSizeX4; i++)
            {
                int tx = (i % brushSizeX1) - brushSize;
                int ty = (i / brushSizeX1) - brushSize;

                if (tx * tx + ty * ty > brushSizeXbrushSize) continue;
                if (x + tx < 0 || y + ty < 0 || x + tx >= texWidth || y + ty >= texHeight) continue; // temporary fix for corner painting

                pixel = (texWidth * (y + ty) + x + tx) << 2;

                //if (useAdditiveColors)
                //{
                // additive over white also
                //if (!useLockArea || (useLockArea && lockMaskPixels[pixel]==1))
                //{

                // TODO: take pattern texture as paint color
                /*
                Color32 patternColor = new Color(x,y,0,1);

                pixels[pixel] = (byte)Mathf.Lerp(pixels[pixel],patternColor.r,patternColor.b/255f*brushAlphaStrength);
                pixels[pixel+1] = (byte)Mathf.Lerp(pixels[pixel+1],patternColor.g,patternColor.b/255f*brushAlphaStrength);
                pixels[pixel+2] = (byte)Mathf.Lerp(pixels[pixel+2],patternColor.b,patternColor.b/255f*brushAlphaStrength);
                pixels[pixel+3] = (byte)Mathf.Lerp(pixels[pixel+3],patternColor.b,patternColor.b/255*brushAlphaStrength);
                */
                //}

                //}else{ // no additive, just paint my colors

                if (!useLockArea || (useLockArea && lockMaskPixels[pixel] == 1))
                {
                    float yy = Mathf.Repeat(y + ty, customPatternWidth);
                    float xx = Mathf.Repeat(x + tx, customPatternWidth);
                    int pixel2 = (int)Mathf.Repeat((customPatternWidth * xx + yy) * 4, patternBrushBytes.Length);

                    pixels[pixel] = patternBrushBytes[pixel2];
                    pixels[pixel + 1] = patternBrushBytes[pixel2 + 1];
                    pixels[pixel + 2] = patternBrushBytes[pixel2 + 2];
                    pixels[pixel + 3] = patternBrushBytes[pixel2 + 3];
                }

                //} // if additive
            } // for area
        } // DrawPatternCircle()



        // actual custom brush painting function
        void DrawCustomBrush(int px, int py)
        {
            // get position where we paint
            int startX = (int)(px - customBrushWidthHalf);
            int startY = (int)(py - customBrushWidthHalf);

            if (startX < 0)
            {
                startX = 0;
            }
            else {
                if (startX + customBrushWidth >= texWidth) startX = texWidthMinusCustomBrushWidth;
            }

            if (startY < 1)  // TODO: temporary fix, 1 instead of 0
            {
                startY = 1;
            }
            else {
                if (startY + customBrushHeight >= texHeight) startY = texHeightMinusCustomBrushHeight;
            }

            // could use this for speed (but then its box shaped..)
            //System.Array.Copy(splatPixByte,0,data,4*(startY*startX),splatPixByte.Length);	

            int pixel = (texWidth * startY + startX) << 2;
            int brushPixel = 0;

            //
            for (int y = 0; y < customBrushHeight; y++)
            {
                for (int x = 0; x < customBrushWidth; x++)
                {
                    brushPixel = (customBrushWidth * (y) + x) << 2;

                    // we have some color at this brush pixel?
                    if (customBrushBytes[brushPixel + 3] > 0 && (!useLockArea || (useLockArea && lockMaskPixels[pixel] == 1)))
                    {

                        if (useCustomBrushAlpha) // use alpha from brush
                        {
                            if (useAdditiveColors)
                            {

                                brushAlphaLerpVal = customBrushBytes[brushPixel + 3] * brushAlphaStrength * 0.01f; // 0.01 is temporary fix so that default brush & custom brush both work

                                if (overrideCustomBrushColor)
                                {
                                    pixels[pixel] = ByteLerp(pixels[pixel], paintColor.r, brushAlphaLerpVal);
                                    pixels[pixel + 1] = ByteLerp(pixels[pixel + 1], paintColor.g, brushAlphaLerpVal);
                                    pixels[pixel + 2] = ByteLerp(pixels[pixel + 2], paintColor.b, brushAlphaLerpVal);
                                }
                                else { // use paint color instead of brush texture
                                    pixels[pixel] = ByteLerp(pixels[pixel], customBrushBytes[brushPixel], brushAlphaLerpVal);
                                    pixels[pixel + 1] = ByteLerp(pixels[pixel + 1], customBrushBytes[brushPixel + 1], brushAlphaLerpVal);
                                    pixels[pixel + 2] = ByteLerp(pixels[pixel + 2], customBrushBytes[brushPixel + 2], brushAlphaLerpVal);
                                }
                                pixels[pixel + 3] = ByteLerp(pixels[pixel + 3], paintColor.a, brushAlphaLerpVal);

                            }
                            else { // no additive colors
                                if (overrideCustomBrushColor)
                                {
                                    pixels[pixel] = ByteLerp(pixels[pixel], paintColor.r, brushAlphaLerpVal);
                                    pixels[pixel + 1] = ByteLerp(pixels[pixel + 1], paintColor.g, brushAlphaLerpVal);
                                    pixels[pixel + 2] = ByteLerp(pixels[pixel + 2], paintColor.b, brushAlphaLerpVal);
                                }
                                else { // use paint color instead of brush texture
                                    pixels[pixel] = customBrushBytes[brushPixel];
                                    pixels[pixel + 1] = customBrushBytes[brushPixel + 1];
                                    pixels[pixel + 2] = customBrushBytes[brushPixel + 2];
                                }
                                pixels[pixel + 3] = customBrushBytes[brushPixel + 3];
                            }

                        }
                        else { // use paint color alpha

                            if (useAdditiveColors)
                            {
                                if (overrideCustomBrushColor)
                                {
                                    pixels[pixel] = ByteLerp(pixels[pixel], paintColor.r, brushAlphaLerpVal);
                                    pixels[pixel + 1] = ByteLerp(pixels[pixel + 1], paintColor.g, brushAlphaLerpVal);
                                    pixels[pixel + 2] = ByteLerp(pixels[pixel + 2], paintColor.b, brushAlphaLerpVal);
                                }
                                else {
                                    pixels[pixel] = ByteLerp(pixels[pixel], customBrushBytes[brushPixel], brushAlphaLerpVal);
                                    pixels[pixel + 1] = ByteLerp(pixels[pixel + 1], customBrushBytes[brushPixel + 1], brushAlphaLerpVal);
                                    pixels[pixel + 2] = ByteLerp(pixels[pixel + 2], customBrushBytes[brushPixel + 2], brushAlphaLerpVal);
                                }
                                pixels[pixel + 3] = ByteLerp(pixels[pixel + 3], paintColor.a, brushAlphaLerpVal);
                            }
                            else { // no additive colors
                                if (overrideCustomBrushColor)
                                {
                                    pixels[pixel] = ByteLerp(pixels[pixel], paintColor.r, brushAlphaLerpVal);
                                    pixels[pixel + 1] = ByteLerp(pixels[pixel + 1], paintColor.g, brushAlphaLerpVal);
                                    pixels[pixel + 2] = ByteLerp(pixels[pixel + 2], paintColor.b, brushAlphaLerpVal);
                                }
                                else {
                                    pixels[pixel] = customBrushBytes[brushPixel];
                                    pixels[pixel + 1] = customBrushBytes[brushPixel + 1];
                                    pixels[pixel + 2] = customBrushBytes[brushPixel + 2];
                                }
                                pixels[pixel + 3] = customBrushBytes[brushPixel + 3];
                            }
                        }
                    }
                    pixel += 4;

                } // for x

                pixel = (texWidth * (startY == 0 ? 1 : startY + y) + startX + 1) * 4;
            } // for y
        } // DrawCustomBrush

        void FloodFillMaskOnly(int x, int y)
        {
            // get canvas hit color
            byte hitColorR = maskPixels[((texWidth * (y) + x) * 4) + 0];
            byte hitColorG = maskPixels[((texWidth * (y) + x) * 4) + 1];
            byte hitColorB = maskPixels[((texWidth * (y) + x) * 4) + 2];
            byte hitColorA = maskPixels[((texWidth * (y) + x) * 4) + 3];

            // early exit if its same color already
            //if (paintColor.r == hitColorR && paintColor.g == hitColorG && paintColor.b == hitColorB && paintColor.b == hitColorA) return;

            if (!canDrawOnBlack)
            {
                if (hitColorA == 0) return;
            }


            Queue<int> fillPointX = new Queue<int>();
            Queue<int> fillPointY = new Queue<int>();
            fillPointX.Enqueue(x);
            fillPointY.Enqueue(y);

            int ptsx, ptsy;
            int pixel = 0;

            lockMaskPixels = new byte[texWidth * texHeight * 4];

            while (fillPointX.Count > 0)
            {

                ptsx = fillPointX.Dequeue();
                ptsy = fillPointY.Dequeue();

                if (ptsy - 1 > -1)
                {
                    pixel = (texWidth * (ptsy - 1) + ptsx) * 4; // down
                    if (lockMaskPixels[pixel] == 0
                        && maskPixels[pixel + 0] == hitColorR
                        && maskPixels[pixel + 1] == hitColorG
                        && maskPixels[pixel + 2] == hitColorB
                        && maskPixels[pixel + 3] == hitColorA)
                    {
                        fillPointX.Enqueue(ptsx);
                        fillPointY.Enqueue(ptsy - 1);
                        DrawPoint(pixel);
                        lockMaskPixels[pixel] = 1;
                    }
                }

                if (ptsx + 1 < texWidth)
                {
                    pixel = (texWidth * ptsy + ptsx + 1) * 4; // right
                    if (lockMaskPixels[pixel] == 0
                        && maskPixels[pixel + 0] == hitColorR
                        && maskPixels[pixel + 1] == hitColorG
                        && maskPixels[pixel + 2] == hitColorB
                        && maskPixels[pixel + 3] == hitColorA)
                    {
                        fillPointX.Enqueue(ptsx + 1);
                        fillPointY.Enqueue(ptsy);
                        DrawPoint(pixel);
                        lockMaskPixels[pixel] = 1;
                    }
                }

                if (ptsx - 1 > -1)
                {
                    pixel = (texWidth * ptsy + ptsx - 1) * 4; // left
                    if (lockMaskPixels[pixel] == 0
                        && maskPixels[pixel + 0] == hitColorR
                        && maskPixels[pixel + 1] == hitColorG
                        && maskPixels[pixel + 2] == hitColorB
                        && maskPixels[pixel + 3] == hitColorA)
                    {
                        fillPointX.Enqueue(ptsx - 1);
                        fillPointY.Enqueue(ptsy);
                        DrawPoint(pixel);
                        lockMaskPixels[pixel] = 1;
                    }
                }

                if (ptsy + 1 < texHeight)
                {
                    pixel = (texWidth * (ptsy + 1) + ptsx) * 4; // up
                    if (lockMaskPixels[pixel] == 0
                        && maskPixels[pixel + 0] == hitColorR
                        && maskPixels[pixel + 1] == hitColorG
                        && maskPixels[pixel + 2] == hitColorB
                        && maskPixels[pixel + 3] == hitColorA)
                    {
                        fillPointX.Enqueue(ptsx);
                        fillPointY.Enqueue(ptsy + 1);
                        DrawPoint(pixel);
                        lockMaskPixels[pixel] = 1;
                    }
                }
            }
        } // floodfill


        void CallFloodFill(int x, int y)
        {
            if (useThreshold)
            {
                if (useMaskLayerOnly)
                {
                    FloodFillMaskOnlyWithThreshold(x, y);
                }
                else {
                    FloodFillWithTreshold(x, y);
                }
            }
            else { // no threshold
                if (useMaskLayerOnly)
                {
                    FloodFillMaskOnly(x, y);
                }
                else {

                    FloodFill(x, y);
                }
            }
        }

        // basic floodfill
        void FloodFill(int x, int y)
        {
            // get canvas hit color
            byte hitColorR = pixels[((texWidth * (y) + x) * 4) + 0];
            byte hitColorG = pixels[((texWidth * (y) + x) * 4) + 1];
            byte hitColorB = pixels[((texWidth * (y) + x) * 4) + 2];
            byte hitColorA = pixels[((texWidth * (y) + x) * 4) + 3];

            //if (!canDrawOnBlack) // NOTE: currently broken
            //{
            //if (hitColorR==0 && hitColorG==0 && hitColorB==0 && hitColorA!=0) return;
            //}

            // early exit if its same color already
            if (paintColor.r == hitColorR && paintColor.g == hitColorG && paintColor.b == hitColorB && paintColor.a == hitColorA) return;

            Queue<int> fillPointX = new Queue<int>();
            Queue<int> fillPointY = new Queue<int>();
            fillPointX.Enqueue(x);
            fillPointY.Enqueue(y);

            int ptsx, ptsy;
            int pixel = 0;

            while (fillPointX.Count > 0)
            {

                ptsx = fillPointX.Dequeue();
                ptsy = fillPointY.Dequeue();

                if (ptsy - 1 > -1)
                {
                    pixel = (texWidth * (ptsy - 1) + ptsx) * 4; // down
                    if (pixels[pixel + 0] == hitColorR
                        && pixels[pixel + 1] == hitColorG
                        && pixels[pixel + 2] == hitColorB
                        && pixels[pixel + 3] == hitColorA)
                    {
                        fillPointX.Enqueue(ptsx);
                        fillPointY.Enqueue(ptsy - 1);
                        DrawPoint(pixel);
                    }
                }

                if (ptsx + 1 < texWidth)
                {
                    pixel = (texWidth * ptsy + ptsx + 1) * 4; // right
                    if (pixels[pixel + 0] == hitColorR
                        && pixels[pixel + 1] == hitColorG
                        && pixels[pixel + 2] == hitColorB
                        && pixels[pixel + 3] == hitColorA)
                    {
                        fillPointX.Enqueue(ptsx + 1);
                        fillPointY.Enqueue(ptsy);
                        DrawPoint(pixel);
                    }
                }

                if (ptsx - 1 > -1)
                {
                    pixel = (texWidth * ptsy + ptsx - 1) * 4; // left
                    if (pixels[pixel + 0] == hitColorR
                        && pixels[pixel + 1] == hitColorG
                        && pixels[pixel + 2] == hitColorB
                        && pixels[pixel + 3] == hitColorA)
                    {
                        fillPointX.Enqueue(ptsx - 1);
                        fillPointY.Enqueue(ptsy);
                        DrawPoint(pixel);
                    }
                }

                if (ptsy + 1 < texHeight)
                {
                    pixel = (texWidth * (ptsy + 1) + ptsx) * 4; // up
                    if (pixels[pixel + 0] == hitColorR
                        && pixels[pixel + 1] == hitColorG
                        && pixels[pixel + 2] == hitColorB
                        && pixels[pixel + 3] == hitColorA)
                    {
                        fillPointX.Enqueue(ptsx);
                        fillPointY.Enqueue(ptsy + 1);
                        DrawPoint(pixel);
                    }
                }
            }
        } // floodfill

        void FloodFillMaskOnlyWithThreshold(int x, int y)
        {
            //Debug.Log("hits");
            // get canvas hit color
            byte hitColorR = maskPixels[((texWidth * (y) + x) * 4) + 0];
            byte hitColorG = maskPixels[((texWidth * (y) + x) * 4) + 1];
            byte hitColorB = maskPixels[((texWidth * (y) + x) * 4) + 2];
            byte hitColorA = maskPixels[((texWidth * (y) + x) * 4) + 3];

            if (!canDrawOnBlack)
            {
                if (hitColorA != 0) return;
            }

            // early exit if outside threshold?
            //if (CompareThreshold(paintColor.r,hitColorR) && CompareThreshold(paintColor.g,hitColorG) && CompareThreshold(paintColor.b,hitColorB) && CompareThreshold(paintColor.b,hitColorA)) return;
            if (paintColor.r == hitColorR && paintColor.g == hitColorG && paintColor.b == hitColorB && paintColor.a == hitColorA) return;

            Queue<int> fillPointX = new Queue<int>();
            Queue<int> fillPointY = new Queue<int>();
            fillPointX.Enqueue(x);
            fillPointY.Enqueue(y);

            int ptsx, ptsy;
            int pixel = 0;

            lockMaskPixels = new byte[texWidth * texHeight * 4];

            while (fillPointX.Count > 0)
            {

                ptsx = fillPointX.Dequeue();
                ptsy = fillPointY.Dequeue();

                if (ptsy - 1 > -1)
                {
                    pixel = (texWidth * (ptsy - 1) + ptsx) * 4; // down
                    if (lockMaskPixels[pixel] == 0
                        && CompareThreshold(maskPixels[pixel + 0], hitColorR)
                        && CompareThreshold(maskPixels[pixel + 1], hitColorG)
                        && CompareThreshold(maskPixels[pixel + 2], hitColorB)
                        && CompareThreshold(maskPixels[pixel + 3], hitColorA))
                    {
                        fillPointX.Enqueue(ptsx);
                        fillPointY.Enqueue(ptsy - 1);
                        DrawPoint(pixel);
                        lockMaskPixels[pixel] = 1;
                    }
                }

                if (ptsx + 1 < texWidth)
                {
                    pixel = (texWidth * ptsy + ptsx + 1) * 4; // right
                    if (lockMaskPixels[pixel] == 0
                        && CompareThreshold(maskPixels[pixel + 0], hitColorR)
                        && CompareThreshold(maskPixels[pixel + 1], hitColorG)
                        && CompareThreshold(maskPixels[pixel + 2], hitColorB)
                        && CompareThreshold(maskPixels[pixel + 3], hitColorA))
                    {
                        fillPointX.Enqueue(ptsx + 1);
                        fillPointY.Enqueue(ptsy);
                        DrawPoint(pixel);
                        lockMaskPixels[pixel] = 1;
                    }
                }

                if (ptsx - 1 > -1)
                {
                    pixel = (texWidth * ptsy + ptsx - 1) * 4; // left
                    if (lockMaskPixels[pixel] == 0
                        && CompareThreshold(maskPixels[pixel + 0], hitColorR)
                        && CompareThreshold(maskPixels[pixel + 1], hitColorG)
                        && CompareThreshold(maskPixels[pixel + 2], hitColorB)
                        && CompareThreshold(maskPixels[pixel + 3], hitColorA))
                    {
                        fillPointX.Enqueue(ptsx - 1);
                        fillPointY.Enqueue(ptsy);
                        DrawPoint(pixel);
                        lockMaskPixels[pixel] = 1;
                    }
                }

                if (ptsy + 1 < texHeight)
                {
                    pixel = (texWidth * (ptsy + 1) + ptsx) * 4; // up
                    if (lockMaskPixels[pixel] == 0
                        && CompareThreshold(maskPixels[pixel + 0], hitColorR)
                        && CompareThreshold(maskPixels[pixel + 1], hitColorG)
                        && CompareThreshold(maskPixels[pixel + 2], hitColorB)
                        && CompareThreshold(maskPixels[pixel + 3], hitColorA))
                    {
                        fillPointX.Enqueue(ptsx);
                        fillPointY.Enqueue(ptsy + 1);
                        DrawPoint(pixel);
                        lockMaskPixels[pixel] = 1;
                    }
                }
            }
        } // floodfillWithTreshold


        void FloodFillWithTreshold(int x, int y)
        {
            // get canvas hit color
            byte hitColorR = pixels[((texWidth * (y) + x) * 4) + 0];
            byte hitColorG = pixels[((texWidth * (y) + x) * 4) + 1];
            byte hitColorB = pixels[((texWidth * (y) + x) * 4) + 2];
            byte hitColorA = pixels[((texWidth * (y) + x) * 4) + 3];

            if (!canDrawOnBlack)
            {
                if (hitColorR == 0 && hitColorG == 0 && hitColorB == 0 && hitColorA != 0) return;
            }

            // early exit if outside threshold
            //if (CompareThreshold(paintColor.r,hitColorR) && CompareThreshold(paintColor.g,hitColorG) && CompareThreshold(paintColor.b,hitColorB) && CompareThreshold(paintColor.b,hitColorA)) return;
            if (paintColor.r == hitColorR && paintColor.g == hitColorG && paintColor.b == hitColorB && paintColor.a == hitColorA) return;

            Queue<int> fillPointX = new Queue<int>();
            Queue<int> fillPointY = new Queue<int>();
            fillPointX.Enqueue(x);
            fillPointY.Enqueue(y);

            int ptsx, ptsy;
            int pixel = 0;

            lockMaskPixels = new byte[texWidth * texHeight * 4];

            while (fillPointX.Count > 0)
            {

                ptsx = fillPointX.Dequeue();
                ptsy = fillPointY.Dequeue();

                if (ptsy - 1 > -1)
                {
                    pixel = (texWidth * (ptsy - 1) + ptsx) * 4; // down
                    if (lockMaskPixels[pixel] == 0
                        && CompareThreshold(pixels[pixel + 0], hitColorR)
                        && CompareThreshold(pixels[pixel + 1], hitColorG)
                        && CompareThreshold(pixels[pixel + 2], hitColorB)
                        && CompareThreshold(pixels[pixel + 3], hitColorA))
                    {
                        fillPointX.Enqueue(ptsx);
                        fillPointY.Enqueue(ptsy - 1);
                        DrawPoint(pixel);
                        lockMaskPixels[pixel] = 1;
                    }
                }

                if (ptsx + 1 < texWidth)
                {
                    pixel = (texWidth * ptsy + ptsx + 1) * 4; // right
                    if (lockMaskPixels[pixel] == 0
                        && CompareThreshold(pixels[pixel + 0], hitColorR)
                        && CompareThreshold(pixels[pixel + 1], hitColorG)
                        && CompareThreshold(pixels[pixel + 2], hitColorB)
                        && CompareThreshold(pixels[pixel + 3], hitColorA))
                    {
                        fillPointX.Enqueue(ptsx + 1);
                        fillPointY.Enqueue(ptsy);
                        DrawPoint(pixel);
                        lockMaskPixels[pixel] = 1;
                    }
                }

                if (ptsx - 1 > -1)
                {
                    pixel = (texWidth * ptsy + ptsx - 1) * 4; // left
                    if (lockMaskPixels[pixel] == 0
                        && CompareThreshold(pixels[pixel + 0], hitColorR)
                        && CompareThreshold(pixels[pixel + 1], hitColorG)
                        && CompareThreshold(pixels[pixel + 2], hitColorB)
                        && CompareThreshold(pixels[pixel + 3], hitColorA))
                    {
                        fillPointX.Enqueue(ptsx - 1);
                        fillPointY.Enqueue(ptsy);
                        DrawPoint(pixel);
                        lockMaskPixels[pixel] = 1;
                    }
                }

                if (ptsy + 1 < texHeight)
                {
                    pixel = (texWidth * (ptsy + 1) + ptsx) * 4; // up
                    if (lockMaskPixels[pixel] == 0
                        && CompareThreshold(pixels[pixel + 0], hitColorR)
                        && CompareThreshold(pixels[pixel + 1], hitColorG)
                        && CompareThreshold(pixels[pixel + 2], hitColorB)
                        && CompareThreshold(pixels[pixel + 3], hitColorA))
                    {
                        fillPointX.Enqueue(ptsx);
                        fillPointY.Enqueue(ptsy + 1);
                        DrawPoint(pixel);
                        lockMaskPixels[pixel] = 1;
                    }
                }
            }
        } // floodfillWithTreshold


        void LockAreaFill(int x, int y)
        {

            byte hitColorR = pixels[((texWidth * (y) + x) * 4) + 0];
            byte hitColorG = pixels[((texWidth * (y) + x) * 4) + 1];
            byte hitColorB = pixels[((texWidth * (y) + x) * 4) + 2];
            byte hitColorA = pixels[((texWidth * (y) + x) * 4) + 3];

            if (!canDrawOnBlack)
            {
                if (hitColorR == 0 && hitColorG == 0 && hitColorB == 0 && hitColorA != 0) return;
            }

            Queue<int> fillPointX = new Queue<int>();
            Queue<int> fillPointY = new Queue<int>();
            fillPointX.Enqueue(x);
            fillPointY.Enqueue(y);

            int ptsx, ptsy;
            int pixel = 0;

            lockMaskPixels = new byte[texWidth * texHeight * 4];

            while (fillPointX.Count > 0)
            {

                ptsx = fillPointX.Dequeue();
                ptsy = fillPointY.Dequeue();

                if (ptsy - 1 > -1)
                {
                    pixel = (texWidth * (ptsy - 1) + ptsx) * 4; // down

                    if (lockMaskPixels[pixel] == 0
                        && (pixels[pixel + 0] == hitColorR || pixels[pixel + 0] == paintColor.r)
                        && (pixels[pixel + 1] == hitColorG || pixels[pixel + 1] == paintColor.g)
                        && (pixels[pixel + 2] == hitColorB || pixels[pixel + 2] == paintColor.b)
                        && (pixels[pixel + 3] == hitColorA || pixels[pixel + 3] == paintColor.a))
                    {
                        fillPointX.Enqueue(ptsx);
                        fillPointY.Enqueue(ptsy - 1);
                        lockMaskPixels[pixel] = 1;
                    }
                }

                if (ptsx + 1 < texWidth)
                {
                    pixel = (texWidth * ptsy + ptsx + 1) * 4; // right
                    if (lockMaskPixels[pixel] == 0
                        && (pixels[pixel + 0] == hitColorR || pixels[pixel + 0] == paintColor.r)
                        && (pixels[pixel + 1] == hitColorG || pixels[pixel + 1] == paintColor.g)
                        && (pixels[pixel + 2] == hitColorB || pixels[pixel + 2] == paintColor.b)
                        && (pixels[pixel + 3] == hitColorA || pixels[pixel + 3] == paintColor.a))
                    {
                        fillPointX.Enqueue(ptsx + 1);
                        fillPointY.Enqueue(ptsy);
                        lockMaskPixels[pixel] = 1;
                    }
                }

                if (ptsx - 1 > -1)
                {
                    pixel = (texWidth * ptsy + ptsx - 1) * 4; // left
                    if (lockMaskPixels[pixel] == 0
                        && (pixels[pixel + 0] == hitColorR || pixels[pixel + 0] == paintColor.r)
                        && (pixels[pixel + 1] == hitColorG || pixels[pixel + 1] == paintColor.g)
                        && (pixels[pixel + 2] == hitColorB || pixels[pixel + 2] == paintColor.b)
                        && (pixels[pixel + 3] == hitColorA || pixels[pixel + 3] == paintColor.a))
                    {
                        fillPointX.Enqueue(ptsx - 1);
                        fillPointY.Enqueue(ptsy);
                        lockMaskPixels[pixel] = 1;
                    }
                }

                if (ptsy + 1 < texHeight)
                {
                    pixel = (texWidth * (ptsy + 1) + ptsx) * 4; // up
                    if (lockMaskPixels[pixel] == 0
                        && (pixels[pixel + 0] == hitColorR || pixels[pixel + 0] == paintColor.r)
                        && (pixels[pixel + 1] == hitColorG || pixels[pixel + 1] == paintColor.g)
                        && (pixels[pixel + 2] == hitColorB || pixels[pixel + 2] == paintColor.b)
                        && (pixels[pixel + 3] == hitColorA || pixels[pixel + 3] == paintColor.a))
                    {
                        fillPointX.Enqueue(ptsx);
                        fillPointY.Enqueue(ptsy + 1);
                        lockMaskPixels[pixel] = 1;
                    }
                }
            }
        } // LockAreaFill


        void LockAreaFillMaskOnly(int x, int y)
        {
            byte hitColorR = maskPixels[((texWidth * (y) + x) * 4) + 0];
            byte hitColorG = maskPixels[((texWidth * (y) + x) * 4) + 1];
            byte hitColorB = maskPixels[((texWidth * (y) + x) * 4) + 2];
            byte hitColorA = maskPixels[((texWidth * (y) + x) * 4) + 3];

            if (!canDrawOnBlack)
            {
                if (hitColorR == 0 && hitColorG == 0 && hitColorB == 0 && hitColorA != 0) return;
            }

            Queue<int> fillPointX = new Queue<int>();
            Queue<int> fillPointY = new Queue<int>();
            fillPointX.Enqueue(x);
            fillPointY.Enqueue(y);

            int ptsx, ptsy;
            int pixel = 0;

            lockMaskPixels = new byte[texWidth * texHeight * 4];

            while (fillPointX.Count > 0)
            {

                ptsx = fillPointX.Dequeue();
                ptsy = fillPointY.Dequeue();

                if (ptsy - 1 > -1)
                {
                    pixel = (texWidth * (ptsy - 1) + ptsx) * 4; // down

                    if (lockMaskPixels[pixel] == 0
                        && (maskPixels[pixel + 0] == hitColorR || maskPixels[pixel + 0] == paintColor.r)
                        && (maskPixels[pixel + 1] == hitColorG || maskPixels[pixel + 1] == paintColor.g)
                        && (maskPixels[pixel + 2] == hitColorB || maskPixels[pixel + 2] == paintColor.b)
                        && (maskPixels[pixel + 3] == hitColorA || maskPixels[pixel + 3] == paintColor.a))
                    {
                        fillPointX.Enqueue(ptsx);
                        fillPointY.Enqueue(ptsy - 1);
                        lockMaskPixels[pixel] = 1;
                    }
                }

                if (ptsx + 1 < texWidth)
                {
                    pixel = (texWidth * ptsy + ptsx + 1) * 4; // right
                    if (lockMaskPixels[pixel] == 0
                        && (maskPixels[pixel + 0] == hitColorR || maskPixels[pixel + 0] == paintColor.r)
                        && (maskPixels[pixel + 1] == hitColorG || maskPixels[pixel + 1] == paintColor.g)
                        && (maskPixels[pixel + 2] == hitColorB || maskPixels[pixel + 2] == paintColor.b)
                        && (maskPixels[pixel + 3] == hitColorA || maskPixels[pixel + 3] == paintColor.a))
                    {
                        fillPointX.Enqueue(ptsx + 1);
                        fillPointY.Enqueue(ptsy);
                        lockMaskPixels[pixel] = 1;
                    }
                }

                if (ptsx - 1 > -1)
                {
                    pixel = (texWidth * ptsy + ptsx - 1) * 4; // left
                    if (lockMaskPixels[pixel] == 0
                        && (maskPixels[pixel + 0] == hitColorR || maskPixels[pixel + 0] == paintColor.r)
                        && (maskPixels[pixel + 1] == hitColorG || maskPixels[pixel + 1] == paintColor.g)
                        && (maskPixels[pixel + 2] == hitColorB || maskPixels[pixel + 2] == paintColor.b)
                        && (maskPixels[pixel + 3] == hitColorA || maskPixels[pixel + 3] == paintColor.a))
                    {
                        fillPointX.Enqueue(ptsx - 1);
                        fillPointY.Enqueue(ptsy);
                        lockMaskPixels[pixel] = 1;
                    }
                }

                if (ptsy + 1 < texHeight)
                {
                    pixel = (texWidth * (ptsy + 1) + ptsx) * 4; // up
                    if (lockMaskPixels[pixel] == 0
                        && (maskPixels[pixel + 0] == hitColorR || maskPixels[pixel + 0] == paintColor.r)
                        && (maskPixels[pixel + 1] == hitColorG || maskPixels[pixel + 1] == paintColor.g)
                        && (maskPixels[pixel + 2] == hitColorB || maskPixels[pixel + 2] == paintColor.b)
                        && (maskPixels[pixel + 3] == hitColorA || maskPixels[pixel + 3] == paintColor.a))
                    {
                        fillPointX.Enqueue(ptsx);
                        fillPointY.Enqueue(ptsy + 1);
                        lockMaskPixels[pixel] = 1;
                    }
                }
            }
        } // LockAreaFillMaskOnly


        // compares if two values are below threshold
        bool CompareThreshold(byte a, byte b)
        {
            //return Mathf.Abs(b-b)<=threshold;
            if (a < b) { a ^= b; b ^= a; a ^= b; } // http://lab.polygonal.de/?p=81
            return (a - b) <= paintThreshold;
        }

        // create locking mask floodfill, using threshold, checking pixels from mask only
        void LockAreaFillWithThresholdMaskOnly(int x, int y)
        {
            //			Debug.Log("LockAreaFillWithThresholdMaskOnly");
            // get canvas color from this point
            byte hitColorR = maskPixels[(texWidth * y + x) * 4 + 0];
            byte hitColorG = maskPixels[(texWidth * y + x) * 4 + 1];
            byte hitColorB = maskPixels[(texWidth * y + x) * 4 + 2];
            byte hitColorA = maskPixels[(texWidth * y + x) * 4 + 3];

            if (!canDrawOnBlack)
            {
                if (hitColorR == 0 && hitColorG == 0 && hitColorB == 0 && hitColorA != 0) return;
            }

            Queue<int> fillPointX = new Queue<int>();
            Queue<int> fillPointY = new Queue<int>();
            fillPointX.Enqueue(x);
            fillPointY.Enqueue(y);

            int ptsx, ptsy;
            int pixel = 0;

            lockMaskPixels = new byte[texWidth * texHeight * 4];

            while (fillPointX.Count > 0)
            {

                ptsx = fillPointX.Dequeue();
                ptsy = fillPointY.Dequeue();

                if (ptsy - 1 > -1)
                {
                    pixel = (texWidth * (ptsy - 1) + ptsx) * 4; // down

                    if (lockMaskPixels[pixel] == 0 // this pixel is not used yet
                        && (CompareThreshold(maskPixels[pixel + 0], hitColorR))
                        && (CompareThreshold(maskPixels[pixel + 1], hitColorG))
                        && (CompareThreshold(maskPixels[pixel + 2], hitColorB))
                        && (CompareThreshold(maskPixels[pixel + 3], hitColorA)))
                    {
                        fillPointX.Enqueue(ptsx);
                        fillPointY.Enqueue(ptsy - 1);
                        lockMaskPixels[pixel] = 1;
                    }
                }

                if (ptsx + 1 < texWidth)
                {
                    pixel = (texWidth * ptsy + ptsx + 1) * 4; // right
                    if (lockMaskPixels[pixel] == 0
                        && (CompareThreshold(maskPixels[pixel + 0], hitColorR))
                        && (CompareThreshold(maskPixels[pixel + 1], hitColorG))
                        && (CompareThreshold(maskPixels[pixel + 2], hitColorB))
                        && (CompareThreshold(maskPixels[pixel + 3], hitColorA)))
                    {
                        fillPointX.Enqueue(ptsx + 1);
                        fillPointY.Enqueue(ptsy);
                        lockMaskPixels[pixel] = 1;
                    }
                }

                if (ptsx - 1 > -1)
                {
                    pixel = (texWidth * ptsy + ptsx - 1) * 4; // left
                    if (lockMaskPixels[pixel] == 0
                        && (CompareThreshold(maskPixels[pixel + 0], hitColorR))
                        && (CompareThreshold(maskPixels[pixel + 1], hitColorG))
                        && (CompareThreshold(maskPixels[pixel + 2], hitColorB))
                        && (CompareThreshold(maskPixels[pixel + 3], hitColorA)))
                    {
                        fillPointX.Enqueue(ptsx - 1);
                        fillPointY.Enqueue(ptsy);
                        lockMaskPixels[pixel] = 1;
                    }
                }

                if (ptsy + 1 < texHeight)
                {
                    pixel = (texWidth * (ptsy + 1) + ptsx) * 4; // up
                    if (lockMaskPixels[pixel] == 0
                        && (CompareThreshold(maskPixels[pixel + 0], hitColorR))
                        && (CompareThreshold(maskPixels[pixel + 1], hitColorG))
                        && (CompareThreshold(maskPixels[pixel + 2], hitColorB))
                        && (CompareThreshold(maskPixels[pixel + 3], hitColorA)))
                    {
                        fillPointX.Enqueue(ptsx);
                        fillPointY.Enqueue(ptsy + 1);
                        lockMaskPixels[pixel] = 1;
                    }
                }
            }
        } // LockAreaFillWithThresholdMaskOnly


        void LockAreaFillWithThresholdMaskOnlyGetArea(int x, int y, bool getArea)
        {
            // temporary fix for IOS notification center pulldown crash
            if (x >= texWidth) x = texWidth - 1;
            if (y >= texHeight) y = texHeight - 1;

            int fullArea = 0;
            int alreadyFilled = 0;

            // get canvas color from this point
            byte hitColorR = maskPixels[(texWidth * y + x) * 4 + 0];
            byte hitColorG = maskPixels[(texWidth * y + x) * 4 + 1];
            byte hitColorB = maskPixels[(texWidth * y + x) * 4 + 2];
            byte hitColorA = maskPixels[(texWidth * y + x) * 4 + 3];

            if (!canDrawOnBlack)
            {
                if (hitColorR == 0 && hitColorG == 0 && hitColorB == 0 && hitColorA != 0) return;
            }

            Queue<int> fillPointX = new Queue<int>();
            Queue<int> fillPointY = new Queue<int>();
            fillPointX.Enqueue(x);
            fillPointY.Enqueue(y);

            int ptsx, ptsy;
            int pixel = 0;

            lockMaskPixels = new byte[texWidth * texHeight * 4];

            while (fillPointX.Count > 0)
            {

                ptsx = fillPointX.Dequeue();
                ptsy = fillPointY.Dequeue();

                if (ptsy - 1 > -1)
                {
                    pixel = (texWidth * (ptsy - 1) + ptsx) * 4; // down

                    if (lockMaskPixels[pixel] == 0 // this pixel is not used yet
                        && (CompareThreshold(maskPixels[pixel + 0], hitColorR))
                        && (CompareThreshold(maskPixels[pixel + 1], hitColorG))
                        && (CompareThreshold(maskPixels[pixel + 2], hitColorB))
                        && (CompareThreshold(maskPixels[pixel + 3], hitColorA)))
                    {
                        fillPointX.Enqueue(ptsx);
                        fillPointY.Enqueue(ptsy - 1);
                        lockMaskPixels[pixel] = 1;
                        fullArea++;

                        if (IsSameColor(paintColor, pixels[pixel + 0], pixels[pixel + 1], pixels[pixel + 2]))
                        {
                            alreadyFilled++;
                        }

                    }
                }

                if (ptsx + 1 < texWidth)
                {
                    pixel = (texWidth * ptsy + ptsx + 1) * 4; // right
                    if (lockMaskPixels[pixel] == 0
                        && (CompareThreshold(maskPixels[pixel + 0], hitColorR))
                        && (CompareThreshold(maskPixels[pixel + 1], hitColorG))
                        && (CompareThreshold(maskPixels[pixel + 2], hitColorB))
                        && (CompareThreshold(maskPixels[pixel + 3], hitColorA)))
                    {
                        fillPointX.Enqueue(ptsx + 1);
                        fillPointY.Enqueue(ptsy);
                        lockMaskPixels[pixel] = 1;
                        fullArea++;
                        if (IsSameColor(paintColor, pixels[pixel + 0], pixels[pixel + 1], pixels[pixel + 2]))
                        {
                            alreadyFilled++;
                        }

                    }
                }

                if (ptsx - 1 > -1)
                {
                    pixel = (texWidth * ptsy + ptsx - 1) * 4; // left
                    if (lockMaskPixels[pixel] == 0
                        && (CompareThreshold(maskPixels[pixel + 0], hitColorR))
                        && (CompareThreshold(maskPixels[pixel + 1], hitColorG))
                        && (CompareThreshold(maskPixels[pixel + 2], hitColorB))
                        && (CompareThreshold(maskPixels[pixel + 3], hitColorA)))
                    {
                        fillPointX.Enqueue(ptsx - 1);
                        fillPointY.Enqueue(ptsy);
                        lockMaskPixels[pixel] = 1;
                        fullArea++;
                        if (IsSameColor(paintColor, pixels[pixel + 0], pixels[pixel + 1], pixels[pixel + 2]))
                        {
                            alreadyFilled++;
                        }
                    }
                }

                if (ptsy + 1 < texHeight)
                {
                    pixel = (texWidth * (ptsy + 1) + ptsx) * 4; // up
                    if (lockMaskPixels[pixel] == 0
                        && (CompareThreshold(maskPixels[pixel + 0], hitColorR))
                        && (CompareThreshold(maskPixels[pixel + 1], hitColorG))
                        && (CompareThreshold(maskPixels[pixel + 2], hitColorB))
                        && (CompareThreshold(maskPixels[pixel + 3], hitColorA)))
                    {
                        fillPointX.Enqueue(ptsx);
                        fillPointY.Enqueue(ptsy + 1);
                        lockMaskPixels[pixel] = 1;
                        fullArea++;
                        if (IsSameColor(paintColor, pixels[pixel + 0], pixels[pixel + 1], pixels[pixel + 2]))
                        {
                            alreadyFilled++;
                        }
                    }
                }
            } // while

            if (getArea)
            {
                if (AreaPaintedEvent != null) AreaPaintedEvent(fullArea, alreadyFilled, alreadyFilled / (float)fullArea * 100f, PixelToWorld(x, y));
            }

        } // void


        bool IsSameColor(Color32 a, byte r, byte g, byte b)
        {
            //Debug.Log(b+" : "+r+","+g+","+b);
            return (a.r == r && a.g == g && a.b == b);
        }

        // create locking mask floodfill, using threshold
        void LockMaskFillWithThreshold(int x, int y)
        {
            //			Debug.Log("LockMaskFillWithTreshold");
            // get canvas color from this point
            byte hitColorR = pixels[((texWidth * (y) + x) * 4) + 0];
            byte hitColorG = pixels[((texWidth * (y) + x) * 4) + 1];
            byte hitColorB = pixels[((texWidth * (y) + x) * 4) + 2];
            byte hitColorA = pixels[((texWidth * (y) + x) * 4) + 3];

            if (!canDrawOnBlack)
            {
                if (hitColorR == 0 && hitColorG == 0 && hitColorB == 0 && hitColorA != 0) return;
            }

            Queue<int> fillPointX = new Queue<int>();
            Queue<int> fillPointY = new Queue<int>();
            fillPointX.Enqueue(x);
            fillPointY.Enqueue(y);

            int ptsx, ptsy;
            int pixel = 0;

            lockMaskPixels = new byte[texWidth * texHeight * 4];

            while (fillPointX.Count > 0)
            {

                ptsx = fillPointX.Dequeue();
                ptsy = fillPointY.Dequeue();

                if (ptsy - 1 > -1)
                {
                    pixel = (texWidth * (ptsy - 1) + ptsx) * 4; // down

                    if (lockMaskPixels[pixel] == 0 // this pixel is not used yet
                        && (CompareThreshold(pixels[pixel + 0], hitColorR) || CompareThreshold(pixels[pixel + 0], paintColor.r)) // if pixel is same as hit color OR same as paint color
                        && (CompareThreshold(pixels[pixel + 1], hitColorG) || CompareThreshold(pixels[pixel + 1], paintColor.g))
                        && (CompareThreshold(pixels[pixel + 2], hitColorB) || CompareThreshold(pixels[pixel + 2], paintColor.b))
                        && (CompareThreshold(pixels[pixel + 3], hitColorA) || CompareThreshold(pixels[pixel + 3], paintColor.a)))
                    {
                        fillPointX.Enqueue(ptsx);
                        fillPointY.Enqueue(ptsy - 1);
                        lockMaskPixels[pixel] = 1;
                    }
                }

                if (ptsx + 1 < texWidth)
                {
                    pixel = (texWidth * ptsy + ptsx + 1) * 4; // right
                    if (lockMaskPixels[pixel] == 0
                        && (CompareThreshold(pixels[pixel + 0], hitColorR) || CompareThreshold(pixels[pixel + 0], paintColor.r)) // if pixel is same as hit color OR same as paint color
                        && (CompareThreshold(pixels[pixel + 1], hitColorG) || CompareThreshold(pixels[pixel + 1], paintColor.g))
                        && (CompareThreshold(pixels[pixel + 2], hitColorB) || CompareThreshold(pixels[pixel + 2], paintColor.b))
                        && (CompareThreshold(pixels[pixel + 3], hitColorA) || CompareThreshold(pixels[pixel + 3], paintColor.a)))
                    {
                        fillPointX.Enqueue(ptsx + 1);
                        fillPointY.Enqueue(ptsy);
                        lockMaskPixels[pixel] = 1;
                    }
                }

                if (ptsx - 1 > -1)
                {
                    pixel = (texWidth * ptsy + ptsx - 1) * 4; // left
                    if (lockMaskPixels[pixel] == 0
                        && (CompareThreshold(pixels[pixel + 0], hitColorR) || CompareThreshold(pixels[pixel + 0], paintColor.r)) // if pixel is same as hit color OR same as paint color
                        && (CompareThreshold(pixels[pixel + 1], hitColorG) || CompareThreshold(pixels[pixel + 1], paintColor.g))
                        && (CompareThreshold(pixels[pixel + 2], hitColorB) || CompareThreshold(pixels[pixel + 2], paintColor.b))
                        && (CompareThreshold(pixels[pixel + 3], hitColorA) || CompareThreshold(pixels[pixel + 3], paintColor.a)))
                    {
                        fillPointX.Enqueue(ptsx - 1);
                        fillPointY.Enqueue(ptsy);
                        lockMaskPixels[pixel] = 1;
                    }
                }

                if (ptsy + 1 < texHeight)
                {
                    pixel = (texWidth * (ptsy + 1) + ptsx) * 4; // up
                    if (lockMaskPixels[pixel] == 0
                        && (CompareThreshold(pixels[pixel + 0], hitColorR) || CompareThreshold(pixels[pixel + 0], paintColor.r)) // if pixel is same as hit color OR same as paint color
                        && (CompareThreshold(pixels[pixel + 1], hitColorG) || CompareThreshold(pixels[pixel + 1], paintColor.g))
                        && (CompareThreshold(pixels[pixel + 2], hitColorB) || CompareThreshold(pixels[pixel + 2], paintColor.b))
                        && (CompareThreshold(pixels[pixel + 3], hitColorA) || CompareThreshold(pixels[pixel + 3], paintColor.a)))
                    {
                        fillPointX.Enqueue(ptsx);
                        fillPointY.Enqueue(ptsy + 1);
                        lockMaskPixels[pixel] = 1;
                    }
                }
            }
        } // LockMaskFillWithTreshold


        // get custom brush texture into custombrushpixels array, this needs to be called if custom brush is changed
        public void ReadCurrentCustomBrush()
        {
            // NOTE: this works only for square brushes
            customBrushWidth = customBrushes[selectedBrush].width;
            customBrushHeight = customBrushes[selectedBrush].height;
            customBrushBytes = new byte[customBrushWidth * customBrushHeight * 4];

            int pixel = 0;
            Color32[] brushPixel = customBrushes[selectedBrush].GetPixels32();
            for (int y = 0; y < customBrushHeight; y++)
            {
                for (int x = 0; x < customBrushWidth; x++)
                {
                    customBrushBytes[pixel] = brushPixel[x+y*customBrushWidth].r;
                    customBrushBytes[pixel + 1] = brushPixel[x + y * customBrushWidth].g;
                    customBrushBytes[pixel + 2] = brushPixel[x + y * customBrushWidth].b;
                    customBrushBytes[pixel + 3] = brushPixel[x + y * customBrushWidth].a;
                    pixel += 4;
                }
            }

            // precalculate few brush size values
            customBrushWidthHalf = (int)(customBrushWidth * 0.5f);
            texWidthMinusCustomBrushWidth = texWidth - customBrushWidth;
            texHeightMinusCustomBrushHeight = texHeight - customBrushHeight;
        }


        // reads current texture pattern into pixel array, NOTE: only works with square textures
        public void ReadCurrentCustomPattern()
        {
            if (customPatterns == null || customPatterns.Length == 0 || customPatterns[selectedPattern] == null) { Debug.LogError("Problem: No custom patterns assigned on " + gameObject.name); return; }

            customPatternWidth = customPatterns[selectedPattern].width;
            customPatternHeight = customPatterns[selectedPattern].height;
            patternBrushBytes = new byte[customPatternWidth * customPatternHeight * 4];

            int pixel = 0;
            Color32[] brushPixel = customPatterns[selectedPattern].GetPixels32();

            for (int x = 0; x < customPatternWidth; x++)
            {
                for (int y = 0; y < customPatternHeight; y++)
                {
                    patternBrushBytes[pixel] = brushPixel[x + y * customPatternWidth].r;
                    patternBrushBytes[pixel + 1] = brushPixel[x + y * customPatternWidth].g;
                    patternBrushBytes[pixel + 2] = brushPixel[x + y * customPatternWidth].b;
                    patternBrushBytes[pixel + 3] = brushPixel[x + y * customPatternWidth].a;

                    pixel += 4;
                }
            }
        }

        // draws single point to this pixel coordinate, with current paint color
        public void DrawPoint(int x, int y)
        {
            int pixel = (texWidth * y + x) * 4;
            pixels[pixel] = paintColor.r;
            pixels[pixel + 1] = paintColor.g;
            pixels[pixel + 2] = paintColor.b;
            pixels[pixel + 3] = paintColor.a;
        }


        // draws single point to this pixel array index, with current paint color
        public void DrawPoint(int pixel)
        {
            pixels[pixel] = paintColor.r;
            pixels[pixel + 1] = paintColor.g;
            pixels[pixel + 2] = paintColor.b;
            pixels[pixel + 3] = paintColor.a;
        }


        // draw line between 2 points (if moved too far/fast)
        // http://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm
        public void DrawLine(int startX, int startY, int endX, int endY)
        {
            int x1 = endX;
            int y1 = endY;
            int tempVal = x1 - startX;
            int dx = (tempVal + (tempVal >> 31)) ^ (tempVal >> 31); // http://stackoverflow.com/questions/6114099/fast-integer-abs-function
            tempVal = y1 - startY;
            int dy = (tempVal + (tempVal >> 31)) ^ (tempVal >> 31);


            int sx = startX < x1 ? 1 : -1;
            int sy = startY < y1 ? 1 : -1;
            int err = dx - dy;
            int pixelCount = 0;
            int e2;
            for (;;) // endless loop
            {
                if (hiQualityBrush)
                {
                    DrawCircle(startX, startY);
                }
                else {
                    pixelCount++;
                    if (pixelCount > brushSizeDiv4) // might have small gaps if this is used, but its alot(tm) faster to skip few pixels
                    {
                        pixelCount = 0;
                        DrawCircle(startX, startY);
                    }
                }

                if (startX == x1 && startY == y1) break;
                e2 = 2 * err;
                if (e2 > -dy)
                {
                    err = err - dy;
                    startX = startX + sx;
                }
                else if (e2 < dx)
                {
                    err = err + dx;
                    startY = startY + sy;
                }
            }
        } // drawline

        public void DrawLine(Vector2 start, Vector2 end)
        {
            DrawLine((int)start.x, (int)start.y, (int)end.x, (int)end.y);
        }


        void DrawLineWithBrush(Vector2 start, Vector2 end)
        {
            int x0 = (int)start.x;
            int y0 = (int)start.y;
            int x1 = (int)end.x;
            int y1 = (int)end.y;
            int tempVal = x1 - x0;
            int dx = (tempVal + (tempVal >> 31)) ^ (tempVal >> 31); // http://stackoverflow.com/questions/6114099/fast-integer-abs-function
            tempVal = y1 - y0;
            int dy = (tempVal + (tempVal >> 31)) ^ (tempVal >> 31);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            int pixelCount = 0;
            int e2;
            for (;;)
            {
                if (hiQualityBrush)
                {
                    DrawCustomBrush(x0, y0);
                }
                else {
                    pixelCount++;
                    if (pixelCount > brushSizeDiv4)
                    {
                        pixelCount = 0;
                        DrawCustomBrush(x0, y0);
                    }
                }
                if (x0 == x1 && y0 == y1) break;
                e2 = 2 * err;
                if (e2 > -dy)
                {
                    err = err - dy;
                    x0 = x0 + sx;
                }
                else if (e2 < dx)
                {
                    err = err + dx;
                    y0 = y0 + sy;
                }
            }
        }


        void DrawLineWithPattern(Vector2 start, Vector2 end)
        {
            int x0 = (int)start.x;
            int y0 = (int)start.y;
            int x1 = (int)end.x;
            int y1 = (int)end.y;
            int tempVal = x1 - x0;
            int dx = (tempVal + (tempVal >> 31)) ^ (tempVal >> 31); // http://stackoverflow.com/questions/6114099/fast-integer-abs-function
            tempVal = y1 - y0;
            int dy = (tempVal + (tempVal >> 31)) ^ (tempVal >> 31);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            int pixelCount = 0;
            int e2;
            for (;;)
            {
                if (hiQualityBrush)
                {
                    DrawPatternCircle(x0, y0);
                }
                else {
                    pixelCount++;
                    if (pixelCount > brushSizeDiv4)
                    {
                        pixelCount = 0;
                        DrawPatternCircle(x0, y0);
                    }
                }

                if ((x0 == x1) && (y0 == y1)) break;
                e2 = 2 * err;
                if (e2 > -dy)
                {
                    err = err - dy;
                    x0 = x0 + sx;
                }
                else if (e2 < dx)
                {
                    err = err + dx;
                    y0 = y0 + sy;
                }
            }
        }

        void EraseWithImageLine(Vector2 start, Vector2 end)
        {
            int x0 = (int)start.x;
            int y0 = (int)start.y;
            int x1 = (int)end.x;
            int y1 = (int)end.y;
            int tempVal = x1 - x0;
            int dx = (tempVal + (tempVal >> 31)) ^ (tempVal >> 31); // http://stackoverflow.com/questions/6114099/fast-integer-abs-function
            tempVal = y1 - y0;
            int dy = (tempVal + (tempVal >> 31)) ^ (tempVal >> 31);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            int pixelCount = 0;
            int e2;
            for (;;)
            {
                if (hiQualityBrush)
                {
                    EraseWithImage(x0, y0);
                }
                else {
                    pixelCount++;
                    if (pixelCount > brushSizeDiv4)
                    {
                        pixelCount = 0;
                        EraseWithImage(x0, y0);
                    }
                }

                if ((x0 == x1) && (y0 == y1)) break;
                e2 = 2 * err;
                if (e2 > -dy)
                {
                    err = err - dy;
                    x0 = x0 + sx;
                }
                else if (e2 < dx)
                {
                    err = err + dx;
                    y0 = y0 + sy;
                }
            }
        }

        void EraseWithBackgroundColorLine(Vector2 start, Vector2 end)
        {
            int x0 = (int)start.x;
            int y0 = (int)start.y;
            int x1 = (int)end.x;
            int y1 = (int)end.y;
            int tempVal = x1 - x0;
            int dx = (tempVal + (tempVal >> 31)) ^ (tempVal >> 31); // http://stackoverflow.com/questions/6114099/fast-integer-abs-function
            tempVal = y1 - y0;
            int dy = (tempVal + (tempVal >> 31)) ^ (tempVal >> 31);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            int pixelCount = 0;
            int e2;
            for (;;)
            {
                if (hiQualityBrush)
                {
                    EraseWithBackgroundColor(x0, y0);
                }
                else {
                    pixelCount++;
                    if (pixelCount > brushSizeDiv4)
                    {
                        pixelCount = 0;
                        EraseWithBackgroundColor(x0, y0);
                    }
                }

                if ((x0 == x1) && (y0 == y1)) break;
                e2 = 2 * err;
                if (e2 > -dy)
                {
                    err = err - dy;
                    x0 = x0 + sx;
                }
                else if (e2 < dx)
                {
                    err = err + dx;
                    y0 = y0 + sy;
                }
            }
        }


        /*
		// Bresenham line with custom width, not used yet, still broken
		// TODO: fix this.. http://members.chello.at/~easyfilter/bresenham.html
		void DrawLineWidth(Vector2 start, Vector2 end, float wd)
		//		void DrawLineWidth(int x0, int y0, int x1, int y1, float wd)
		{ 
			int x0=(int)start.x;
			int y0=(int)start.y;
			int x1=(int)end.x;
			int y1=(int)end.y;
			
			int dx = Mathf.Abs(x1-x0), sx = x0 < x1 ? 1 : -1; 
			int dy = Mathf.Abs(y1-y0), sy = y0 < y1 ? 1 : -1; 
			int err = dx-dy, e2, x2, y2;                          // error value e_xy 
			float ed = dx+dy == 0 ? 1 : Mathf.Sqrt((float)dx*dx+(float)dy*dy);
			
			for (wd = (wd+1)/2;;) // pixel loop
			{                                   
				DrawPoint(x0, y0);
				e2 = err;
				x2 = x0;
				if (2*e2 >= -dx) // x step
				{                                           
					for (e2 += dy, y2 = y0; e2 < ed*wd && (y1 != y2 || dx > dy); e2 += dx)
					{
						DrawPoint(x0, y2 += sy);
					}
					if (x0 == x1) break;
					e2 = err; err -= dy; x0 += sx; 
				} 
				
				if (2*e2 <= dy) // y step
				{                                            
					for (e2 = dx-e2; e2 < ed*wd && (x1 != x2 || dx < dy); e2 += dy)
					{
						DrawPoint(x2 += sx, y0);
					}
					if (y0 == y1) break;
					err += dx; y0 += sy; 
				}
			}
		} // DrawLineWidth
		*/


        // Basic undo function, copies original array (before drawing) into the image and applies it
        public void DoUndo()
        {
            if (undoEnabled)
            {
                if (undoPixels.Count > 0)
                {
                    System.Array.Copy(undoPixels[undoPixels.Count - 1], pixels, undoPixels[undoPixels.Count - 1].Length);
                    drawingTexture.LoadRawTextureData(undoPixels[undoPixels.Count - 1]);
                    drawingTexture.Apply(false);

                    undoPixels.RemoveAt(undoPixels.Count - 1);
                } // else, no undo available
            }
        }

        public void GrabUndoBufferNow()
        {
            // TODO: remove oldest item, if too many buffers
            if (undoPixels.Count >= maxUndoBuffers)
            {
                undoPixels.RemoveAt(0);
            }

            undoPixels.Add(new byte[texWidth * texHeight * 4]); // TODO: need to reset size, if image size changes
            System.Array.Copy(pixels, undoPixels[undoPixels.Count - 1], pixels.Length);
        }

        // if this is called, undo buffer gets updated
        public void ClearImage()
        {
            ClearImage(true);
        }

        // this override can be called with bool, to disable undo buffer grab
        public void ClearImage(bool updateUndoBuffer)
        {

            if (undoEnabled && updateUndoBuffer)
            {
                GrabUndoBufferNow();
            }


            if (usingClearingImage)
            {
                ClearImageWithImage();
            }
            else {

                int pixel = 0;
                for (int y = 0; y < texHeight; y++)
                {
                    for (int x = 0; x < texWidth; x++)
                    {
                        pixels[pixel] = clearColor.r;
                        pixels[pixel + 1] = clearColor.g;
                        pixels[pixel + 2] = clearColor.b;
                        pixels[pixel + 3] = clearColor.a;
                        pixel += 4;
                    }
                }

                UpdateTexture();
            }
        } // clear image


        public void ClearImageWithImage()
        {
            // fill pixels array with clearpixels array
            System.Array.Copy(clearPixels, 0, pixels, 0, clearPixels.Length);


            // just assign our clear image array into tex
            drawingTexture.LoadRawTextureData(clearPixels);
            drawingTexture.Apply(false);
        } // clear image


        public void ReadMaskImage()
        {
            maskPixels = new byte[texWidth * texHeight * 4];

            int smoothenResolution = 5; // currently fixed value
            int smoothArea = smoothenResolution * smoothenResolution;
            int smoothCenter = Mathf.FloorToInt(smoothenResolution / 2);

            int pixel = 0;
            Color c;
            
            for (int y = 0; y < texHeight; y++)
            {
                for (int x = 0; x < texWidth; x++)
                {

                    if (smoothenMaskEdges)
                    {
                        c = new Color(0, 0, 0, 0);
                        c = maskTex.GetPixel(x, y); // center

                        if (c.a > 0)
                        {
                            for (int i = 0; i < smoothArea; i++)
                            {
                                int xx = (i / smoothenResolution) | 0; // 0, 0, 0
                                int yy = i % smoothenResolution;
                                if (maskTex.GetPixel(x + xx - smoothCenter, y + yy - smoothCenter).a < (255 - paintThreshold) / 255f)
                                {
                                    c = new Color(0, 0, 0, 0);
                                }
                            }
                        }
                    }
                    else { // default (works well if texture is "point" filter mode
                        c = maskTex.GetPixel(x, y);
                    }
                    maskPixels[pixel] = (byte)(c.r * 255);
                    maskPixels[pixel + 1] = (byte)(c.g * 255);
                    maskPixels[pixel + 2] = (byte)(c.b * 255);
                    maskPixels[pixel + 3] = (byte)(c.a * 255);
                    pixel += 4;
                }
            }

        }

        // reads original drawing canvas texture, so than when Clear image is called, we can restore the original pixels
        public void ReadClearingImage()
        {
            clearPixels = new byte[texWidth * texHeight * 4];

            //Debug.Log(myRenderer.material.HasProperty(targetTexture));
            //Debug.Log(myRenderer.material.GetTexture(targetTexture));

            // get our current texture into tex, is this needed?, also targettexture might be different..?
            // FIXME: usually target texture is same as drawingTexture..?
            drawingTexture.SetPixels32(((Texture2D)myRenderer.material.GetTexture(targetTexture)).GetPixels32());
            drawingTexture.Apply(false);

            int pixel = 0;
            Color32[] tempPixels = drawingTexture.GetPixels32();
            int tempCount = tempPixels.Length;

            for (int i = 0; i < tempCount; i++)
            {
                clearPixels[pixel] = tempPixels[i].r;
                clearPixels[pixel + 1] = tempPixels[i].g;
                clearPixels[pixel + 2] = tempPixels[i].b;
                clearPixels[pixel + 3] = tempPixels[i].a;
                pixel += 4;
            }
        }

        void CreateFullScreenQuad()
        {
            // create mesh plane, fits in camera view (with screensize adjust taken into consideration)
            Mesh go_Mesh = GetComponent<MeshFilter>().mesh;
            go_Mesh.Clear();
            Vector3[] referenceCorners = new Vector3[4];

            if (referenceArea) // use reference object & canvas scaling for size
            {
                if (referenceArea == null) Debug.LogError("RectTransform not assigned in " + transform.name, gameObject);

                // NOTE: this fails, if canvas is not direct parent of the referenceArea object?
                var temporaryCanvasArray = referenceArea.GetComponentsInParent<Canvas>();

                if (temporaryCanvasArray == null || temporaryCanvasArray.Length == 0) Debug.LogError("Canvas not found from ReferenceArea parent", gameObject);
                if (temporaryCanvasArray.Length > 1) Debug.LogError("More than 1 Canvas was found from ReferenceArea parent, can cause problems", gameObject);

                var referenceCanvas = temporaryCanvasArray[0]; // take first canvas
                if (referenceCanvas == null) Debug.LogError("Canvas not found from ReferenceArea parent", gameObject);

                // get current scale factor
                canvasScaleFactor = referenceCanvas.scaleFactor;

                // get vertex positions for borders
                referenceCorners[0] = new Vector3(referenceArea.offsetMin.x * canvasScaleFactor, referenceArea.offsetMin.y * canvasScaleFactor, 0);
                referenceCorners[1] = new Vector3(referenceArea.offsetMin.x * canvasScaleFactor, Screen.height + referenceArea.offsetMax.y * canvasScaleFactor, 0);
                referenceCorners[2] = new Vector3(Screen.width + referenceArea.offsetMax.x * canvasScaleFactor, Screen.height + referenceArea.offsetMax.y * canvasScaleFactor, 0);
                referenceCorners[3] = new Vector3(Screen.width + referenceArea.offsetMax.x * canvasScaleFactor, referenceArea.offsetMin.y * canvasScaleFactor, 0);

                // reset Z position and center/scale to camera view
                for (int i = 0; i < referenceCorners.Length; i++)
                {
                    referenceCorners[i] = referenceCorners[i];
                    referenceCorners[i].z = -cam.transform.position.z;
                }
                go_Mesh.vertices = referenceCorners;

            }
            else { // just use full screen quad for main camera


                referenceCorners[0] = new Vector3(0, canvasSizeAdjust.y, cam.nearClipPlane); // bottom left
                referenceCorners[1] = new Vector3(0, cam.pixelHeight + canvasSizeAdjust.y, cam.nearClipPlane); // top left
                referenceCorners[2] = new Vector3(cam.pixelWidth + canvasSizeAdjust.x, cam.pixelHeight + canvasSizeAdjust.y, cam.nearClipPlane); // top right
                referenceCorners[3] = new Vector3(cam.pixelWidth + canvasSizeAdjust.x, canvasSizeAdjust.y, cam.nearClipPlane); // bottom right
            }

            // move to screen
            float nearClipOffset = 0.01f; // otherwise raycast wont hit, if exactly at nearclip z


            for (int i = 0; i < referenceCorners.Length; i++)
            {
                referenceCorners[i].z = -cam.transform.position.z + nearClipOffset;
                referenceCorners[i] = cam.ScreenToWorldPoint(referenceCorners[i]);
            }


            go_Mesh.vertices = referenceCorners;

            go_Mesh.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
            go_Mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };

            go_Mesh.RecalculateNormals();
            go_Mesh.RecalculateBounds();

            go_Mesh.tangents = new[] { new Vector4(1.0f, 0.0f, 0.0f, -1.0f), new Vector4(1.0f, 0.0f, 0.0f, -1.0f), new Vector4(1.0f, 0.0f, 0.0f, -1.0f), new Vector4(1.0f, 0.0f, 0.0f, -1.0f) };


            // add mesh collider
            if (gameObject.GetComponent<MeshCollider>() == null) gameObject.AddComponent<MeshCollider>();
        }


        public void SetBrushSize(int newSize)
        {
            brushSize = (int)Mathf.Clamp(newSize, 1, 999);

            brushSizeX1 = brushSize << 1;
            brushSizeXbrushSize = brushSize * brushSize;
            brushSizeX4 = brushSizeXbrushSize << 2;
            brushSizeDiv4 = hiQualityBrush ? 0 : brushSize >> 2;

            UpdateLineModePreviewObjects();
        }

        public void SetDrawModeLine()
        {
            drawMode = DrawMode.ShapeLines;
        }


        public void SetDrawModeBrush()
        {
            drawMode = DrawMode.Default;
        }

        public void SetDrawModeFill()
        {
            drawMode = DrawMode.FloodFill;
        }

        public void SetDrawModeShapes()
        {
            drawMode = DrawMode.CustomBrush;
        }

        public void SetDrawModePattern()
        {
            drawMode = DrawMode.Pattern;
        }

        public void SetDrawModeEraser()
        {
            drawMode = DrawMode.Eraser;
        }

        // returns current image (later: including all layers) as Texture2D
        public Texture2D GetCanvasAsTexture()
        {
            var image = new Texture2D((int)(texWidth / resolutionScaler), (int)(texHeight / resolutionScaler), TextureFormat.RGBA32, false);

            /*
			Mesh go_Mesh = GetComponent<MeshFilter>().mesh;
			var topLeft = cam.WorldToScreenPoint(go_Mesh.vertices[0]);
			var topRight= cam.WorldToScreenPoint(go_Mesh.vertices[3]);
			var bottomRight = cam.WorldToScreenPoint(go_Mesh.vertices[2]);
			*/
            //var image = new Texture2D((int)(bottomRight.x-topLeft.x),(int)(bottomRight.y-topRight.y), TextureFormat.ARGB32, false);

            // TODO: combine layers to single texture
            image.LoadRawTextureData(pixels);
            image.Apply(false);
            return image;
        }


        // returns screenshot as Texture2D
        public Texture2D GetScreenshot()
        {
            HideUI();

            cam.Render();
            Mesh go_Mesh = GetComponent<MeshFilter>().mesh;
            var topLeft = cam.WorldToScreenPoint(go_Mesh.vertices[0]);
            var topRight = cam.WorldToScreenPoint(go_Mesh.vertices[3]);
            var bottomRight = cam.WorldToScreenPoint(go_Mesh.vertices[2]);
            var image = new Texture2D((int)(bottomRight.x - topLeft.x), (int)(bottomRight.y - topRight.y), TextureFormat.ARGB32, false);
            image.ReadPixels(new Rect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y), 0, 0);
            image.Apply(false);

            ShowUI();
            return image;
        }

        public int SnapToGrid(int val)
        {
            return (int)(val - val % gridSize);
        }


        // converts pixel coordinate to world position
        public Vector3 PixelToWorld(int x, int y)
        {
            Vector3 pixelPos = new Vector3(x, y, 0); // x,y = texture pixel pos

            float planeWidth = myRenderer.bounds.size.x;
            float planeHeight = myRenderer.bounds.size.y;

            float localX = ((pixelPos.x / texWidth) - 0.5f) * planeWidth;
            float localY = ((pixelPos.y / texHeight) - 0.5f) * planeHeight;

            //			return transform.TransformPoint(new Vector3(localX,localY, 0));
            return new Vector3(localX, localY, 0);
        }

        // call this to change color, so that other objects get the new color also
        public void SetPaintColor(Color32 newColor)
        {
            paintColor = newColor;

            SetBrushAlphaStrength(brushAlphaStrength);
            alphaLerpVal = paintColor.a / brushAlphaStrengthVal; // precalc
            UpdateLineModePreviewObjects();
        }

        // set alpha power, good values are usually between 0.1 to 0.001
        public void SetBrushAlphaStrength(float val)
        {
            brushAlphaStrengthVal = 255f / val;
        }

        void UpdateLineModePreviewObjects()
        {
            if (lineRenderer)
            {
                lineRenderer.SetColors(paintColor, paintColor);
                lineRenderer.SetWidth(brushSize*2f / resolutionScaler, brushSize*2f / resolutionScaler);
            }

            if (previewLineCircleEnd)
            {
                previewLineCircleEnd.GetComponent<SpriteRenderer>().color = paintColor;
                previewLineCircleEnd.transform.localScale = Vector3.one * brushSize * 0.8f / resolutionScaler;
            }

            if (previewLineCircleStart)
            {
                previewLineCircleStart.GetComponent<SpriteRenderer>().color = paintColor;
                previewLineCircleStart.transform.localScale = Vector3.one * brushSize * 0.8f / resolutionScaler;
            }
        }

        byte ByteLerp(byte value1, byte value2, float amount)
        {
            return (byte)(value1 + (value2 - value1) * amount);
        }


        // assigns new mask layer image
        public void SetMaskImage(Texture2D newTexture)
        {
            // Check if we have correct material to use mask image (layer)
            if (myRenderer.material.name.StartsWith("CanvasWithAlpha") || GetComponent<Renderer>().material.name.StartsWith("CanvasDefault"))
            {
                // FIXME: this is bit annoying to compare material names..
                Debug.LogWarning("CanvasWithAlpha and CanvasDefault materials do not support using MaskImage (layer). Disabling 'useMaskImage'");
                Debug.LogWarning("CanvasWithAlpha and CanvasDefault materials do not support using MaskImage (layer). Disabling 'useMaskLayerOnly'");
                useMaskLayerOnly = false;
                useMaskImage = false;
                maskTex = null;

            }
            else { //material is ok

                // NOTE: if new texture is different size, problems will occur when drawing (mask is not aligned)
                //if (texWidth!=maskTex.width || texHeight != maskTex.height) Debug.LogWarning("SetMaskImage: New mask texture size is different from existing canvas texture, could cause problems. Current resolution:"+texWidth+"x"+texHeight+" | Mask resolution:"+maskTex.width+"x"+maskTex.height);

                maskTex = newTexture;
                texWidth = newTexture.width;
                texHeight = newTexture.height;
                myRenderer.material.SetTexture("_MaskTex", newTexture);
                ReadMaskImage();
                textureNeedsUpdate = true;
            }
        } // SetMaskImage


        // assigns new canvas image
        public void SetCanvasImage(Texture2D newTexture)
        {
            // NOTE: if new texture is different size, problems will occur when drawing
            myRenderer.material.SetTexture(targetTexture, newTexture);
            InitializeEverything();
        }

        public void SetPanZoomMode(bool state)
        {
            isZoomingOrPanning = state;
            this.enabled = isZoomingOrPanning ? false : true; // Disable Update() loop from this script, if zooming or panning
        }


        // cleaning up buffers - https://github.com/unitycoder/UnityMobilePaint/issues/10
        void OnDestroy()
        {
            if (drawingTexture != null) Texture2D.DestroyImmediate(drawingTexture, true);
            pixels = null;
            maskPixels = null;
            clearPixels = null;
            lockMaskPixels = null;
            if (undoEnabled) undoPixels.Clear();

            // System.GC.Collect();
        }


    } // class
} // namespace
