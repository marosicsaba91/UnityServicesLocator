﻿using System;
using System.Collections.Generic;
using System.Linq; 
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
 
abstract class DynamicServiceSource
{
    List<Type> _allNonAbstractTypes;
    List<Type> _allAbstractTypes;
    List<Type> _possibleAdditionalTypes;
    Dictionary<Type, object> _typeToServiceOnSource;
    Dictionary<Type, ITagged> _typeToTagProviderOnSource;
    ServiceSource _setting;
    bool _isDynamicDataInitialized = false;
 
    public Object LoadedObject { get; private set; } // GameObject or ScriptableObject

    public Dictionary<Type, object> InstantiatedServices { get; private set; } =
        new Dictionary<Type, object>();

    public abstract Loadability Loadability { get; }

    public bool TryGetService(
        Type type,
        IServiceSourceSet set,
        object[] conditionTags,
        List<SerializableTag> serializedTags,
        out object service,
        out bool newInstance)
    {
        newInstance = false;
        Loadability loadability = Loadability;
        if (loadability.type != Loadability.Type.Loadable)
        {
            service = default;
            return false;
        }

        if (conditionTags!= null && conditionTags.Length>0) 
        {
            ITagged tagged = _typeToTagProviderOnSource[type];
            bool success = tagged != null;
            if (success)
            {
                foreach (object tag in conditionTags)
                {
                    if (tag == null) continue;
                    if (tagged.GetTags().Contains(tag)) continue;
                    if ( serializedTags.Any(serializedTag => serializedTag.TagObject.Equals(tag))) continue; 
                    
                    success = false;
                    break;
                }
            }

            if (!success)
            {
                service = default;
                return false;
            }
        }

        if (LoadedObject == null)
        {
            Transform parentObject = null;

            if (NeedParentTransform)
            {
                parentObject = set != null && set.GetType().IsSubclassOf(typeof(Component))
                    ? ((Component) set).transform
                    : ServiceLocator.ParentObject;
            }
            
            LoadedObject = Instantiate(parentObject);
            newInstance = true;
            TryInitializeService();
        }

        if (!InstantiatedServices.ContainsKey(type))
            InstantiatedServices.Add(type,  GetService(type, LoadedObject));

        service = InstantiatedServices[type];
        return true; 
    }

    void TryInitializeService()
    {
        if (LoadedObject == null) return;
        switch (LoadedObject)
        {
            case ScriptableObject so:
            {
                if (so is IInitializable initSo)
                    initSo.Initialize();
                break;
            }
            case GameObject go:
            {
                IInitializable[] initializables = go.GetComponents<IInitializable>();
                foreach (IInitializable initializable in initializables)
                    initializable.Initialize();
                break;
            }
        }
    }

    protected abstract bool NeedParentTransform { get; }

    protected abstract Object Instantiate(Transform parent);

    protected abstract object GetService( Type type, Object instantiatedObject); 
    
    public abstract string Name { get; }

    public abstract Object SourceObject { get; }

    public IReadOnlyList<Type> GetAllNonAbstractTypes()
    {
        InitDynamicTypeDataIfNeeded();
        return _allNonAbstractTypes;
    }

    public IReadOnlyList<Type> GetAllAbstractTypes()
    {
        InitDynamicTypeDataIfNeeded();
        return _allAbstractTypes;
    }

    public IReadOnlyList<Type> GetPossibleAdditionalTypes()
    {         
        InitDynamicTypeDataIfNeeded();
        return _possibleAdditionalTypes;
    }

    public object GetServiceOnSource(Type serviceType)
    { 
        InitDynamicTypeDataIfNeeded();
        return _typeToServiceOnSource[serviceType];
    } 


    void InitDynamicTypeDataIfNeeded()
    { 
        if (_isDynamicDataInitialized) return;
        
        _allNonAbstractTypes = GetNonAbstractTypes();
        _allAbstractTypes = new List<Type>();
        _typeToServiceOnSource = new Dictionary<Type, object>();
        _typeToTagProviderOnSource = new Dictionary<Type, ITagged>();
        _possibleAdditionalTypes = new List<Type>(); 
        foreach (Type concreteType in _allNonAbstractTypes)
        { 
            object serviceInstanceOnSourceObject = GetServiceOnSourceObject(concreteType);
            ITagged tagProviderInstanceOnSourceObject =
                serviceInstanceOnSourceObject is ITagged tagged ? tagged : null;


            IEnumerable<Type> abstractTypes = ServiceTypeHelper.GetServicesOfNonAbstractType(concreteType)
                .Where(abstractType => !_allAbstractTypes.Contains(abstractType));

            foreach (Type abstractType in abstractTypes)
            {
                _allAbstractTypes.Add(abstractType);
                _typeToServiceOnSource.Add(abstractType, serviceInstanceOnSourceObject);
                _typeToTagProviderOnSource.Add(abstractType, tagProviderInstanceOnSourceObject);
            }
            
            
            foreach (Type subclass in AllPossibleAdditionalSubclassesOf(concreteType))
                _possibleAdditionalTypes.Add(subclass);
        }

        _isDynamicDataInitialized = true;
    }

    IEnumerable<Type> AllPossibleAdditionalSubclassesOf(Type type, bool includeInterfaces = true )
    {        
        if(type == null)
            yield break;
        
        yield return type;
        if (includeInterfaces)
        {
            foreach (Type interfaceType in type.GetInterfaces())
                if (interfaceType != typeof(ITagged) && interfaceType != typeof(IInitializable))
                    yield return interfaceType;
        }

        Type baseType = type.BaseType;
        if(baseType == null ||
           baseType == typeof(ScriptableObject) ||
           baseType == typeof(Component) ||
           baseType == typeof(Behaviour) ||
           baseType == typeof(MonoBehaviour))
            yield break;
        
        foreach (Type b in AllPossibleAdditionalSubclassesOf(baseType, includeInterfaces: false ))
            yield return b;
    }

    protected abstract List<Type> GetNonAbstractTypes();
    public abstract object GetServiceOnSourceObject(Type type);
 
    public abstract ServiceSourceTypes SourceType { get; }
    public abstract IEnumerable<ServiceSourceTypes> AlternativeSourceTypes { get; }

    protected abstract void ClearService();

    public IEnumerable<object> GetDynamicTags()
    {
        InitDynamicTypeDataIfNeeded();

        foreach (KeyValuePair<Type, ITagged> pair in _typeToTagProviderOnSource)
            if (pair.Value != null)
            {
                foreach (object tag in pair.Value.GetTags())
                    yield return tag;
            } 
    }

    public void ClearInstancesAndCachedTypes()
    {
        ClearService();
        LoadedObject = null;
        InstantiatedServices.Clear();
        ClearCachedTypes();
    }

    public void ClearCachedTypes()
    {
        if(Application.isPlaying)
            return;
        _isDynamicDataInitialized = false;
    }

    public Texture Icon => FileIconHelper.GetIconOfObject(SourceObject);
}
}