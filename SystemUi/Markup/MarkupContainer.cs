// ----------------------------------------------------------------------------
// The MIT License
// LeopotamGroupLibrary https://github.com/Leopotam/LeopotamGroupLibraryUnity
// Copyright (c) 2012-2017 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using LeopotamGroup.Collections;
using LeopotamGroup.Common;
using LeopotamGroup.Math;
using LeopotamGroup.Serialization;
using LeopotamGroup.SystemUi.Atlases;
using LeopotamGroup.SystemUi.Markup.Generators;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LeopotamGroup.SystemUi.Markup {
    /// <summary>
    /// Ui markup container. Supports spawning of named xml-schema from Resources folder.
    /// </summary>
    public class MarkupContainer : MonoBehaviour {
        public static readonly int HashedUi = "ui".GetStableHashCode ();

        public static readonly int HashedBox = "box".GetStableHashCode ();

        public static readonly int HashedAlign = "align".GetStableHashCode ();

        public static readonly int HashedImage = "image".GetStableHashCode ();

        public static readonly int HashedText = "text".GetStableHashCode ();

        public static readonly int HashedGrid = "grid".GetStableHashCode ();

        public static readonly int HashedName = "name".GetStableHashCode ();

        [SerializeField]
        string _markupPath = "UnknownMarkup";

        [SerializeField]
        List<SpriteAtlas> _atlases = new List<SpriteAtlas> ();

        [SerializeField]
        List<Font> _fonts = new List<Font> ();

        XmlNode _xmlTree;

        Canvas _canvas;

        Dictionary<int, Func<XmlNode, MarkupContainer, GameObject>> _generators = new Dictionary<int, Func<XmlNode, MarkupContainer, GameObject>> (64);

        Dictionary<int, GameObject> _namedNodes = new Dictionary<int, GameObject> (128);

        int _uiLayer;

        bool _isLoaded;

        Font _defaultFont;

        void Awake () {
            _uiLayer = LayerMask.NameToLayer ("UI");
            AttachGenerators ();
        }

        protected virtual void AttachGenerators () {
            _generators.Add (HashedUi, UiNode.Create);
            _generators.Add (HashedBox, BoxNode.Create);
            _generators.Add (HashedAlign, AlignNode.Create);
            _generators.Add (HashedImage, ImageNode.Create);
            _generators.Add (HashedText, TextNode.Create);
            _generators.Add (HashedGrid, GridNode.Create);
        }

        void Load () {
            if (!_isLoaded) {
                _isLoaded = true;
                _xmlTree = LoadXml (_markupPath);
            }
        }

        static XmlNode LoadXml (string markupPath) {
            var asset = Resources.Load<TextAsset> (markupPath);
            if (asset == null) {
                Debug.LogWarning ("Cant load markup " + markupPath);
                return null;
            }
            XmlNode xmlTree = null;
            try {
                xmlTree = Singleton.Get<XmlSerialization> ().Deserialize (asset.text, true);
            } catch (Exception ex) {
                Debug.LogWarning (ex);
            }
            Resources.UnloadAsset (asset);
            return xmlTree;
        }

        void LateUpdate () {
            if (!_isLoaded) {
                CreateVisuals ();
            }
        }

        void Clear () {
            _canvas = null;
            _namedNodes.Clear ();
            var tr = transform;
            for (int i = tr.childCount - 1; i >= 0; i--) {
                DestroyImmediate (tr.GetChild (i));
            }
        }

        void CreateVisualNode (XmlNode xmlTree, Transform root) {
            if (xmlTree == null) {
                return;
            }
            Func<XmlNode, MarkupContainer, GameObject> generator;
            if (!_generators.TryGetValue (xmlTree.NameHash, out generator)) {
                generator = BoxNode.Create;
            }
            var go = generator (xmlTree, this);
            var tr = go.transform;
            go.layer = _uiLayer;
            tr.SetParent (root, false);

            if ((object) _canvas == null) {
                _canvas = go.GetComponentInChildren<Canvas> ();
            }

            var nodeName = xmlTree.GetAttribute (HashedName);
            if (!string.IsNullOrEmpty (nodeName)) {
                var nodeNameHash = nodeName.GetStableHashCode ();
                if (_namedNodes.ContainsKey (nodeNameHash)) {
#if UNITY_EDITOR
                    Debug.LogWarning ("Duplicate name: " + nodeName);
#endif
                } else {
                    _namedNodes[nodeNameHash] = go;
                }
            }

            var children = xmlTree.Children;
            for (int i = 0, iMax = children.Count; i < iMax; i++) {
                CreateVisualNode (children[i], tr);
            }
        }

        /// <summary>
        /// Get root canvas of this infrastructure.
        /// </summary>
        public Canvas GetCanvas () {
            return _canvas;
        }

        /// <summary>
        /// Force cleanup / create widgets infrastructure from attached xml-schema.
        /// </summary>
        public void CreateVisuals () {
            if ((object) _defaultFont == null) {
                _defaultFont = _fonts.Count > 0 ? _fonts[0] : Resources.GetBuiltinResource<Font> ("Arial.ttf");
            }
            Load ();
            Clear ();
            CreateVisualNode (_xmlTree, transform);
        }

        /// <summary>
        /// Attach sprite atlas. Should be called before any visuals with content from this atlas will be created.
        /// </summary>
        /// <param name="font">Sprite atlas.</param>
        public void AttachAtlas (SpriteAtlas atlas) {
            if ((object) atlas != null && !_atlases.Contains (atlas)) {
                _atlases.Add (atlas);
            }
        }

        /// <summary>
        /// Attach font. Should be called before any visuals with content from this font will be created.
        /// </summary>
        /// <param name="font">Font.</param>
        public void AttachFont (Font font, bool asDefault = false) {
            if ((object) font != null && !_fonts.Contains (font)) {
                _fonts.Add (font);
                if (asDefault) {
                    _defaultFont = font;
                }
            }
        }

        /// <summary>
        /// Get sprite from attached atlas or null.
        /// </summary>
        /// <param name="spriteName">Name of atlas-sprite pair. Should be in format "atlasName;spriteName".</param>
        public Sprite GetAtlasSprite (string spriteName) {
            if (string.IsNullOrEmpty (spriteName)) {
                return null;
            }
            var parts = spriteName.Split (';');
            if (parts.Length != 2) {
                Debug.LogWarning ("Invalid sprite name: " + spriteName);
                return null;
            }
            var atlasName = parts[0];
            for (var i = _atlases.Count - 1; i >= 0; i--) {
                if (string.CompareOrdinal (_atlases[i].GetName (), atlasName) == 0) {
                    return _atlases[i].Get (parts[1]);
                }
            }
            return null;
        }

        public Font GetFont (string fontName) {
            for (var i = _fonts.Count - 1; i >= 0; i--) {
                if (string.CompareOrdinal (_fonts[i].name, fontName) == 0) {
                    return _fonts[i];
                }
            }
            return _defaultFont;
        }

        /// <summary>
        /// Get GameObject of specific node from markup or null.
        /// </summary>
        /// <param name="name">Unique name of node.</param>
        public GameObject GetNamedNode (string name) {
            var hash = name.GetStableHashCode ();
            GameObject go;
            if (_namedNodes.TryGetValue (hash, out go)) {
                return go;
            }
            return null;
        }

        /// <summary>
        /// Create new markup infrastructure from code.
        /// </summary>
        /// <param name="markupPath">Path to xml-schema from Resources folder.</param>
        /// <param name="parent">Root transform for infrastructure.</param>
        public static MarkupContainer CreateMarkup (string markupPath, Transform parent = null) {
            if (string.IsNullOrEmpty (markupPath)) {
                return null;
            }
            var container =
                new GameObject (
#if UNITY_EDITOR
                    "_MARKUP_" + markupPath
#endif
                ).AddComponent<MarkupContainer> ();
            container._markupPath = markupPath;
            if ((object) parent != null) {
                container.transform.SetParent (parent, false);
            }
            return container;
        }
    }
}