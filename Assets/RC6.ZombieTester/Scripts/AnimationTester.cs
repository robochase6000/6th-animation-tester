using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEditor;
using System.Collections;
using RC6.ZombieTester;

public class AnimationTester : MonoBehaviour
{
    [Tooltip("The target GameObject with the Animator component.")]
    public GameObject Target;

    [Tooltip("Optional: Filter clips by folder path (e.g., 'Assets/Animations/Humanoid'). Leave empty to load all clips.")]
    public string filterFolderPath = "";

    [Tooltip("Time to wait between animations when auto-cycling (in seconds).")]
    public float autoCycleDelay = 1f;

    [Tooltip("Enable to automatically cycle through animations on start.")]
    public bool autoCycle = false;

    [Tooltip("The neutral position to reset the character to before each clip.")]
    public Vector3 neutralPosition = Vector3.zero;

    [Tooltip("ScriptableObject to store favorite animations.")]
    public FavoriteAnimationsAnnotated favoriteAnimations;

    public enum EMode
    {
        FindClipsInProject,
        UseFavoritedClips,
    }
    public EMode Mode = EMode.FindClipsInProject;

    private Animator animator;
    private List<AnimationClip> clips;
    private List<AnimationClip> filteredClips;
    private Dictionary<AnimationClip, string> clipPaths;
    private PlayableGraph playableGraph;
    private int currentIndex = -1;

    private FavoriteAnimationsAnnotated.Item _currentItem = null;
    private int CurrentIndex
    {
        get => currentIndex;
        set
        {
            currentIndex = value;
            if (_resumeProgress)
            {
                PlayerPrefs.SetInt(PlayPrefsKey_CurrentIndex, currentIndex);
                PlayerPrefs.Save();
            }
            
            // Ensure slider value stays in sync
            _clipSliderValue = Mathf.RoundToInt(currentIndex);

            _currentItem = null;
            if (currentIndex >= 0 && currentIndex < filteredClips.Count)
            {
                var currentClip = filteredClips[currentIndex];
                foreach (var item in favoriteAnimations.Items)
                {
                    if (item.Clip == currentClip)
                    {
                        _currentItem = item;
                        break;
                    }
                }
            }
            
            PlayClip(CurrentIndex);
        }
    }
    private string currentClipName = "";
    private string currentClipPath = "";
    private string searchQuery = "";
    private bool isPaused = false;

    // For the favorites window
    private Rect animControlsRect = new Rect(0f, 0f, 300, 400);
    private Rect favoritesWindowRect = new Rect(Screen.width - 310, 10, 300, 400);
    private Vector2 scrollPosition;
    private Vector2 scrollPositionAnimControls;

    [SerializeField] private bool _resumeProgress = true;
    public const string PlayPrefsKey_CurrentIndex = "AnimationTester.currentIndex";
    
    [SerializeField] private GUIStyle _defaultLabelStyle;
    [SerializeField] private GUIStyle _highlightedLabelStyle;
    void Start()
    {
        // Get Animator component from Target
        if (Target == null)
        {
            Debug.LogError("Target GameObject is not assigned in the Inspector.");
            return;
        }

        animator = Target.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("No Animator component found on the Target GameObject. Attach one to the character root.");
            return;
        }

        // Disable any existing controller to let playables take control
        animator.runtimeAnimatorController = null;

        clips = new List<AnimationClip>();
        clipPaths = new Dictionary<AnimationClip, string>();
        
        switch (Mode)
        {
            case EMode.FindClipsInProject:
                // Find all AnimationClips in the project using AssetDatabase
                string[] guids = AssetDatabase.FindAssets("t:AnimationClip");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (clip != null && !clip.legacy) // Ensure clip is not Legacy
                    {
                        if (string.IsNullOrEmpty(filterFolderPath) || path.StartsWith(filterFolderPath))
                        {
                            clips.Add(clip);
                            clipPaths[clip] = path;
                        }
                    }
                }
                break;
            case EMode.UseFavoritedClips:
                foreach (var item in favoriteAnimations.Items)
                {
                    if (item.Clip == null || item.Clip.legacy)
                    {
                        continue;
                    }
                    var path = AssetDatabase.GetAssetPath(item.Clip);
                    clips.Add(item.Clip);
                    clipPaths[item.Clip] = path;
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (clips.Count == 0)
        {
            Debug.LogError("No non-Legacy AnimationClips found in the project. Check your filterFolderPath or ensure you have AnimationClips in your Assets.");
            return;
        }

        Debug.Log($"Loaded {clips.Count} non-Legacy animation clips.");

        // Initialize filtered clips
        filteredClips = clips.OrderBy(clip => clip.name).ToList();

        if (_resumeProgress)
        {
            if (PlayerPrefs.HasKey(PlayPrefsKey_CurrentIndex))
            {
                CurrentIndex = Mathf.Clamp(PlayerPrefs.GetInt(PlayPrefsKey_CurrentIndex), 0, filteredClips.Count - 1);
            }
        }
        
        // Start with the first clip
        NextClip();

        if (autoCycle)
        {
            StartCoroutine(AutoCycleCoroutine());
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayClip(CurrentIndex);
        }
        
        // Press Space or Right Arrow for next clip
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            NextClip(Input.GetKey(KeyCode.LeftShift) ? 20 : 1);
        }

        // Press Left Arrow for previous clip
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            PreviousClip(Input.GetKey(KeyCode.LeftShift) ? -20 : -1);
        }

        // Press P to toggle pause
        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
        }

        // Press number keys 1-8 to toggle favorite status
        if (favoriteAnimations != null && CurrentIndex >= 0)
        {
            AnimationClip currentClip = filteredClips[CurrentIndex];

            foreach (var category in favoriteAnimations.Categories)
            {
                if (category.KeyCode == KeyCode.None)
                {
                    continue;
                }
                
                if (Input.GetKeyDown(category.KeyCode))
                {
                    ToggleFavoriteList(currentClip, category);
                }
            }
        }
    }

    private void ToggleFavoriteList(AnimationClip clip, FavoriteAnimationsAnnotated.Category category)
    {
        // is it favorited already?
        bool found = false;
        FavoriteAnimationsAnnotated.Item itemToRemove = null;
        foreach (var item in favoriteAnimations.Items)
        {
            if (item.Clip == clip)
            {
                found = true;
                if (item.Categories.Contains(category.Name))
                {
                    item.Categories.Remove(category.Name);
                    if (item.Categories.Count == 0)
                    {
                        itemToRemove = item;
                    }
                }
                else
                {
                    item.Categories.Add(category.Name);
                }

                break;
            }
        }

        if (!found)
        {
            _currentItem = new FavoriteAnimationsAnnotated.Item()
            {
                Clip = clip,
                Categories = { category.Name }
            };
            favoriteAnimations.Items.Add(_currentItem);
        }
        else if (itemToRemove != null)
        {
            favoriteAnimations.Items.Remove(itemToRemove);
            if (_currentItem == itemToRemove)
            {
                _currentItem = null;
            }
        }
    }

    private void NextClip(int incrementAmount = 1)
    {
        CurrentIndex = (CurrentIndex + incrementAmount) % filteredClips.Count;
        PlayClip(CurrentIndex);
    }

    private void PreviousClip(int decrementAmount = -1)
    {
        CurrentIndex = (CurrentIndex + decrementAmount + filteredClips.Count) % filteredClips.Count;
        PlayClip(CurrentIndex);
    }

    private void PlayClip(int index)
    {
        // Reset position to neutral
        Target.transform.position = neutralPosition;

        // Clean up previous graph if exists
        if (playableGraph.IsValid())
        {
            playableGraph.Stop();
            playableGraph.Destroy();
        }

        // Create a new playable graph
        playableGraph = PlayableGraph.Create("AnimationTester");
        var output = AnimationPlayableOutput.Create(playableGraph, "Output", animator);
        var clipPlayable = AnimationClipPlayable.Create(playableGraph, filteredClips[index]);
        
        // Connect and play
        output.SetSourcePlayable(clipPlayable);
        if (!isPaused)
        {
            playableGraph.Play();
        }
        else
        {
            playableGraph.Evaluate(0);
        }

        currentClipName = filteredClips[index].name;
        currentClipPath = clipPaths[filteredClips[index]];
        Debug.Log($"Playing animation: {currentClipName} (Length: {filteredClips[index].length}s, Index: {CurrentIndex + 1}/{filteredClips.Count}, Path: {currentClipPath})");
    }

    private void TogglePause()
    {
        isPaused = !isPaused;
        if (playableGraph.IsValid())
        {
            if (isPaused)
            {
                playableGraph.Stop();
            }
            else
            {
                playableGraph.Play();
            }
        }
        Debug.Log($"Animation playback {(isPaused ? "paused" : "resumed")}.");
    }

    private IEnumerator AutoCycleCoroutine()
    {
        while (true)
        {
            // Wait for the current clip to finish plus delay
            if (CurrentIndex >= 0 && !isPaused)
            {
                yield return new WaitForSeconds(filteredClips[CurrentIndex].length + autoCycleDelay);
                NextClip();
            }
            else
            {
                yield return null;
            }
        }
    }

    void OnGUI()
    {
        animControlsRect = GUI.Window(0, animControlsRect, DrawAnimControlsWindow, "Controls");
        // Favorites window
        if (favoriteAnimations != null)
        {
            favoritesWindowRect = GUI.Window(1, favoritesWindowRect, DrawFavoritesWindow, "Favorites List");
        }
    }

    private float _clipSliderValue = 0f;

    private void DrawAnimControlsWindow(int id)
    {
        GUILayout.BeginVertical();
        
        // Draw horizontal slider with integer steps from 0 to list count - 1
        {
            _clipSliderValue =
                GUILayout.HorizontalSlider(_clipSliderValue, 0f, filteredClips.Count - 1, GUILayout.Width(200));

            // Round to nearest integer to ensure no decimals
            int index = Mathf.RoundToInt(_clipSliderValue);

            // Ensure slider value stays at integer
            _clipSliderValue = index;

            if (index != CurrentIndex)
            {
                CurrentIndex = index;
            }
        }
        
        scrollPositionAnimControls = GUILayout.BeginScrollView(scrollPositionAnimControls);

        DrawAnimControlsGUI();

        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUI.DragWindow(new Rect(0, 0, animControlsRect.width, 20));
    }

    private void DrawAnimControlsGUI()
    {
        // Display current animation info
        GUILayout.Label($"Current Animation: {currentClipName} ({CurrentIndex + 1}/{filteredClips.Count})");
        GUILayout.Label($"Path: {currentClipPath}");
        
        // Search bar
        GUILayout.Label("Search:");
        searchQuery = GUILayout.TextField(searchQuery);
        if (GUILayout.Button("Apply"))
        {
            ApplySearchFilter();
        }

        // Ping in Project button
        if (GUILayout.Button("Ping in Project") && CurrentIndex >= 0)
        {
            EditorGUIUtility.PingObject(filteredClips[CurrentIndex]);
        }

        if (favoriteAnimations != null)
        {
            foreach (var category in favoriteAnimations.Categories)
            {
                bool categorySelected = _currentItem != null && _currentItem.Categories.Contains(category.Name); 
                
                GUILayout.Label($"{category.KeyCode} - {category.Name}", categorySelected ? _highlightedLabelStyle : _defaultLabelStyle);
            }
            GUI.color = Color.white;
        }
    }

    private void DrawColoredLabel(Rect position, string text, Color color)
    {
        GUIStyle coloredStyle = new GUIStyle();
        coloredStyle.fontSize = 20;
        coloredStyle.normal.textColor = color;
        GUI.Label(position, text, coloredStyle);
    }

    private void DrawFavoritesWindow(int windowID)
    {
        GUILayout.BeginVertical();
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

/*
        DrawCategoryList("Idle", favoriteAnimations.Idle);
        DrawCategoryList("Walk", favoriteAnimations.Walk);
        DrawCategoryList("Run", favoriteAnimations.Run);
        DrawCategoryList("Turn", favoriteAnimations.Turn);
        DrawCategoryList("Attack", favoriteAnimations.Attack);
        DrawCategoryList("Crawl", favoriteAnimations.Crawl);
        DrawCategoryList("Hit", favoriteAnimations.Hit);
        DrawCategoryList("Grapple", favoriteAnimations.Grapple);
*/

        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUI.DragWindow(new Rect(0, 0, favoritesWindowRect.width, 20));
    }

    private void DrawCategoryList(string category, List<AnimationClip> list)
    {
        GUILayout.Label($"{category} ({list.Count}):", GUILayout.Height(20));
        foreach (AnimationClip clip in list)
        {
            GUI.color = clip.name == currentClipName ? Color.green : Color.white;
            if (GUILayout.Button(clip.name))
            {
                HandleClipSelected(clip);
            }
        }
        GUILayout.Space(10);
        GUI.color = Color.white;
    }

    private void HandleClipSelected(AnimationClip clip)
    {
        if (filteredClips.Contains(clip))
        {
            CurrentIndex = filteredClips.IndexOf(clip);
            PlayClip(CurrentIndex);
        }
    }

    private void ApplySearchFilter()
    {
        filteredClips = clips
            .Where(clip => string.IsNullOrEmpty(searchQuery) || clip.name.ToLower().Contains(searchQuery.ToLower()))
            .OrderBy(clip => clip.name)
            .ToList();

        if (filteredClips.Count == 0)
        {
            Debug.LogWarning("No clips match the search query.");
            filteredClips = clips.OrderBy(clip => clip.name).ToList();
            CurrentIndex = -1;
        }
        else
        {
            CurrentIndex = -1;
            NextClip();
        }

        Debug.Log($"Filtered to {filteredClips.Count} clips matching query: '{searchQuery}'");
    }

    void OnDestroy()
    {
        if (playableGraph.IsValid())
        {
            playableGraph.Destroy();
        }
    }
}