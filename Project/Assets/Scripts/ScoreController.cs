using UnityEngine;
using TMPro;

public class ScoreController : MonoBehaviour
{
    private TextMeshPro[] texts;
    private int score;
    public int initialScore;
    
    void Start()
    {
        texts = GetComponentsInChildren<TextMeshPro>();
        Reset();
    }
    
    public void Reset(){
        score = initialScore;
        SetText();
    }

    public void Increase(int increase){
        score += increase;
        SetText();
    }

    private void SetText(){
        foreach (TextMeshPro text in texts)
            text.text = "" + score;
    }
}
