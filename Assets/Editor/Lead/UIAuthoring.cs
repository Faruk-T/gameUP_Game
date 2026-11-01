using System;
using GameUp.App;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GameUp.Editor
{
    // Authoring only: every control and persistent listener is serialized into a scene or prefab.
    internal static class UIAuthoring
    {
        internal static readonly Color Ink = new Color(.06f, .17f, .19f);
        internal static readonly Color Paper = new Color(.96f, .93f, .85f);
        internal static readonly Color Accent = new Color(.15f, .63f, .57f);
        internal static Font Body => AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/PatrickHand-Regular.ttf");
        internal static Font Title => Body;

        internal static void Set(UnityEngine.Object owner, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(owner); var property = so.FindProperty(field);
            if (property == null) throw new InvalidOperationException(owner.GetType().Name + "." + field);
            property.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }
        internal static void Number(UnityEngine.Object owner, string field, float value)
        {
            var so = new SerializedObject(owner); so.FindProperty(field).floatValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }
        internal static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(.5f,.5f); rt.pivot = new Vector2(.5f,.5f);
            rt.anchoredPosition = new Vector2(x,y); rt.sizeDelta = new Vector2(width,height); return rt;
        }
        internal static void Stretch(RectTransform rt)
        { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
        internal static Canvas Canvas(string name, Transform parent = null, int order = 0)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent) go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f; return canvas;
        }
        internal static Image Image(string name, Transform parent, float x, float y, float w, float h, Color color, Sprite sprite = null)
        {
            var image = Rect(name,parent,x,y,w,h).gameObject.AddComponent<Image>(); image.color=color; image.sprite=sprite;
            image.raycastTarget=false; return image;
        }
        internal static GameObject Overlay(string name, Transform parent, Color color)
        {
            var image = Image(name,parent,0,0,1920,1080,color); Stretch(image.rectTransform); image.raycastTarget=true; return image.gameObject;
        }
        internal static Text Text(string name, Transform parent, string value, float x, float y, float w, float h,
            int size=28, Color? color=null, TextAnchor alignment=TextAnchor.UpperLeft, bool title=false)
        {
            var text=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Text>(); text.font=title?Title:Body;
            text.text=value; text.fontSize=size; text.color=color??Ink; text.alignment=alignment; text.raycastTarget=false;
            text.horizontalOverflow=HorizontalWrapMode.Wrap; text.verticalOverflow=VerticalWrapMode.Truncate; text.supportRichText=true;
            return text;
        }
        internal static Button Button(string name, Transform parent, string label, float x, float y, float w, UnityAction action)
        {
            var image=Image(name,parent,x,y,w,66,Paper,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Menu/SharedButtonLight.png"));
            image.raycastTarget=true; var button=image.gameObject.AddComponent<Button>(); button.targetGraphic=image;
            var colors=button.colors; colors.normalColor=Color.white; colors.highlightedColor=new Color(.7f,.95f,.87f);
            colors.selectedColor=colors.highlightedColor; colors.pressedColor=Accent; colors.disabledColor=new Color(.5f,.5f,.5f); button.colors=colors;
            Text("Label",image.transform,label,0,0,w-36,58,28,Ink,TextAnchor.MiddleCenter);
            if (action!=null) UnityEventTools.AddPersistentListener(button.onClick,action); return button;
        }
        internal static ScrollRect Scroll(string name, Transform parent, float x, float y, float w, float h, out RectTransform content)
        {
            var root=Rect(name,parent,x,y,w,h); var scroll=root.gameObject.AddComponent<ScrollRect>(); scroll.horizontal=false; scroll.movementType=ScrollRect.MovementType.Clamped;
            var viewport=Rect("Viewport",root,0,0,w,h); Stretch(viewport); viewport.gameObject.AddComponent<RectMask2D>();
            var hit=viewport.gameObject.AddComponent<Image>(); hit.color=Color.clear;
            content=Rect("Content",viewport,0,0,w-24,0); content.anchorMin=new Vector2(0,1); content.anchorMax=new Vector2(1,1);
            content.pivot=new Vector2(.5f,1); content.sizeDelta=new Vector2(-24,0); content.anchoredPosition=Vector2.zero;
            var layout=content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing=20; layout.childControlHeight=true;
            layout.childControlWidth=true; layout.childForceExpandHeight=false;
            var fit=content.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport=viewport; scroll.content=content; scroll.scrollSensitivity=36; return scroll;
        }
        internal static SettingsPanel Settings(Transform parent) => DocumentMenuAuthoring.CreateSettings(parent);
    }
}
