using UnityEditor;
using UnityEngine;
using UnityEngine.Video; 

public class TutorialWindow : EditorWindow {
    private TutorialPage currentPage;
    private Vector2 scrollPos;
    // ─── Video playback fields ───
    private VideoPlayer _videoPlayer;
    private RenderTexture _videoRT;

    [MenuItem("Window/Tutorial Guide")]
    public static void OpenWindow() {
        var w = GetWindow<TutorialWindow>("Tutorial");
        w.LoadFirstPage();
    }

    private GameObject _videoGO;  // keep this at class scope alongside _videoPlayer/_videoRT

    
private void OnEnable() {
    // … your other setup …

    // Create a hidden go to host the VideoPlayer component
    _videoGO = new GameObject("TutorialWindowVideoPlayer");
    _videoGO.hideFlags = HideFlags.HideAndDontSave;
    _videoPlayer = _videoGO.AddComponent<VideoPlayer>();
    _videoPlayer.playOnAwake = false;
    _videoPlayer.renderMode   = VideoRenderMode.RenderTexture;

    _videoRT = new RenderTexture(640, 360, 0);
    _videoPlayer.targetTexture = _videoRT;
    // Force repaint whenever the video is playing
    EditorApplication.update += OnEditorUpdate;
}

private void OnDisable() {
    if (_videoPlayer != null) {
        _videoPlayer.Stop();
    }
    if (_videoRT != null) {
        _videoRT.Release();
        DestroyImmediate(_videoRT);
    }
    if (_videoGO != null) {
        DestroyImmediate(_videoGO);
    }
    EditorApplication.update -= OnEditorUpdate;
}


private void OnEditorUpdate() {
  if (_videoPlayer != null && _videoPlayer.isPlaying) {
    Repaint(); 
  }
}



    void LoadFirstPage() {
        var guids = AssetDatabase.FindAssets("t:TutorialPage");
        foreach (var g in guids) {
            var page = AssetDatabase.LoadAssetAtPath<TutorialPage>(AssetDatabase.GUIDToAssetPath(g));
            if (page.previousPage == null) { currentPage = page; break; }
        }
    }

    void OnGUI() {
    if (currentPage == null) {
        EditorGUILayout.HelpBox(
            "Create TutorialPage assets via Assets > Create > Tutorial > Tutorial Page.",
            MessageType.Warning
        );
        return;
    }

    // 1) Scroll view (no ExpandHeight, no ExpandWidth)
    scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

    // 2) Title
    GUILayout.Label(currentPage.pageTitle, EditorStyles.boldLabel);
    GUILayout.Space(6);

    // 3) Body text
    EditorGUILayout.LabelField(
        currentPage.bodyText,
        EditorStyles.wordWrappedLabel
    );
    GUILayout.Space(6);

    // 4) Sections
    foreach (var s in currentPage.sections) {
        switch (s.kind) {
            case TutorialPage.Section.Kind.Text:
                EditorGUILayout.LabelField(
                    s.text,
                    EditorStyles.wordWrappedLabel
                );
                GUILayout.Space(4);
                break;

            case TutorialPage.Section.Kind.Image:
                if (s.image != null) {
                    // Compute display size (never larger than native)
                    float maxW     = position.width - 40f;
                    float displayW = Mathf.Min(maxW, s.image.width);
                    float aspect   = (float)s.image.height / s.image.width;
                    float displayH = displayW * aspect;

                    // Reserve a rect exactly that size
                    Rect imgRect = GUILayoutUtility.GetRect(
                        displayW,      // min width
                        displayH,      // min height
                        displayW,      // max width
                        displayH       // max height
                    );

                    // Draw the texture scaled to fit (letterbox if needed)
                    GUI.DrawTexture(imgRect, s.image, ScaleMode.ScaleToFit);

                    // Fixed gap after image
                    GUILayout.Space(4);
                }
                break;

case TutorialPage.Section.Kind.Video:
    if (s.video != null) {
         // ─── Compute display size (never upscale) ───
         float maxW     = position.width - 40f;
         float clipW    = s.video.width;
         float clipH    = s.video.height;
         float displayW = Mathf.Min(maxW, clipW);
         float displayH = displayW * (clipH / clipW);

         // ─── Resize RT if needed ───
         if (_videoRT.width != (int)displayW || _videoRT.height != (int)displayH) {
             _videoRT.Release();
             _videoRT.width  = (int)displayW;
             _videoRT.height = (int)displayH;
             _videoRT.Create();
             _videoPlayer.targetTexture = _videoRT;
         }

         // ─── Assign & play if not already playing this clip ───
         if (_videoPlayer.clip != s.video) {
             _videoPlayer.clip = s.video;
             _videoPlayer.Play();
         }

         // ─── Reserve rect & draw ───
         Rect vidRect = GUILayoutUtility.GetRect(
             displayW, displayH, displayW, displayH
         );
         GUI.DrawTexture(vidRect, _videoRT, ScaleMode.ScaleToFit);

        // ─── Slim gap between video and controls ───
        GUILayout.Space(2);

        // ─── Controls directly under the video ───
        GUILayout.BeginHorizontal();
        if (_videoPlayer.clip != s.video) {
            if (GUILayout.Button("▶ Load & Play")) {
                _videoPlayer.clip = s.video;
                _videoPlayer.Play();
            }
        } else {
            if (_videoPlayer.isPlaying) {
                if (GUILayout.Button("⏸ Pause")) _videoPlayer.Pause();
            } else {
                if (GUILayout.Button("▶ Play")) _videoPlayer.Play();
            }
        }
        if (GUILayout.Button("⏹ Stop")) {
            _videoPlayer.Stop();
        }
        GUILayout.EndHorizontal();

        // ─── Slim gap before next section ───
        GUILayout.Space(2);
   }
break;

            case TutorialPage.Section.Kind.Link:
                if (GUILayout.Button(s.text, EditorStyles.linkLabel))
                    Application.OpenURL(s.url);
                GUILayout.Space(4);
                break;
        }
    }

    EditorGUILayout.EndScrollView();

    // 5) Navigation
    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Button("Restart")) ResetToFirst();
    GUILayout.FlexibleSpace();
    if (currentPage.previousPage != null && GUILayout.Button("← Back"))
        currentPage = currentPage.previousPage;
    if (currentPage.nextPage != null && GUILayout.Button("Next →"))
        currentPage = currentPage.nextPage;
    EditorGUILayout.EndHorizontal();
}



    void ResetToFirst() {
        while (currentPage.previousPage != null) currentPage = currentPage.previousPage;
    }
}