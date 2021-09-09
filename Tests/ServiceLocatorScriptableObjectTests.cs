﻿using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityServiceLocator; 

namespace Tests
{
    public class ServiceLocatorScriptableObjectTests
    {
        const string testScriptableObject1Name = "UnityServiceLocatorTestScriptableObject1";
        const string testScriptableObject2Name = "UnityServiceLocatorTestScriptableObject2"; 
        SceneServiceInstaller _installer;  
        ScriptableObject _testScriptableObject1;
        ScriptableObject _testScriptableObject2;
        ServiceSource _serviceSource1;
        ServiceSource _serviceSource2;

        internal const int testTag1 = 789;
        internal const string testTag2 = "I hate snakes!";
        internal static readonly Vector2 testTag3 = new Vector2(x: 22f, y: 33f);

        [OneTimeSetUp]
        public static void SetUp() =>ServiceLocatorSceneGameObjectTests.CleanupInstallers();
        
        [UnityTest, Order(order: 1)]
        public IEnumerator Test1_CreateSceneInstaller()
        {
            var installerGameObject = new GameObject("TestInstaller");
            _installer = installerGameObject.AddComponent<SceneServiceInstaller>();
            
            yield return null;
        }
        
        [UnityTest, Order(order: 2)]
        public IEnumerator Test2_AddPrefabSourceToInstaller()
        {
            _testScriptableObject1 = (ScriptableObject) Resources.Load(testScriptableObject1Name);
            _testScriptableObject2 = (ScriptableObject) Resources.Load(testScriptableObject2Name); 
            _serviceSource1 = _installer.AddServiceSource(_testScriptableObject1, ServiceSourceTypes.FromScriptableObjectPrototype);
            _serviceSource2 = _installer.AddServiceSource(_testScriptableObject2, ServiceSourceTypes.FromScriptableObjectFile);
            
            yield return null;
        } 

        [UnityTest, Order(order: 3)]
        public IEnumerator Test3_GetScriptableObjectSourceFromLocator()
        {       
            var service1 = ServiceLocator.Get<UnityServiceLocatorTestScriptableObject1>();
            bool foundService1 = service1 != null && service1 != _testScriptableObject1;
            Assert.IsTrue(foundService1);

            var service2 = ServiceLocator.Get<IUnityServiceLocatorTestInterface2>();
            bool foundService2= service2 != null;
            Assert.IsTrue(foundService2);
            yield return null;
        }
        
        [UnityTest, Order(order: 4)] 
        public IEnumerator Test4_SearchForTags()
        {
            bool success1 = ServiceLocator.TryGet(  
                new object[]{testTag1 + 1},
                out UnityServiceLocatorTestScriptableObject1 _); 
            Assert.IsFalse(success1);
            
            bool success2 = ServiceLocator.TryGet(  
                new object[]{testTag1 },
                out UnityServiceLocatorTestScriptableObject1 _);
            Assert.IsTrue(success2);
            
            bool success3 = ServiceLocator.TryGet(  
                new object[]{testTag1, testTag2},
                out UnityServiceLocatorTestScriptableObject1 _);
            Assert.IsTrue(success3); 
            
            bool success4 = ServiceLocator.TryGet(  
                new object[]{testTag1, testTag2+ "x"},
                out UnityServiceLocatorTestScriptableObject1 _);
            Assert.IsFalse(success4);
            
            yield return null;
        }
        
        [UnityTest, Order(order: 5)] 
        public IEnumerator Test5_AddAndSearchForAdditionalTags()
        { 
            bool success1 = ServiceLocator.TryGet(  
                new object[]{testTag1, testTag2, testTag3},
                out UnityServiceLocatorTestScriptableObject1 _);
            Assert.IsFalse(success1); 
            
            _serviceSource1.AddTag(testTag3);
            yield return null; 
            
            bool success2 = ServiceLocator.TryGet(new object[]{testTag1, testTag2, testTag3},
                out UnityServiceLocatorTestScriptableObject1 _);
            Assert.IsTrue(success2);
        } 
    }
}