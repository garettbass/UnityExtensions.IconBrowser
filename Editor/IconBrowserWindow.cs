using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityExtensions
{

    public class IconBrowserWindow : EditorWindow
    {

        private struct Entry {
            public GUIContent icon;
            public GUIContent name;
            public float rowHeight;
        }

        private const float RowSpacing = 8;

        private const float MinRowHeight = 16;

        private Entry[] m_entries;

        private Entry[] m_searchResults;

        private float m_maxImageWidth;

        private SearchField m_searchField;

        private string m_searchString;

        private Vector2 m_scrollOffset;

        [MenuItem("Window/Icon Browser", false, 9999)]
        public static IconBrowserWindow GetWindow()
        {
            var window = GetWindow<IconBrowserWindow>();
            window.titleContent.text = "Icon Browser";
            window.Show();
            return window;
        }

        private void OnEnable()
        {
            m_searchField = new SearchField();
            m_searchString = string.Empty;
            LoadEntries();
        }

        private void OnGUI()
        {
            SearchToolbarGUI();
            IconScrollViewGUI();
        }

        private void LoadEntries()
        {
            var entries = new List<Entry>();
            var names = new HashSet<string>();

            var textures = Resources.FindObjectsOfTypeAll<Texture2D>();
            foreach (var texture in textures.OrderBy(t => t.name.ToLower()))
            {
                var name = texture.name;
                if (string.IsNullOrEmpty(name))
                    continue;

                if (EditorUtility.IsPersistent(texture) == false)
                    continue;

                Debug.unityLogger.logEnabled = false;
                var icon = EditorGUIUtility.IconContent(name);
                Debug.unityLogger.logEnabled = true;

                if (icon == null)
                    continue;

                var image = icon.image;
                if (image == null)
                    continue;

                if (image.width == 0)
                    continue;

                if (image.height == 0)
                    continue;

                if (names.Add(name) == false)
                    continue;

                entries.Add(new Entry() {
                    icon = icon,
                    name = new GUIContent(name),
                    rowHeight = Mathf.Max(icon.image.height, MinRowHeight),
                });

                m_maxImageWidth = Mathf.Max(m_maxImageWidth, image.width);
            }
            m_entries = entries.ToArray();
            UpdateSearchResults();
        }

        private void SearchToolbarGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var searchRect =
                GUILayoutUtility
                .GetRect(0, 16, GUILayout.ExpandWidth(true));
            searchRect.yMin += 2;
            var oldSearchString = m_searchString;
            var newSearchString = 
                m_searchField.OnGUI(
                    rect: searchRect,
                    text: oldSearchString,
                    style: "ToolbarSeachTextField",
                    cancelButtonStyle: "ToolbarSeachCancelButton",
                    emptyCancelButtonStyle: "ToolbarSeachCancelButtonEmpty")
                ?? "";
            EditorGUILayout.EndHorizontal();

            if (newSearchString != oldSearchString)
            {
                m_searchString = newSearchString;
                UpdateSearchResults();
            }
        }

        private void IconScrollViewGUI()
        {
            m_scrollOffset = EditorGUILayout.BeginScrollView(m_scrollOffset);

            var entries = m_searchResults;

            var viewHeight =
                m_searchResults.Sum(r => r.rowHeight) +
                m_searchResults.Length * RowSpacing;

            var viewRect =
                GUILayoutUtility
                .GetRect(0, viewHeight, GUILayout.ExpandWidth(true));

            var oddRowColor = new Color(0, 0, 0, 0.05f);

            var isRepaint = Event.current.type == EventType.Repaint;
            if (isRepaint)
            {
                var nameStyle = new GUIStyle(EditorStyles.label);
                nameStyle.alignment = TextAnchor.MiddleLeft;

                var rowRect = viewRect;

                for (int i = 0, n = entries.Length; i < n; ++i)
                {
                    if (i > 0) rowRect.y += RowSpacing;

                    var entry = entries[i];
                    var icon = entry.icon;
                    var name = entry.name;
                    var rowHeight = entry.rowHeight;

                    var image = icon.image;
                    var imageWidth = image.width;
                    var imageHeight = image.height;
                    rowRect.height = rowHeight;

                    if (i % 2 == 1)
                    {
                        var fillRect = rowRect;
                        fillRect.yMin -= RowSpacing / 2;
                        fillRect.yMax += RowSpacing / 2;
                        EditorGUI.DrawRect(fillRect, oddRowColor);
                    }

                    var imageRect = rowRect;
                    imageRect.x += (m_maxImageWidth - imageWidth) / 2;
                    imageRect.y += (rowHeight - imageHeight) / 2;
                    imageRect.width = imageWidth;
                    imageRect.height = imageHeight;

                    GUI.DrawTexture(imageRect, image);

                    var nameRect = rowRect;
                    nameRect.xMin += m_maxImageWidth;
                    nameStyle.Draw(nameRect, name, false, false, false, false);

                    rowRect.y += rowHeight;
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void UpdateSearchResults()
        {
            var ignoreCase = StringComparison.OrdinalIgnoreCase;
            m_searchResults =
                m_entries
                .Where(entry =>
                    entry.name.text.IndexOf(m_searchString, ignoreCase) >= 0)
                .ToArray();
        }

    } // class IconBrowserWindow

} // namespace UnityExtensions