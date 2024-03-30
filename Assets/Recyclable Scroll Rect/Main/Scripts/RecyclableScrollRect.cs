﻿//MIT License
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

        public bool IsGrid;
        public bool IsLoop;
        public bool IsReverse;
        //Prototype cell can either be a prefab or present as a child to the content(will automatically be disabled in runtime)
        public RectTransform PrototypeCell;
        //If true the intiziation happens at Start. Controller must assign the datasource in Awake.
        //Set to false if self init is not required and use public init API.
        public bool SelfInitialize = true;

        public enum DirectionType
        {
            Vertical,
            Horizontal
        }

        public DirectionType Direction;

        public RectOffset Padding;
        public Vector2 Spacing;

        //Segments : coloums for vertical and rows for horizontal.
        public int Segments
        {
            set
            {
                _segments = Math.Max(value, 2);
            }
            get
            {
                return _segments;
            }
        }
        [SerializeField]
        private int _segments;

        private RecyclingSystem _recyclingSystem;
        private Vector2 _prevAnchoredPos;

        protected override void Start()
        {
            //defafult(built-in) in scroll rect can have both directions enabled, Recyclable scroll rect can be scrolled in only one direction.
            //setting default as vertical, Initialize() will set this again. 
            //vertical = true;
            //horizontal = false;

            if (!Application.isPlaying) return;

            if (SelfInitialize) Initialize();
        }

        /// <summary>
        /// Initialization when selfInitalize is true. Assumes that data source is set in controller's Awake.
        /// </summary>
        private void Initialize()
        {
            //Contruct the recycling system.
            switch (Direction)
            {
                case DirectionType.Vertical:
                    if (IsLoop)
                    {
                        _recyclingSystem = new LoopVerticalRecyclingSystem(PrototypeCell, viewport, content, Padding, Spacing, DataSource, IsGrid, IsReverse, Segments);

                    }
                    else
                    {
                        _recyclingSystem = new VerticalRecyclingSystem(PrototypeCell, viewport, content, Padding, Spacing, DataSource, IsGrid, IsReverse, Segments);
                    }
                    break;
                case DirectionType.Horizontal:
                    if (IsLoop)
                    {
                        _recyclingSystem = new LoopHorizontalRecyclingSystem(PrototypeCell, viewport, content, Padding, Spacing, DataSource, IsGrid, IsReverse, Segments);
                    }
                    else
                    {
                        _recyclingSystem = new HorizontalRecyclingSystem(PrototypeCell, viewport, content, Padding, Spacing, DataSource, IsGrid, IsReverse, Segments);
                    }
                    break;
            }

            vertical = Direction == DirectionType.Vertical;
            horizontal = Direction == DirectionType.Horizontal;

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