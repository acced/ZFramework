using System.Collections.Generic;
using SkillEditor.Data;
using TBSF.Core;
using TBSF.Grid;
using UnityEngine;

namespace SkillEditor.Runtime
{
    /// <summary>
    /// 搜索目标任务Spec
    /// 支持 Physics2D 碰撞检测和六边形网格搜索两种模式
    /// </summary>
    public class SearchTargetTaskSpec : TaskSpec
    {
        private List<AbilitySystemComponent> _foundTargets = new List<AbilitySystemComponent>();
        private SearchTargetTaskNodeData SearchNodeData => NodeData as SearchTargetTaskNodeData;

        private static readonly Collider2D[] s_ColliderBuffer = new Collider2D[64];

        public static bool DebugDraw = true;
        public static float DebugDrawDuration = 2f;
        public static Color DebugDrawColor = Color.green;

        /// <summary>
        /// 六边形网格提供者 (外部注入, 用于六边形搜索)
        /// </summary>
        public static IHexGridProvider HexGridProvider { get; set; }

        /// <summary>
        /// 六边形搜索后的受影响格子 (供预览/提示系统读取)
        /// </summary>
        public List<HexCoordinates> LastAffectedHexCells { get; private set; } = new List<HexCoordinates>();

#if UNITY_EDITOR
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void DomainReloadReset()
        {
            DebugDraw = true;
            DebugDrawDuration = 2f;
            DebugDrawColor = Color.green;
            HexGridProvider = null;
        }
#endif

        protected override void OnExecute(AbilitySystemComponent target)
        {
            _foundTargets.Clear();
            LastAffectedHexCells.Clear();

            var nodeData = SearchNodeData;
            if (nodeData == null)
            {
                SpecExecutor.ExecuteConnectedNodes(SkillId, NodeGuid, "无目标", GetExecutionContext());
                return;
            }

            bool isHexSearch = nodeData.searchShapeType == SearchShapeType.HexArea
                            || nodeData.searchShapeType == SearchShapeType.HexLine
                            || nodeData.searchShapeType == SearchShapeType.HexRing;

            if (isHexSearch)
            {
                ExecuteHexSearch(nodeData);
            }
            else
            {
                ExecutePhysicsSearch(nodeData);
            }

            if (nodeData.maxTargets > 0 && _foundTargets.Count > nodeData.maxTargets)
                _foundTargets.RemoveRange(nodeData.maxTargets, _foundTargets.Count - nodeData.maxTargets);

            var ctx = GetExecutionContext();
            if (_foundTargets.Count == 0)
            {
                SpecExecutor.ExecuteConnectedNodes(SkillId, NodeGuid, "无目标", ctx);
            }
            else
            {
                foreach (var findTarget in _foundTargets)
                {
                    var targetCtx = ctx.CreateWithParentInput(findTarget);
                    SpecExecutor.ExecuteConnectedNodes(SkillId, NodeGuid, "对每个目标", targetCtx);
                }
            }

            SpecExecutor.ExecuteConnectedNodes(SkillId, NodeGuid, "完成效果", ctx);
        }

        // ============ 六边形搜索 ============

        private void ExecuteHexSearch(SearchTargetTaskNodeData nodeData)
        {
            if (HexGridProvider == null) return;

            Vector3 centerPos = Context.GetPosition(nodeData.positionSource, nodeData.positionBindingName);
            var centerCoord = HexCoordinates.FromWorldPosition(centerPos);
            var centerCell = HexGridProvider.GetCell(centerCoord);

            List<HexCell> cells = null;
            switch (nodeData.searchShapeType)
            {
                case SearchShapeType.HexArea:
                    cells = HexGridProvider.GetCellsInRange(centerCoord, nodeData.hexRadius);
                    break;
                case SearchShapeType.HexRing:
                    cells = HexGridProvider.GetCellsOnRing(centerCoord, nodeData.hexRadius);
                    break;
                case SearchShapeType.HexLine:
                    var dir = (HexDirection)Mathf.Clamp(nodeData.hexDirectionIndex, 0, 5);
                    var endCoord = HexUtils.Step(centerCoord, dir, nodeData.hexLineLength);
                    cells = HexGridProvider.GetCellsOnLine(centerCoord, endCoord);
                    break;
            }

            if (cells == null) return;

            foreach (var cell in cells)
            {
                if (!nodeData.hexIncludeSelf && cell.Coordinates == centerCoord)
                    continue;

                if (nodeData.hexCheckHeight && centerCell != null)
                {
                    float heightDiff = Mathf.Abs(cell.Height - centerCell.Height);
                    if (heightDiff > nodeData.hexMaxHeightDiff) continue;
                }

                LastAffectedHexCells.Add(cell.Coordinates);

                if (cell.OccupyingUnitId != 0)
                {
                    var asc = FindASCByUnitId(cell.OccupyingUnitId);
                    if (asc != null && IsValidTarget(asc))
                        _foundTargets.Add(asc);
                }
            }
        }

        private AbilitySystemComponent FindASCByUnitId(int unitId)
        {
            if (GASBridge.UnitBinder == null) return null;

            var owner = GASBridge.UnitBinder.FindOwnerByUnitId(unitId);
            if (owner == null) return null;

            return GASHost.GetASC(owner);
        }

        // ============ Physics2D 搜索 (原有逻辑) ============

        private void ExecutePhysicsSearch(SearchTargetTaskNodeData nodeData)
        {
            Vector2 centerPosition = Context.GetPosition(nodeData.positionSource, nodeData.positionBindingName);
            GameObject sourceObject = Context.GetSourceObject(nodeData.positionSource);
            Transform centerTransform = sourceObject?.transform;

            switch (nodeData.searchShapeType)
            {
                case SearchShapeType.Circle:
                    SearchCircle(centerPosition, nodeData.searchCircleRadius);
                    DebugDrawCircle(centerPosition, nodeData.searchCircleRadius);
                    break;
                case SearchShapeType.Sector:
                    if (centerTransform != null)
                    {
                        var sectorForward = GetFacingDirection(centerTransform);
                        SearchSector(centerPosition, centerTransform, nodeData.searchSectorRadius, nodeData.searchSectorAngle);
                        DebugDrawSector(centerPosition, sectorForward, nodeData.searchSectorRadius, nodeData.searchSectorAngle);
                    }
                    break;
                case SearchShapeType.Line:
                    SearchLine(centerPosition, centerTransform);
                    break;
            }
        }

        /// <summary>
        /// 圆形范围检测 - 使用Physics2D.OverlapCircleAll
        /// </summary>
        private void SearchCircle(Vector2 center, float radius)
        {
            int count = Physics2D.OverlapCircleNonAlloc(center, radius, s_ColliderBuffer);
            for (int i = 0; i < count; i++)
            {
                var asc = GetASCFromCollider(s_ColliderBuffer[i]);
                if (asc != null && IsValidTarget(asc))
                {
                    _foundTargets.Add(asc);
                }
            }
        }

        /// <summary>
        /// 扇形范围检测 - 先用圆形检测，再过滤角度
        /// </summary>
        private void SearchSector(Vector2 center, Transform casterTransform, float radius, float angle)
        {
            float halfAngle = angle * 0.5f;

            // 获取角色朝向（角色默认朝左，所以使用 -transform.right）
            Vector2 forward = GetFacingDirection(casterTransform);

            int count = Physics2D.OverlapCircleNonAlloc(center, radius, s_ColliderBuffer);
            for (int i = 0; i < count; i++)
            {
                var collider = s_ColliderBuffer[i];
                var asc = GetASCFromCollider(collider);
                if (asc == null || !IsValidTarget(asc)) continue;

                Vector2 toTarget = (Vector2)collider.transform.position - center;

                if (toTarget.sqrMagnitude < 0.001f) continue;

                float angleToTarget = Vector2.Angle(forward, toTarget);

                if (angleToTarget <= halfAngle)
                {
                    _foundTargets.Add(asc);
                }
            }
        }

        /// <summary>
        /// 直线/矩形范围检测 - 使用Physics2D.OverlapBoxAll
        /// </summary>
        private void SearchLine(Vector2 center, Transform casterTransform)
        {
            var nodeData = SearchNodeData;
            if (nodeData == null) return;

            Vector2 direction;
            float width, length;
            Vector2 startPos = center;

            // 获取角色朝向（角色默认朝左，所以使用 -transform.right）
            Vector2 baseForward = casterTransform != null ? GetFacingDirection(casterTransform) : Vector2.right;

            switch (nodeData.searchLineType)
            {
                case LineType.UnitDirection:
                    direction = RotateVector2(baseForward, nodeData.searchLineDirectionOffsetAngle);
                    width = nodeData.searchLineDirectionWidth;
                    length = nodeData.searchLineDirectionLength;
                    break;
                case LineType.BetweenPoints:
                    // 使用 PositionSourceType 获取起点和终点位置
                    startPos = Context.GetPosition(nodeData.lineStartPositionSource, nodeData.lineStartBindingName);
                    Vector2 endPos = Context.GetPosition(nodeData.lineEndPositionSource, nodeData.lineEndBindingName);
                    direction = (endPos - startPos).normalized;
                    width = nodeData.searchLineBetweenWidth;
                    length = Vector2.Distance(startPos, endPos);
                    break;
                default:
                    direction = RotateVector2(Vector2.right, nodeData.searchLineAbsoluteAngle);
                    width = nodeData.searchLineAbsoluteWidth;
                    length = nodeData.searchLineAbsoluteLength;
                    break;
            }

            // 计算Box的中心点和尺寸
            Vector2 boxCenter = startPos + direction * (length * 0.5f);
            Vector2 boxSize = new Vector2(length, width);
            float boxAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // 调试绘制
            DebugDrawBox(boxCenter, boxSize, boxAngle);

            int count = Physics2D.OverlapBoxNonAlloc(boxCenter, boxSize, boxAngle, s_ColliderBuffer);
            for (int i = 0; i < count; i++)
            {
                var asc = GetASCFromCollider(s_ColliderBuffer[i]);
                if (asc != null && IsValidTarget(asc))
                {
                    _foundTargets.Add(asc);
                }
            }
        }

        private AbilitySystemComponent GetASCFromCollider(Collider2D collider)
        {
            if (collider == null || GASBridge.UnitBinder == null) return null;

            var owner = GASBridge.UnitBinder.GetOwnerFromCollider(collider);
            if (owner == null) return null;

            return GASHost.GetASC(owner);
        }

        /// <summary>
        /// 旋转2D向量
        /// </summary>
        private Vector2 RotateVector2(Vector2 v, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        /// <summary>
        /// 获取角色朝向（角色默认朝左）
        /// scale.x >= 0 时朝左，scale.x < 0 时朝右
        /// </summary>
        private Vector2 GetFacingDirection(Transform casterTransform)
        {
            // 角色默认朝左，所以：
            // scale.x >= 0（默认）-> 朝左 -> Vector2.left
            // scale.x < 0（翻转）-> 朝右 -> Vector2.right
            return casterTransform.localScale.x >= 0 ? Vector2.left : Vector2.right;
        }

        private bool IsValidTarget(AbilitySystemComponent target)
        {
            if (target == null) return false;
            if (target == GetTarget()) return false;
            var nodeData = SearchNodeData;
            if (nodeData == null) return false;
            if (!nodeData.searchTargetTags.IsEmpty && !target.HasAnyTags(nodeData.searchTargetTags)) return false;
            if (!nodeData.searchExcludeTags.IsEmpty && target.HasAnyTags(nodeData.searchExcludeTags)) return false;
            return true;
        }

        #region 调试绘制

        private static readonly Vector2[] s_BoxLocalCorners = new Vector2[4];
        private static readonly Vector3[] s_BoxWorldCorners = new Vector3[4];

        private void DebugDrawCircle(Vector2 center, float radius)
        {
            if (!DebugDraw) return;

            const int segments = 32;
            const float angleStep = 360f / segments;
            Vector3 prev = new Vector3(center.x + radius, center.y, 0);

            for (int i = 1; i <= segments; i++)
            {
                float rad = i * angleStep * Mathf.Deg2Rad;
                Vector3 next = new Vector3(
                    center.x + Mathf.Cos(rad) * radius,
                    center.y + Mathf.Sin(rad) * radius, 0);
                Debug.DrawLine(prev, next, DebugDrawColor, DebugDrawDuration);
                prev = next;
            }
        }

        private void DebugDrawSector(Vector2 center, Vector2 forward, float radius, float angle)
        {
            if (!DebugDraw) return;

            float halfAngle = angle * 0.5f;
            int arcSegments = Mathf.Max(8, (int)(angle / 10f));
            float step = angle / arcSegments;

            float forwardAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
            float startAngle = forwardAngle - halfAngle;

            Vector3 center3D = new Vector3(center.x, center.y, 0);

            float leftRad = (forwardAngle - halfAngle) * Mathf.Deg2Rad;
            float rightRad = (forwardAngle + halfAngle) * Mathf.Deg2Rad;
            Vector3 leftEdge = center3D + new Vector3(Mathf.Cos(leftRad) * radius, Mathf.Sin(leftRad) * radius, 0);
            Vector3 rightEdge = center3D + new Vector3(Mathf.Cos(rightRad) * radius, Mathf.Sin(rightRad) * radius, 0);

            Debug.DrawLine(center3D, leftEdge, DebugDrawColor, DebugDrawDuration);
            Debug.DrawLine(center3D, rightEdge, DebugDrawColor, DebugDrawDuration);

            float a1 = startAngle * Mathf.Deg2Rad;
            Vector3 prev = center3D + new Vector3(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius, 0);
            for (int i = 1; i <= arcSegments; i++)
            {
                float a2 = (startAngle + i * step) * Mathf.Deg2Rad;
                Vector3 next = center3D + new Vector3(Mathf.Cos(a2) * radius, Mathf.Sin(a2) * radius, 0);
                Debug.DrawLine(prev, next, DebugDrawColor, DebugDrawDuration);
                prev = next;
            }
        }

        private void DebugDrawBox(Vector2 center, Vector2 size, float angle)
        {
            if (!DebugDraw) return;

            float rad = angle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            float hw = size.x * 0.5f;
            float hh = size.y * 0.5f;

            s_BoxLocalCorners[0] = new Vector2(-hw, -hh);
            s_BoxLocalCorners[1] = new Vector2(hw, -hh);
            s_BoxLocalCorners[2] = new Vector2(hw, hh);
            s_BoxLocalCorners[3] = new Vector2(-hw, hh);

            for (int i = 0; i < 4; i++)
            {
                float rx = s_BoxLocalCorners[i].x * cos - s_BoxLocalCorners[i].y * sin;
                float ry = s_BoxLocalCorners[i].x * sin + s_BoxLocalCorners[i].y * cos;
                s_BoxWorldCorners[i] = new Vector3(center.x + rx, center.y + ry, 0);
            }

            Debug.DrawLine(s_BoxWorldCorners[0], s_BoxWorldCorners[1], DebugDrawColor, DebugDrawDuration);
            Debug.DrawLine(s_BoxWorldCorners[1], s_BoxWorldCorners[2], DebugDrawColor, DebugDrawDuration);
            Debug.DrawLine(s_BoxWorldCorners[2], s_BoxWorldCorners[3], DebugDrawColor, DebugDrawDuration);
            Debug.DrawLine(s_BoxWorldCorners[3], s_BoxWorldCorners[0], DebugDrawColor, DebugDrawDuration);
        }

        #endregion
    }
}
