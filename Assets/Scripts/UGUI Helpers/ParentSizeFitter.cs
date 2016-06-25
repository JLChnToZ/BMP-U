using System;
using UnityEngine;
using UnityEngine.UI;

namespace JLChnToZ.Toolset.UI {
    [AddComponentMenu("UGUI/Layout/ParentSizeFitter", 15)]
    public class ParentSizeFitter: ContentSizeFitter {
        [NonSerialized]
        RectTransform _rectTransform;
        RectTransform rectTransform {
            get {
                if(_rectTransform == null)
                    _rectTransform = GetComponent<RectTransform>();
                return _rectTransform;
            }
        }

        [SerializeField]
        RectTransform target;
        public RectTransform Target {
            get { return target; }
            set {
                if(!target.Equals(value)) {
                    target = value;
                    SetDirty(true);
                }
            }
        }

        public new FitMode horizontalFit {
            get { return m_HorizontalFit; }
            set {
                if(!m_HorizontalFit.Equals(value)) {
                    m_HorizontalFit = value;
                    SetDirty(true);
                }
            }
        }

        public new FitMode verticalFit {
            get { return m_VerticalFit; }
            set {
                if(!m_VerticalFit.Equals(value)) {
                    m_VerticalFit = value;
                    SetDirty(true);
                }
            }
        }

        DrivenRectTransformTracker tracker;

        protected override void OnEnable() {
            base.OnEnable();
            SetDirty(false);
        }

        protected override void OnDisable() {
            tracker.Clear();
            if(target != null) LayoutRebuilder.MarkLayoutForRebuild(target);
            base.OnDisable();
        }

        void HandleSelfFittingAlongAxis(int axis) {
            FitMode fitting = (axis == 0 ? horizontalFit : verticalFit);
            if(fitting == FitMode.Unconstrained) return;

            tracker.Add(this, target,
                axis == 0 ? DrivenTransformProperties.SizeDeltaX :
                DrivenTransformProperties.SizeDeltaY
            );

            if(fitting == FitMode.MinSize)
                target.SetSizeWithCurrentAnchors(
                    (RectTransform.Axis)axis,
                    LayoutUtility.GetMinSize(rectTransform, axis)
                );
            else
                target.SetSizeWithCurrentAnchors(
                    (RectTransform.Axis)axis,
                    LayoutUtility.GetPreferredSize(rectTransform, axis)
                );
        }

        public void SetDirty(bool callBase) {
            if(!IsActive()) return;
            if(target != null) LayoutRebuilder.MarkLayoutForRebuild(target);
            if(callBase) SetDirty();
        }

        public override void SetLayoutHorizontal() {
            tracker.Clear();
            HandleSelfFittingAlongAxis(0);
        }

        public override void SetLayoutVertical() {
            HandleSelfFittingAlongAxis(1);
        }

        protected override void OnRectTransformDimensionsChange() {
            SetDirty(true);
        }

#if UNITY_EDITOR
        protected override void OnValidate() {
            SetDirty(true);
        }
#endif
    }
}