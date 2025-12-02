using RaidPopup.Models;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace RaidPopup.UI
{
    /// <summary>
    /// Compact raid notification panel positioned near Fika's Online Players panel
    /// </summary>
    public class RaidNotificationPanel : MonoBehaviour
    {
        private GameObject _canvasObj;
        private GameObject _contentContainer;
        private List<GameObject> _raidEntries = new List<GameObject>();
        private bool _initialized = false;

        // Styling to match Fika's dark UI
        private readonly Color _panelBgColor = new Color(0f, 0f, 0f, 0.75f);
        private readonly Color _accentColor = new Color(0.1f, 0.6f, 0.95f, 1f);
        private readonly Color _accentHoverColor = new Color(0.2f, 0.7f, 1.0f, 1f);
        private readonly Color _mapColor = new Color(0.85f, 0.85f, 0.9f, 1f);
        private readonly Color _subtextColor = new Color(0.55f, 0.55f, 0.6f, 1f);
        private readonly Color _hoverColor = new Color(0.08f, 0.08f, 0.1f, 0.85f);

        private void Start()
        {
            StartCoroutine(DelayedInit());
        }

        private System.Collections.IEnumerator DelayedInit()
        {
            yield return new WaitForSeconds(0.3f);
            
            try
            {
                CreatePanel();
                _initialized = true;
                RefreshDisplay();
                RaidPopupPlugin.Log?.LogInfo("RaidPopup: Panel initialized");
            }
            catch (System.Exception ex)
            {
                RaidPopupPlugin.Log?.LogError($"RaidPopup init error: {ex.Message}");
            }
        }

        private void CreatePanel()
        {
            _canvasObj = new GameObject("RaidPopupCanvas");
            _canvasObj.transform.SetParent(transform, false);
            
            var canvas = _canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            
            var scaler = _canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            _canvasObj.AddComponent<GraphicRaycaster>();

            _contentContainer = new GameObject("Content");
            _contentContainer.transform.SetParent(_canvasObj.transform, false);

            var contentRect = _contentContainer.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(1, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(1, 1);
            contentRect.anchoredPosition = new Vector2(-320, -70);
            contentRect.sizeDelta = new Vector2(260, 0);

            var vertLayout = _contentContainer.AddComponent<VerticalLayoutGroup>();
            vertLayout.spacing = 6;
            vertLayout.childAlignment = TextAnchor.UpperRight;
            vertLayout.childControlWidth = true;
            vertLayout.childControlHeight = true;
            vertLayout.childForceExpandWidth = true;
            vertLayout.childForceExpandHeight = false;

            var sizeFitter = _contentContainer.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        public void RefreshDisplay()
        {
            if (!_initialized || _contentContainer == null) return;

            try
            {
                foreach (var entry in _raidEntries)
                {
                    if (entry != null) Destroy(entry);
                }
                _raidEntries.Clear();

                var raids = RaidPopupPlugin.Instance?.ActiveRaids;
                if (raids == null || raids.Count == 0)
                {
                    return;
                }

                foreach (var raid in raids)
                {
                    CreateRaidEntry(raid);
                }
            }
            catch (System.Exception ex)
            {
                RaidPopupPlugin.Log?.LogError($"RefreshDisplay error: {ex.Message}");
            }
        }

        private void CreateRaidEntry(ActiveRaid raid)
        {
            var entryObj = new GameObject($"RaidEntry_{raid.Id}");
            entryObj.transform.SetParent(_contentContainer.transform, false);
            _raidEntries.Add(entryObj);

            var bgImage = entryObj.AddComponent<Image>();
            bgImage.color = _panelBgColor;
            bgImage.raycastTarget = true;

            var entryLayout = entryObj.AddComponent<LayoutElement>();
            entryLayout.preferredHeight = 70;
            entryLayout.preferredWidth = 260;

            // Left accent bar
            var accentBar = CreateChild(entryObj.transform, "Accent");
            var accentRect = accentBar.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0, 0);
            accentRect.anchorMax = new Vector2(0, 1);
            accentRect.pivot = new Vector2(0, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(3, 0);
            var accentImage = accentBar.AddComponent<Image>();
            accentImage.color = _accentColor;
            accentImage.raycastTarget = false;

            // Click handler
            string raidId = raid.Id;
            var clickHandler = entryObj.AddComponent<RaidClickHandler>();
            clickHandler.Setup(raidId, bgImage, _panelBgColor, _hoverColor, accentImage, _accentColor, _accentHoverColor);

            // Text container
            var textContainer = CreateChild(entryObj.transform, "Text");
            var textRect = textContainer.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12, 6);
            textRect.offsetMax = new Vector2(-8, -6);

            var textLayout = textContainer.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 2;
            textLayout.childAlignment = TextAnchor.MiddleLeft;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandHeight = false;

            // Map name
            CreateTMPLabel(
                textContainer.transform, 
                raid.GetDisplayLocation().ToUpper(), 
                15,
                FontStyles.Bold, 
                _mapColor,
                22
            );

            // Host and Time
            string info = $"Host: {raid.Nickname} • Time: {raid.GetFormattedTime()}";
            CreateTMPLabel(
                textContainer.transform, 
                info, 
                12, 
                FontStyles.Normal, 
                _subtextColor, 
                16
            );
        }

        private GameObject CreateChild(Transform parent, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private void CreateTMPLabel(Transform parent, string text, int fontSize, FontStyles style, Color color, float height)
        {
            var obj = new GameObject("Label");
            obj.transform.SetParent(parent, false);

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.raycastTarget = false;

            var layout = obj.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
        }

        private void OnDestroy()
        {
            RaidPopupPlugin.Log?.LogInfo("RaidPopup: Panel destroyed");
        }
    }

    /// <summary>
    /// Click handler for raid entries
    /// </summary>
    public class RaidClickHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private string _raidId;
        private Image _bgImage;
        private Image _accentImage;
        private Color _normalColor;
        private Color _hoverColor;
        private Color _normalAccentColor;
        private Color _hoverAccentColor;

        public void Setup(string raidId, Image bgImage, Color normalColor, Color hoverColor, Image accentImage, Color normalAccentColor, Color hoverAccentColor)
        {
            _raidId = raidId;
            _bgImage = bgImage;
            _normalColor = normalColor;
            _hoverColor = hoverColor;
            _accentImage = accentImage;
            _normalAccentColor = normalAccentColor;
            _hoverAccentColor = hoverAccentColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            RaidPopupPlugin.Instance?.DismissRaid(_raidId);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_bgImage != null) _bgImage.color = _hoverColor;
            if (_accentImage != null) _accentImage.color = _hoverAccentColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_bgImage != null) _bgImage.color = _normalColor;
            if (_accentImage != null) _accentImage.color = _normalAccentColor;
        }
    }
}
