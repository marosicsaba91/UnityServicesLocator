﻿#if UNITY_EDITOR

using System.Linq;
using MUtility;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
class ServiceTypesColumn : Column<FoldableRow<ServiceLocatorRow>>
{

    readonly ServiceLocatorWindow _serviceLocatorWindow;
 
    string SearchTypeText
    {
        get => _serviceLocatorWindow.searchTypeText;
        set => _serviceLocatorWindow.searchTypeText = value;
    }

    string[] _typeSearchWords = null;
    public bool NoSearch => string.IsNullOrEmpty(SearchTypeText); 

    public ServiceTypesColumn(ServiceLocatorWindow serviceLocatorWindow)
    {
        columnInfo = new ColumnInfo
        {
            relativeWidthWeight = 0.5f,
            fixWidth = 75,
            customHeaderDrawer = DrawHeader
        };
        _serviceLocatorWindow = serviceLocatorWindow;
    }
    
    public override void DrawCell(Rect position, FoldableRow<ServiceLocatorRow> row, GUIStyle style, Action onChanged)
    {
        if (row.element.Category != ServiceLocatorRow.RowCategory.Source) return;
        
        List<Type> types = row.element.source.GetServiceTypes().ToList();
        
        if (types.IsNullOrEmpty())
        {
            GUI.Label(position, "-");
            return;
        }
 
        DrawTypes(position, types);
    }


    void DrawTypes(Rect position, IReadOnlyList<Type> types)
    { 
        const float popupWidth = 25;
        if (types.Count <= 0) return;
        
        bool morThanOneType = types.Count > 1;
        if (morThanOneType)
        {
            DrawTypePopup(
                new Rect(position.xMax - popupWidth, position.y, popupWidth, position.height),
                types);
            position.width -= popupWidth + 1;
        }

        DrawType(position, types[0]); 
    }

    public static void DrawType(Rect position, Type type)
    {
        GUIContent content = FileIconHelper.GetGUIContentToType(type);
        if (GUI.Button(position, content, new GUIStyle("Label")))
            TryPing(type); 
    }

    void DrawTypePopup(Rect position, IReadOnlyList<Type> typesToDrawInPopup)
    {
        position.y += 1;
        position.height -= 2;
        position.x -= 1;
        var contents = new string[typesToDrawInPopup.Count];
        for (var i = 0; i < typesToDrawInPopup.Count; i++)
            contents[i] = FileIconHelper.GetGUIContentToType(typesToDrawInPopup[i]).tooltip;

        int index = EditorGUI.Popup(
            position,
            selectedIndex: -1,
            contents,
            new GUIStyle(GUI.skin.button));

        GUI.Label(position, $"+{typesToDrawInPopup.Count}", ServicesEditorHelper.SmallLabelStyle);

        if (index >= -0)
            TryPing(typesToDrawInPopup[index]);
    } 
         

    public static void TryPing(Type pingable)
    {
        Object obj = TypeToFileHelper.GetObject(pingable);
        if(obj!= null)
            EditorGUIUtility.PingObject(obj);
    } 

    void DrawHeader(Rect pos)
    {
        const float labelWidth = 45; 
        var labelPos = new Rect(
            pos.x + 2,
            pos.y + 2,
            labelWidth,
            pos.height - 4);
        
        GUI.Label(labelPos, "Types");
        
        bool modernUI = EditorHelper.IsModernEditorUI; 

        float searchTextW = Mathf.Min(200f, pos.width - 52f);
        var searchTypePos = new Rect(
            pos.xMax - searchTextW - 2,
            pos.y + 3 + (modernUI ? 0 : 1),
            searchTextW,
            pos.height - 5);
        SearchTypeText = EditorGUI.TextField(searchTypePos, SearchTypeText, GUI.skin.FindStyle("ToolbarSeachTextField"));

        _typeSearchWords = ServicesEditorHelper.GenerateSearchWords(SearchTypeText);
    }
    public bool ApplyTypeSearchOnSource(IServiceSourceSet set, ServiceSource source) =>
        ApplyTypeSearchOnTypeArray(source.GetServiceTypes());

    public bool ApplyTypeSearchOnTypeArray(IEnumerable<Type> typesOnService)
    {
        if (string.IsNullOrEmpty(SearchTypeText)) return true;
        if (_typeSearchWords == null) return true;
        if (typesOnService == null) return false;

        Type[] types = typesOnService.ToArray();
        string[] typeTexts = types.Select(type => type.FullName.ToLower()).ToArray();

        foreach (string searchWord in _typeSearchWords)
            if (!typeTexts.Any(typeName => typeName.Contains(searchWord)))
                return false;

        return true; 
    }

    protected override GUIStyle GetDefaultStyle() => null;

}
}
#endif