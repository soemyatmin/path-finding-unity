using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Pathfinder : MonoBehaviour {
    Node m_startNode;
    Node m_goalNode;

    Graph m_graph;
    GraphView m_graphView;

    PriorityQueue<Node> m_frontierNodes;
    List<Node> m_exploredNodes;
    List<Node> m_pathNodes;

    public Color startColor = Color.green;
    public Color goalColor = Color.red;
    public Color frontierColor = Color.magenta;
    public Color exploredColor = Color.gray;
    public Color pathColor = Color.cyan;

    public Color arrowColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    public Color highlightColor = new Color(15f, 15f, 0.5f, 1f);

    public bool showIterations = true;
    public bool showColor = true;
    public bool showArrow = true;
    public bool exitOnGoal = true;

    public bool isComplete = false;
    int m_iteration = 0;

    public enum Mode {
        BreadthFirstSearch = 0,
        Dijkstra = 1,
        GreedyBastFirstSearch = 2,
        AStar = 3
    }

    public Mode mode = Mode.BreadthFirstSearch;
    public void Init(Graph graph, GraphView graphView, Node start, Node goal) {
        if (graph == null || graphView == null || start == null || goal == null) {
            Debug.LogError("Missing Component");
            return;
        }
        if (start.nodeType == NodeType.Blocked || goal.nodeType == NodeType.Blocked) {
            Debug.LogError("Start and goal node should not be blocked");
            return;
        }
        m_graph = graph;
        m_graphView = graphView;
        m_startNode = start;
        m_goalNode = goal;
        ShowColors();

        m_frontierNodes = new PriorityQueue<Node>();
        m_frontierNodes.Enqueue(start);
        m_exploredNodes = new List<Node>();
        m_pathNodes = new List<Node>();

        for (int x = 0; x < m_graph.Width; x++) {
            for (int y = 0; y < m_graph.Height; y++) {
                m_graph.nodes[x, y].Reset();
            }
        }
        isComplete = false;
        m_iteration = 0;
        m_startNode.distanceTraveled = 0;
    }
    private void ShowColors(bool lerpColor = false, float lerpValue = 0.5f) {
        ShowColors(m_graphView, m_startNode, m_goalNode, lerpColor, lerpValue);
    }

    private void ShowColors(GraphView graphView, Node start, Node goal, bool lerpColor = false, float lerpValue = 0.5f) {
        if (graphView == null || start == null || goal == null) {
            Debug.LogError("Missing Component in Show Color");
            return;
        }
        if (m_frontierNodes != null) {
            graphView.ColorNodes(m_frontierNodes.ToList(), frontierColor, lerpColor, lerpValue);
        }
        if (m_exploredNodes != null) {
            graphView.ColorNodes(m_exploredNodes.ToList(), exploredColor, lerpColor, lerpValue);
        }
        if (m_pathNodes != null && m_pathNodes.Count > 0) {
            graphView.ColorNodes(m_pathNodes, pathColor, lerpColor, lerpValue * 2f);
        }
        NodeView startNodeView = graphView.nodeViews[start.xIndex, start.yIndex];
        if (startNodeView != null) {
            startNodeView.ColorNode(startColor);
        }
        NodeView goalNodeView = graphView.nodeViews[goal.xIndex, goal.yIndex];
        if (goalNodeView != null) {
            goalNodeView.ColorNode(goalColor);
        }
    }

    public IEnumerator SearchRoutine(float timeStep) {
        float timeStart = Time.realtimeSinceStartup;
        while (!isComplete) {
            if (m_frontierNodes.Count > 0) {
                Node currentNode = m_frontierNodes.Dequeue();
                m_iteration++;
                if (!m_exploredNodes.Contains(currentNode)) {
                    m_exploredNodes.Add(currentNode);
                }
                if (mode == Mode.BreadthFirstSearch) {
                    ExpandFrontierBreadthFirst(currentNode);
                } else if (mode == Mode.Dijkstra) {
                    ExpandFrontierDijkstra(currentNode);
                } else if (mode == Mode.GreedyBastFirstSearch) {
                    ExpandFrontierGreedyBestFirst(currentNode);
                } else if (mode == Mode.AStar) {
                    ExpandFrontierAStar(currentNode);
                }
                if (m_frontierNodes.Contains(m_goalNode)) {
                    m_pathNodes = GetPathNodes(m_goalNode);
                    if (exitOnGoal) {
                        isComplete = true;
                        Debug.Log("Pathfinder mode: " + mode.ToString() + "____ Path Length: " + m_goalNode.distanceTraveled.ToString());
                    }
                }
                if (showIterations) {
                    ShowDiagnostics(true, 0.5f);
                    yield return new WaitForSeconds(timeStep);
                }
            } else {
                isComplete = true;
            }
        }
        ShowDiagnostics(true, 0.5f);
        Debug.Log("Elapse time : " + (Time.realtimeSinceStartup - timeStart).ToString() + " seconds");
        yield return null;
    }

    private void ShowDiagnostics(bool lerpColor = false, float lerpValue = 0.5f) {
        if (showColor) {
            ShowColors(lerpColor, lerpValue);
        }
        if (m_graphView != null && showArrow) {
            m_graphView.ShowNodeArrows(m_frontierNodes.ToList(), arrowColor);
            if (m_frontierNodes.Contains(m_goalNode)) {
                m_graphView.ShowNodeArrows(m_pathNodes, highlightColor);
            }
        }
    }

    void ExpandFrontierBreadthFirst(Node node) {
        if (node != null) {
            for (int i = 0; i < node.neighbors.Count; i++) {
                if (!m_exploredNodes.Contains(node.neighbors[i]) && !m_frontierNodes.Contains(node.neighbors[i])) {

                    float distanceToNeighbor = m_graph.GetNodeDistance(node, node.neighbors[i]);
                    float newDistanceTraveled = distanceToNeighbor + node.distanceTraveled + (int)node.nodeType;
                    node.neighbors[i].distanceTraveled = newDistanceTraveled;

                    node.neighbors[i].previous = node;
                    node.neighbors[i].priority = m_exploredNodes.Count;
                    m_frontierNodes.Enqueue(node.neighbors[i]);
                }
            }
        }
    }

    void ExpandFrontierDijkstra(Node node) {
        if (node != null) {
            for (int i = 0; i < node.neighbors.Count; i++) {
                if (!m_exploredNodes.Contains(node.neighbors[i])) {
                    float distanceToNeighbor = m_graph.GetNodeDistance(node, node.neighbors[i]);
                    float newDistanceTraveled = distanceToNeighbor + node.distanceTraveled + ((int)node.nodeType) ;
                    if (float.IsPositiveInfinity(node.neighbors[i].distanceTraveled) ||
                        newDistanceTraveled < node.neighbors[i].distanceTraveled) {
                        node.neighbors[i].previous = node;
                        node.neighbors[i].distanceTraveled = newDistanceTraveled;
                    }
                    if (!m_frontierNodes.Contains(node.neighbors[i])) {
                        node.neighbors[i].priority = (int)node.neighbors[i].distanceTraveled;
                        m_frontierNodes.Enqueue(node.neighbors[i]);
                    }

                }
            }
        }
    }

    void ExpandFrontierGreedyBestFirst(Node node) {
        if (node != null) {
            for (int i = 0; i < node.neighbors.Count; i++) {
                if (!m_exploredNodes.Contains(node.neighbors[i]) && !m_frontierNodes.Contains(node.neighbors[i])) {

                    float distanceToNeighbor = m_graph.GetNodeDistance(node, node.neighbors[i]);
                    float newDistanceTraveled = distanceToNeighbor + node.distanceTraveled + (int)node.nodeType;
                    node.neighbors[i].distanceTraveled = newDistanceTraveled;

                    node.neighbors[i].previous = node;
                    if(m_graph != null) {
                        node.neighbors[i].priority = (float)m_graph.GetNodeDistance(node.neighbors[i], m_goalNode);
                    }
                    m_frontierNodes.Enqueue(node.neighbors[i]);
                }
            }
        }
    }

    void ExpandFrontierAStar(Node node) {
        if (node != null) {
            for (int i = 0; i < node.neighbors.Count; i++) {
                if (!m_exploredNodes.Contains(node.neighbors[i])) {
                    float distanceToNeighbor = m_graph.GetNodeDistance(node, node.neighbors[i]);
                    float newDistanceTraveled = distanceToNeighbor + node.distanceTraveled + ((int)node.nodeType);
                    if (float.IsPositiveInfinity(node.neighbors[i].distanceTraveled) || newDistanceTraveled < node.neighbors[i].distanceTraveled) {
                        node.neighbors[i].previous = node;
                        node.neighbors[i].distanceTraveled = newDistanceTraveled;
                    }
                    if (!m_frontierNodes.Contains(node.neighbors[i]) && m_graph != null) {
                        float disctanceToGoal = (float)m_graph.GetNodeDistance(node.neighbors[i], m_goalNode);
                        node.neighbors[i].priority = (float)node.neighbors[i].distanceTraveled + disctanceToGoal;
                        m_frontierNodes.Enqueue(node.neighbors[i]);
                    }

                }
            }
        }
    }

    List<Node> GetPathNodes(Node endNode) {
        List<Node> path = new List<Node>();
        if (endNode == null) {
            return path;
        }
        path.Add(endNode);
        Node currentNode = endNode.previous;
        while (currentNode != null) {
            path.Insert(0, currentNode);
            currentNode = currentNode.previous;
        }
        return path;
    }
}
