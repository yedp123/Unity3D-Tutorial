using UnityEngine;
using UnityEngine.Video;          // ← add this
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Tutorial/Tutorial Page")]
public class TutorialPage : ScriptableObject {
    public string pageTitle;
    [TextArea(4,10)] public string bodyText;

    [System.Serializable]
    public class Section {
        public enum Kind { Text, Image, Link, Video }  // ← add Video
        public Kind kind;
        [TextArea] public string text;
        public Texture2D image;
        public string url;
        public VideoClip video;                       // ← add this
    }

    public List<Section> sections = new List<Section>();
    public TutorialPage previousPage;
    public TutorialPage nextPage;
}
