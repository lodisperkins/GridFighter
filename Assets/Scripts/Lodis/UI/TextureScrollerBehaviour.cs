using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class TextureScrollerBehaviour : MonoBehaviour
{
    [Header("Texture Settings")]
    [Tooltip("Name of the texture property to scroll, e.g., '_MainTex' or '_EmissiveMap'.")]
    [SerializeField] private string texturePropertyName = "_MainTex";

    [Tooltip("Direction to scroll the texture in (X or Y).")]
    [SerializeField] private ScrollDirection scrollDirection = ScrollDirection.X;

    [Tooltip("Speed of the texture scrolling.")]
    [SerializeField] private float scrollSpeed = 0.1f;

    private Renderer rend;
    private Material mat;
    private Vector2 currentOffset;
    private int propertyID;

    private enum ScrollDirection
    {
        X,
        Y
    }

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        mat = rend.material;
        mat.SetVector(texturePropertyName, currentOffset);
    }

    private void Update()
    {
        float delta = Time.time * scrollSpeed;

        if (scrollDirection == ScrollDirection.X)
            currentOffset.x = delta % 1f;
        else
            currentOffset.y = delta % 1f;

        mat.SetVector(texturePropertyName, currentOffset);
    }

    private void OnValidate()
    {
        // Clamp speed to avoid negative wrap-around artifacts
        if (scrollSpeed < 0f)
            scrollSpeed = Mathf.Abs(scrollSpeed);
    }
}
