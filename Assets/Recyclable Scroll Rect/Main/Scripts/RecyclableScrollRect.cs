//MIT License
//Copyright (c) 2020 Mohammed Iqubal Hussain
//Website : Polyandcode.com 

using System;
using UnityEngine;
using UnityEngine.UI;

namespace PolyAndCode.UI
{
    /// <summary>
    /// Entry for the recycling system. Extends Unity's inbuilt ScrollRect.
    /// </summary>
    public class RecyclableScrollRect : ScrollRect
    {
        
        [HideInInspector]
        public IRecyclableScrollRectDataSource DataSource;

        [SerializeField] private bool _isGrid;
        [SerializeField] private bool _isLoop;
        [SerializeField] private bool _isReverse;
        //Prototype cell can either be a prefab or present as a child to the content(will automatically be disabled in runtime)
        [SerializeField] private RectTransform _prototypeCell;
        //If true the intiziation happens at Start. Controller must assign the datasource in Awake.
        //Set to false if self init is not required and use public init API.
        [SerializeField] private bool _selfInitialize = true;

        public enum DirectionType
        {
            Vertical,
            Horizontal
        }
        [SerializeField] private DirectionType _direction;
        [SerializeField] private RectOffset _padding;
        [SerializeField] private Vector2 _spacing;

        //Segments : coloums for vertical and rows for horizontal.
        [SerializeField] private int _segments;

        private RecyclingSystem _recyclingSystem;
        private Vector2 _prevAnchoredPos;

        protected override void Start()
        {
            //defafult(built-in) in scroll rect can have both directions enabled, Recyclable scroll rect can be scrolled in only one direction.
            //setting default as vertical, Initialize() will set this again. 
            //vertical = true;
            //horizontal = false;

            if (!Application.isPlaying) return;

            if (_selfInitialize) Initialize();
        }

        /// <summary>
        /// Initialization when selfInitalize is true. Assumes that data source is set in controller's Awake.
        /// </summary>
        private void Initialize()
        {
            //Contruct the recycling system.
            switch (_direction)
            {
                case DirectionType.Vertical:
                    if (_isLoop)
                    {
                        _recyclingSystem = new LoopVerticalRecyclingSystem(_prototypeCell, viewport, content, _padding, _spacing, DataSource, _isGrid, _isReverse, _segments);

                    }
                    else
                    {
                        _recyclingSystem = new VerticalRecyclingSystem(_prototypeCell, viewport, content, _padding, _spacing, DataSource, _isGrid, _isReverse, _segments);
                    }
                    break;
                case DirectionType.Horizontal:
                    if (_isLoop)
                    {
                        _recyclingSystem = new LoopHorizontalRecyclingSystem(_prototypeCell, viewport, content, _padding, _spacing, DataSource, _isGrid, _isReverse, _segments);
                    }
                    else
                    {
                        _recyclingSystem = new HorizontalRecyclingSystem(_prototypeCell, viewport, content, _padding, _spacing, DataSource, _isGrid, _isReverse, _segments);
                    }
                    break;
            }

            vertical = _direction == DirectionType.Vertical;
            horizontal = _direction == DirectionType.Horizontal;

            _prevAnchoredPos = content.anchoredPosition;
            onValueChanged.RemoveListener(OnValueChangedListener);
            //Adding listener after pool creation to avoid any unwanted recycling behaviour.(rare scenerio)
            _recyclingSystem.Init(() => onValueChanged.AddListener(OnValueChangedListener));
        }

        /// <summary>
        /// public API for Initializing when datasource is not set in controller's Awake. Make sure selfInitalize is set to false. 
        /// </summary>
        public void Initialize(IRecyclableScrollRectDataSource dataSource)
        {
            DataSource = dataSource;
            Initialize();
        }
        public void Reseting()
        {
            StopMovement(); 
            _prevAnchoredPos = content.anchoredPosition;

            onValueChanged.RemoveListener(OnValueChangedListener);
            _recyclingSystem.Reset(()=> onValueChanged.AddListener(OnValueChangedListener));
        }
        public void Refreshing()
        {
            StopMovement();

            onValueChanged.RemoveListener(OnValueChangedListener);
            _recyclingSystem.Refresh(() => onValueChanged.AddListener(OnValueChangedListener));
            
        }

        /// <summary>
        /// Added as a listener to the OnValueChanged event of Scroll rect.
        /// Recycling entry point for recyling systems.
        /// </summary>
        /// <param name="direction">scroll direction</param>
        public void OnValueChangedListener(Vector2 normalizedPos)
        {
            Vector2 dir = content.anchoredPosition - _prevAnchoredPos;
            m_ContentStartPosition += _recyclingSystem.OnValueChangedListener(dir);
            _prevAnchoredPos = content.anchoredPosition;
        }

        /// <summary>
        ///Reloads the data. Call this if a new datasource is assigned.
        /// </summary>
        public void ReloadData()
        {
            ReloadData(DataSource);
        }

        /// <summary>
        /// Overloaded ReloadData with dataSource param
        ///Reloads the data. Call this if a new datasource is assigned.
        /// </summary>
        public void ReloadData(IRecyclableScrollRectDataSource dataSource)
        {
            if (_recyclingSystem != null)
            {
                StopMovement();
                onValueChanged.RemoveListener(OnValueChangedListener);
                _recyclingSystem.DataSource = dataSource;
                _recyclingSystem.Init(() => onValueChanged.AddListener(OnValueChangedListener));
                _prevAnchoredPos = content.anchoredPosition;
            }
        }
    }
}