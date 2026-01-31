using UnityEngine;

public class Sniper_MarkIndicator : MonoBehaviour
{
    [SerializeField] private float heightOffset = 1.9f;

    private SpriteRenderer sr;
    private TextMesh text;

    public void Setup(float newHeightOffset)
    {
        heightOffset = newHeightOffset;

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = Sniper_RuntimeSprites.GetRingSprite();
        sr.sortingOrder = 2000;
        sr.color = new Color(1f, 0.35f, 0.35f, 1f);

        GameObject t = new GameObject("Index");
        t.transform.SetParent(transform, false);
        t.transform.localPosition = new Vector3(0, 0, 0);

        text = t.AddComponent<TextMesh>();
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 48;
        text.characterSize = 0.05f;
        text.color = Color.white;
        text.text = "";

        t.AddComponent<Sniper_SimpleBillboard>();
        gameObject.AddComponent<Sniper_SimpleBillboard>();

        transform.localScale = Vector3.one * 0.7f;
    }

    public void SetIndex(int index)
    {
        if (text != null)
            text.text = index.ToString();
    }

    private void LateUpdate()
    {
        transform.localPosition = new Vector3(0, heightOffset, 0);
    }
}
