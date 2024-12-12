using UnityEngine;
using MelonLoader;

namespace Drova_Modding_API.Systems.Dialogues.Editor
{

    [RegisterTypeInIl2Cpp]
    public class NodeConnectorEditor(IntPtr ptr) : MonoBehaviour(ptr)
    {
        // Positions of the two nodes
        protected Vector2 node1Position = new(100, 100);
        protected Vector2 node2Position = new(300, 200);

        // Size of the nodes
        protected Vector2 nodeSize = new(50, 50);

        // Node colors
        protected Color nodeColor1 = Color.blue;
        protected Color nodeColor2 = Color.red;

        // Line color
        protected Color lineColor = Color.black;

        protected Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.7f); // Dark gray with 70% opacity

        protected string node1Type = "String";
        protected string node1Property = "GlobalLocalPath";

        protected string node2Type = "String";
        protected string node2Property = "Key";


        internal void OnGUI()
        {        
            // Draw the background over half of the screen
            DrawBackground();

            // Draw the nodes as rectangles
            DrawNode(node1Position, ref node1Type, ref node1Property, nodeColor1);
            DrawNode(node2Position, ref node2Type, ref node2Property, nodeColor2);

            // Draw a line connecting the nodes
            DrawEdgeLine(node1Position, node2Position, lineColor);
        }

        // Method to draw a semi-transparent background over half the screen
        void DrawBackground()
        {
            Color previousColor = GUI.color;
            GUI.color = backgroundColor;

            // Draw the background in the center covering half of the screen width
            float bgWidth = Screen.width / 2;
            float bgX = (Screen.width - bgWidth) / 2;
            GUI.Box(new Rect(bgX, 0, bgWidth, Screen.height), "");

            GUI.color = previousColor;
        }

        // Draw a node with editable type and property fields
        void DrawNode(Vector2 position, ref string type, ref string property, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;

            // Draw the node as a rectangle
            GUI.Box(new Rect(position.x, position.y, nodeSize.x, nodeSize.y), "");

            // Editable TextFields for type and property
            type = GUI.TextField(new Rect(position.x + 5, position.y + 5, nodeSize.x - 10, 20), type);
            property = GUI.TextField(new Rect(position.x + 5, position.y + 35, nodeSize.x - 10, 20), property);

            GUI.color = previousColor;
        }

        // Method to draw a line connecting two points
        void DrawLine(Vector2 pointA, Vector2 pointB, Color color)
        {
            Color previousColor = GUI.color; // Save the previous GUI color
            GUI.color = color;               // Set the new color

            // Calculate the difference vector
            Vector2 delta = pointB - pointA;

            // Calculate the angle and length of the line
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            float length = delta.magnitude;

            // Draw the line using a rotated rectangle (a thin box)
            Matrix4x4 matrixBackup = GUI.matrix; // Save the current GUI matrix
            GUIUtility.RotateAroundPivot(angle, pointA);
            GUI.Box(new Rect(pointA.x, pointA.y, length, 2), "");

            GUI.matrix = matrixBackup;        // Restore the original matrix
            GUI.color = previousColor;        // Restore the previous color
        }

        // Draw a line connecting two nodes, starting and ending at their edges
        void DrawEdgeLine(Vector2 node1Pos, Vector2 node2Pos, Color color)
        {
            // Calculate center positions of both nodes
            Vector2 center1 = node1Pos + nodeSize / 2;
            Vector2 center2 = node2Pos + nodeSize / 2;

            // Get the edge points for the line
            Vector2 start = GetEdgePoint(center1, nodeSize, center2);
            Vector2 end = GetEdgePoint(center2, nodeSize, center1);

            // Draw the line
            DrawLine(start, end, color);
        }

        // Calculate the point on the edge of a node rectangle closest to a target point
        Vector2 GetEdgePoint(Vector2 nodeCenter, Vector2 nodeSize, Vector2 target)
        {
            Vector2 direction = (target - nodeCenter).normalized;
            Vector2 halfSize = nodeSize / 2;

            // Calculate the edge intersection point
            float dx = halfSize.x / Mathf.Abs(direction.x);
            float dy = halfSize.y / Mathf.Abs(direction.y);
            float scale = Mathf.Min(dx, dy);

            return nodeCenter + direction * scale;
        }

    }
}
