﻿#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using MUtility;
using Object = UnityEngine.Object;

namespace LooseServices
{
class LooseServiceLoadedColumn: Column<FoldableRow<LooseServiceRow>>
{
    LooseServiceWindow _window;
    public LooseServiceLoadedColumn(LooseServiceWindow window)
    {
        columnInfo = new ColumnInfo
        {
            customHeaderDrawer = DrawHeader,
            relativeWidthWeight = 0.5f,
            fixWidth = 100,
        };
        _window = window;
    }

    GUIStyle _rowButtonStyle;

    public override void DrawCell(Rect position, FoldableRow<LooseServiceRow> row, GUIStyle style, Action onChanged)
    {
        if (row.element.Category == LooseServiceRow.RowCategory.Installer) return;

        if (IsRowHighlighted(row))
            EditorGUI.DrawRect(position, EditorHelper.tableSelectedColor);

        bool isRowSelectable = row.element.loadedInstance != null;
        if (isRowSelectable && position.Contains(Event.current.mousePosition))
            EditorGUI.DrawRect(position, EditorHelper.tableHoverColor);

        Object loadedObject = row.element.loadedInstance;

        var contentPos = new Rect(
            position.x + 1,
            position.y + (position.height - 16) / 2f,
            position.width - 2,
            height: 16);

        ServiceSource source = row.element.source;

        // Loaded Element
        if (loadedObject != null)
        { 
            const float unloadButtonWidth = 23;
            contentPos.width -= unloadButtonWidth;
            if (row.element.Category == LooseServiceRow.RowCategory.Source)
            {
                var loadedContent = new GUIContent(loadedObject.name, LoadedObjectIcon(loadedObject.GetType()));
                GUI.Label(contentPos, loadedContent, LabelStyle);
            }
            else
            {
                contentPos.height = 8;
                contentPos.y += 4;
                var loadedContent = new GUIContent(
                    text: null,
                    EditorGUIUtility.IconContent("d_FilterSelectedOnly").image,
                    "Cached");
            GUI.Label(contentPos, loadedContent, LabelStyle);
            }

 
            // Ping The Object
            if (loadedObject == null) return;
            _rowButtonStyle = _rowButtonStyle ?? new GUIStyle(GUI.skin.label);
            if (GUI.Button(contentPos, GUIContent.none, _rowButtonStyle))
                OnRowClick(row);

            // Load / Unload Button
            if (row.element.Category == LooseServiceRow.RowCategory.Source)
            {
                var unloadButtonRect = new Rect(
                    contentPos.xMax,
                    contentPos.y,
                    unloadButtonWidth,
                    contentPos.height);
                GUIContent unloadContent =
                    EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "d_winbtn_win_close" : "winbtn_win_close");
                if (GUI.Button(unloadButtonRect, unloadContent))
                    source.ClearInstances();
            }

            
        }
        else if (row.element.Category == LooseServiceRow.RowCategory.Source)
        {
            const float loadButtonWidth = 75;
            var actionButtonRect = new Rect(
                contentPos.x + ((contentPos.width - loadButtonWidth) / 2),
                contentPos.y,
                loadButtonWidth,
                contentPos.height);

            bool isLoadable = source != null && source.GetLoadability == Loadability.Loadable;
            if (!isLoadable)
            {
                // Loadability Error
                GUIContent loadabilityError = row.element.loadability.GetGuiContent(source.NotInstantiatableReason);
                GUI.Label(actionButtonRect, loadabilityError, CategoryStyle);
            }
            else
            {
                // Load Button 
                if (GUI.Button(actionButtonRect, "Load", ButtonStyle))
                    foreach (Type type in row.element.source.AllAbstractTypes)
                        row.element.source.TryGetService(
                            type, 
                            row.element.installer,
                            conditionTags: null, 
                            out object _,
                            out bool _);
            }

            GUI.enabled = true;
        }
    }


    static Texture LoadedObjectIcon(Type type)
    {
        if (type.IsSubclassOf(typeof(ScriptableObject)))
            return EditorGUIUtility.IconContent("ScriptableObject Icon").image;
        return EditorGUIUtility.IconContent("GameObject Icon").image;
    }


    protected override GUIStyle GetDefaultStyle() => null;


    static void OnRowClick(FoldableRow<LooseServiceRow> row)
    { 
        Object obj = row.element.loadedInstance;
        if (Selection.objects.Length == 1 && Selection.objects[0] == obj)
            Selection.objects = new Object[]{};
        else
            Selection.objects = new[] {obj};
    }

    static bool IsRowHighlighted(FoldableRow<LooseServiceRow> row) => 
        row.element.loadedInstance!=null && Selection.objects.Contains(row.element.loadedInstance);
    
    
    static GUIStyle _categoryStyle;
    static GUIStyle CategoryStyle => _categoryStyle = _categoryStyle ?? new GUIStyle
    {
        alignment = TextAnchor.MiddleLeft,
        fontSize = 10,
        padding = new RectOffset(left: 2, right: 2,top: 0,bottom: 0),
        normal = {textColor = GUI.skin.label.normal.textColor},
    };
    
    static GUIStyle _labelStyle;
    static GUIStyle LabelStyle => _labelStyle = _labelStyle ?? new GUIStyle
    {
        alignment = TextAnchor.MiddleCenter, 
        normal = {textColor = GUI.skin.label.normal.textColor},
    };
    
        
    static GUIStyle _headerLabelStyle;
    static GUIStyle HeaderLabelStyle => _headerLabelStyle = _headerLabelStyle ?? new GUIStyle
    {
        alignment = TextAnchor.MiddleLeft
    };
        
    static GUIStyle _buttonStyle;
    static GUIStyle ButtonStyle => _buttonStyle = _buttonStyle ?? new GUIStyle(GUI.skin.button)
    {  
        fontSize = 10
    };
    
    void DrawHeader(Rect position)
    {
        const float buttonW = 50;
        const float indent = 4;
        const float margin = 2;
        position.x += indent;
        position.width -= indent;
        GUI.Label(position, "Loaded", HeaderLabelStyle);

        var buttonRect = new Rect(
            position.xMax - buttonW - margin,
            position.y + margin,
            buttonW,
            position.height - 2 * margin);
        if (GUI.Button(buttonRect, "Clear"))
            Services.ClearAllCachedData();
    }
}
}
#endif