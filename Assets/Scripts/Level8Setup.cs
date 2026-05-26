using UnityEngine;

public class Level8Setup : MonoBehaviour
{
    [Header("Level 8: One Way Out")]
    [Header("This script helps setup Level 8 layout")]
    [Header("Place this in the scene and assign references")]

    [SerializeField] private Transform playerStartPosition;
    [SerializeField] private Transform boxPosition;
    [SerializeField] private Transform boxPointPosition;
    [SerializeField] private Transform playerPointPosition;

    [Header("Grid Layout (for reference)")]
    [TextArea(5, 10)]
    [SerializeField] private string levelLayout = @"
Col:   0   1   2   3   4   5
Row 4: [W] [W] [W] [W] [W] [W]
Row 3: [W] [ ] [ ] [ ] [ ] [W]
Row 2: [W] [P] [ ] [B] [ ] [W]   <- P (Player), B (Box_RightOnly)
Row 1: [W] [ ] [ ] [ ] [ ] [W]
Row 0: [W] [ ] [BP][ ][PP][W]   <- BP (BoxPoint), PP (PlayerPoint)

SOLUTION:
1. Player ở bên trái Box, facing phải
2. Nhấn H để grab Box
3. Player đi TRÁI -> Box đi PHẢI (hướng duy nhất được phép)
4. Box lăn đến BoxPoint
5. Player thả Box, đi xuống PlayerPoint
6. WIN!
";

    [Header("Instructions")]
    [TextArea(3, 5)]
    [SerializeField] private string setupInstructions = @"
SETUP STEPS:
1. Duplicate Level_3.unity as Level_8.unity
2. Remove all Box objects
3. Place Box_RightOnly prefab at (3.5, 2.5, 0)
4. Place Player at (1.5, 2.5, 0)
5. Place BoxPoint at (2.5, 0.5, 0)
6. Place PlayerPoint at (4.5, 0.5, 0)
7. Remove extra walls to match layout above
8. Add Level_8 to Build Settings
";

    private void Awake()
    {
        Debug.Log("Level 8: One Way Out - Strategy Pattern Demo");
        Debug.Log("Box chỉ có thể di chuyển sang PHẢI khi bị kéo!");
    }
}
