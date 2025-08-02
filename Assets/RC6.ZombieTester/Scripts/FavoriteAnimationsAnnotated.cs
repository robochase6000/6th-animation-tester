using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RC6.ZombieTester
{
    [CreateAssetMenu(fileName = "FavoriteAnimationsAnnotated", menuName = "ScriptableObjects/FavoriteAnimationsAnnotated", order = 1)]
    public class FavoriteAnimationsAnnotated : ScriptableObject
    {
        public FavoriteAnimations Source;
        
        [Serializable]
        public class Category
        {
            public string Name;
            public KeyCode KeyCode;
        }
        public List<Category> Categories = new();
        
        
        [Serializable]
        public class Tag
        {
            public string Name;
        }
        public List<Tag> Tags = new();
        
        
        [Serializable]
        public class Item
        {
            public AnimationClip Clip;
            public string Notes;
            public List<string> Categories = new List<string>();
            public List<string> Tags = new();
            public bool RootMotion = false;
        }

        public List<Item> Items = new();

        public void CopySource()
        {
            if (Source == null)
            {
                return;
            }
            
            Categories.Clear();
            Items.Clear();

            AddCategory("Idle", KeyCode.Alpha1);
            AddCategory("IdleAggro", KeyCode.Alpha9);
            AddCategory("IdleTick", KeyCode.Alpha0);
            AddCategory("Walk", KeyCode.Alpha2);
            AddCategory("Run", KeyCode.Alpha3);
            AddCategory("Turn", KeyCode.Alpha4);
            AddCategory("Attack", KeyCode.Alpha5);
            AddCategory("Crawl", KeyCode.Alpha6);
            AddCategory("Hit", KeyCode.Alpha7);
            AddCategory("Grapple", KeyCode.Alpha8);

            AddClips(Source.Idle, "Idle");
            AddClips(Source.Walk, "Walk");
            AddClips(Source.Run, "Run");
            AddClips(Source.Turn, "Turn");
            AddClips(Source.Attack, "Attack");
            AddClips(Source.Crawl, "Crawl");
            AddClips(Source.Hit, "Hit");
            AddClips(Source.Grapple, "Grapple");

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public void AddCategory(string name, KeyCode keyCode)
        {
            Categories.Add(new Category()
            {
                Name = name,
                KeyCode = keyCode
            });
        }

        public void AddClips(List<AnimationClip> sourceList, string category)
        {
            foreach (var x in sourceList)
            {
                var item = new Item()
                {
                    Clip = x,
                    Categories = { category }
                };
                Items.Add(item);
            }
        }
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(FavoriteAnimationsAnnotated))]
    public class FavoriteAnimationsAnnotatedEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Copy From Source"))
            {
                var x = (FavoriteAnimationsAnnotated)target;
                x.CopySource();
            }
        }
    }
#endif
}