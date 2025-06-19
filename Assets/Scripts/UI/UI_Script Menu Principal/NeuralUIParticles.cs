using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class NeuralUIParticles : MonoBehaviour
{
    [Header("Configuración de Nodos")]
    public RectTransform canvasTransform;
    public GameObject nodePrefab;
    public int nodeCount = 20;
    public float connectionDistance = 200f;
    public float moveAmplitude = 20f;
    public float moveSpeed = 0.5f;
    public RectTransform particleContainer;

    [Header("Líneas")]
    public Material lineMaterial;

    private class Node
    {
        public GameObject obj;
        public Vector2 basePosition;
        public float offsetX, offsetY;
    }

    private List<Node> nodes = new List<Node>();
    private List<UILineRenderer> lines = new List<UILineRenderer>();
    private List<(Node, Node, UILineRenderer)> connections = new List<(Node, Node, UILineRenderer)>();

    void Start()
    {
        GenerateNodes();
        ConnectNodes();
    }

    void Update()
    {
        AnimateNodes();
        UpdateLines();
    }

    void GenerateNodes()
    {
        float halfWidth = canvasTransform.rect.width / 2f;
        float halfHeight = canvasTransform.rect.height / 2f;

        float marginX = 100f;
        float marginY = 100f;

        for (int i = 0; i < nodeCount; i++)
        {
            GameObject node = Instantiate(nodePrefab, particleContainer);
            Vector2 basePos = new Vector2(
                Random.Range(-halfWidth + marginX, halfWidth - marginX),
                Random.Range(-halfHeight + marginY, halfHeight - marginY)
            );

            node.GetComponent<RectTransform>().anchoredPosition = basePos;

            Node n = new Node
            {
                obj = node,
                basePosition = basePos,
                offsetX = Random.Range(0f, 100f),
                offsetY = Random.Range(0f, 100f)
            };

            nodes.Add(n);
        }
    }
    void ConnectNodes()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = i + 1; j < nodes.Count; j++)
            {
                float dist = Vector2.Distance(
                    nodes[i].basePosition,
                    nodes[j].basePosition
                );

                if (dist <= connectionDistance)
                {
                    GameObject lineObj = new GameObject("UILine");
                    lineObj.transform.SetParent(particleContainer, false);

                    UILineRenderer line = lineObj.AddComponent<UILineRenderer>();
                    line.material = lineMaterial;

                    connections.Add((nodes[i], nodes[j], line));
                }
            }
        }
    }

    void AnimateNodes()
    {
        float time = Time.time * moveSpeed;

        foreach (Node n in nodes)
        {
            float x = n.basePosition.x + Mathf.PerlinNoise(time + n.offsetX, 0f) * moveAmplitude - moveAmplitude / 2;
            float y = n.basePosition.y + Mathf.PerlinNoise(0f, time + n.offsetY) * moveAmplitude - moveAmplitude / 2;
            n.obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
        }
    }

    void UpdateLines()
    {
        foreach (var (a, b, line) in connections)
        {
            Vector2 posA = a.obj.GetComponent<RectTransform>().anchoredPosition;
            Vector2 posB = b.obj.GetComponent<RectTransform>().anchoredPosition;
            line.SetPositions(posA, posB);
        }
    }
}
