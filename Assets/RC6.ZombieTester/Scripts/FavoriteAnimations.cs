using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FavoriteAnimations", menuName = "ScriptableObjects/FavoriteAnimations", order = 1)]
public class FavoriteAnimations : ScriptableObject
{
    public List<AnimationClip> Idle = new List<AnimationClip>();
    public List<AnimationClip> Walk = new List<AnimationClip>();
    public List<AnimationClip> Run = new List<AnimationClip>();
    public List<AnimationClip> Turn = new List<AnimationClip>();
    public List<AnimationClip> Attack = new List<AnimationClip>();
    public List<AnimationClip> Crawl = new List<AnimationClip>();
    public List<AnimationClip> Hit = new List<AnimationClip>();
    public List<AnimationClip> Grapple = new List<AnimationClip>();
}