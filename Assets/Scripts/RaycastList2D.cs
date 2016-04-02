using UnityEngine;

public class RaycastHits2D : PreAllocatedListBase<RaycastHit2D> {
    public RaycastHits2D():base(){ }
    public RaycastHits2D(int capacity) : base(capacity) { }

    public void BoxCast(
        Vector2 origin, Vector2 size, float angle, Vector2 direction,
        float distance = float.PositiveInfinity,
        int layerMask = Physics2D.DefaultRaycastLayers,
        float minDepth = float.NegativeInfinity,
        float maxDepth = float.PositiveInfinity) {
        Count = Physics2D.BoxCastNonAlloc(origin, size, angle, direction, Results, distance, layerMask, minDepth, maxDepth);
    }
    
    public void CircleCast(
        Vector2 origin, float radius, Vector2 direction,
        float distance = float.PositiveInfinity,
        int layerMask = Physics2D.DefaultRaycastLayers,
        float minDepth = float.NegativeInfinity,
        float maxDepth = float.PositiveInfinity) {
        Count = Physics2D.CircleCastNonAlloc(origin, radius, direction, Results, distance, layerMask, minDepth, maxDepth);
    }
    
    public void Linecast(Vector2 start, Vector2 end,
        int layerMask = Physics2D.DefaultRaycastLayers,
        float minDepth = float.NegativeInfinity,
        float maxDepth = float.PositiveInfinity) {
        Count = Physics2D.LinecastNonAlloc(start, end, Results, layerMask, minDepth, maxDepth);
    }

    public void Raycast(Vector2 origin, Vector2 direction,
        float distance = float.PositiveInfinity,
        int layerMask = Physics2D.DefaultRaycastLayers,
        float minDepth = float.NegativeInfinity,
        float maxDepth = float.PositiveInfinity) {
        Count = Physics2D.RaycastNonAlloc(origin, direction, Results, distance, layerMask, minDepth, maxDepth);
    }

    public void RayIntersection(
        Ray ray,
        float distance = float.PositiveInfinity,
        int layerMask = Physics2D.DefaultRaycastLayers) {
        Count = Physics2D.GetRayIntersectionNonAlloc(ray, Results, distance, layerMask);
    }
}
