﻿//MIT License
//Copyright (c) 2020 Mohammed Iqubal Hussain
//Website : Polyandcode.com 

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PolyAndCode.UI
{

    /// <summary>
    /// Recyling system for Vertical type.
    /// </summary>
    public class VerticalRecyclingSystem : RecyclingSystem
    {
        //Assigned by constructor
        private readonly int _coloumns;

        //Trackers
        private int _topMostCellIndex, _bottomMostCellIndex; //Topmost and bottommost cell in the heirarchy
        private int _TopMostCellColoumn, _BottomMostCellColoumn; // used for recyling in Grid layout. top-most and bottom-most coloumn
        private int _FirstCellColoumn, _LastCellColoumn; //used for loop recycling in grid layout.

        //Cached zero vector 
        private Vector2 zeroVector = Vector2.zero;

        #region INIT
        public VerticalRecyclingSystem(RectTransform prototypeCell, RectTransform viewport, RectTransform content, RectOffset padding, Vector2 spacing, IRecyclableScrollRectDataSource dataSource, bool isGrid,bool isReverse, int coloumns)
        {
            PrototypeCell = prototypeCell;
            Viewport = viewport;
            Content = content;
            Padding = padding;
            Spacing = spacing;
            DataSource = dataSource;
            IsGrid = isGrid;
            IsReverse = isReverse;

            _coloumns = isGrid ? coloumns : 1;
            _recyclableViewBounds = new Bounds();
        }
        public VerticalRecyclingSystem()
        {

        }

        /// <summary>
        /// Function for initiazation.
        /// </summary>
        /// <param name="onInitialized">callback when init done</param>
        /// <returns></returns>>
        public override void Init(System.Action onInitialized)
        {
            SetTopAnchor(Content);
            Content.anchoredPosition = Vector2.zero;
            SetRecyclingBounds();

            //Cell Poool
            CreateCellPool();

            _currentTopItem = (_cacheCellPool.Count > _itemCount ? _cacheCellPool.Count % _itemCount : _cacheCellPool.Count) - 1;
            _currentBottomItem = 0;

            _FirstCellColoumn = 0;
            _LastCellColoumn = (_itemCount % _coloumns) - 1;

            if (_LastCellColoumn == -1) _LastCellColoumn = _coloumns - 1;

            _topMostCellIndex = 0;
            _bottomMostCellIndex = _cacheCellPool.Count - 1;

            //Set content height according to no of rows
            int rows = Mathf.CeilToInt(((float)_itemCount) / _coloumns);

            float contentYSize = rows * _cellHeight + (rows - 1) * Spacing.y + Padding.top + Padding.bottom;
            Content.sizeDelta = new Vector2(Content.sizeDelta.x, contentYSize);
            SetTopAnchor(Content);

            onInitialized?.Invoke();
        }
        /// <summary>
        /// Function for reseting recycle view, when you have things like search bar and you wanna update view list, use this
        /// </summary>
        /// <param name="onReset"></param>
        public override void Reset(Action onReset = null)
        {
            SetTopAnchor(Content);
            Content.anchoredPosition = Vector3.zero;
            SetRecyclingBounds();

            //Cell Poool
            ResetCellPool();

            _currentTopItem = (_cacheCellPool.Count > _itemCount ? _cacheCellPool.Count % _itemCount : _cacheCellPool.Count) - 1;
            _currentBottomItem = 0;

            _FirstCellColoumn = 0;
            _LastCellColoumn = (_itemCount % _coloumns) - 1;

            if (_LastCellColoumn == -1) _LastCellColoumn = _coloumns - 1;

            _topMostCellIndex = 0;
            _bottomMostCellIndex = _cacheCellPool.Count - 1;

            //Set content height according to no of rows
            int rows = Mathf.CeilToInt(((float)_itemCount) / _coloumns);

            float contentYSize = rows * _cellHeight + (rows - 1) * Spacing.y + Padding.top + Padding.bottom;
            Content.sizeDelta = new Vector2(Content.sizeDelta.x, contentYSize);
            SetTopAnchor(Content);

            onReset?.Invoke();
        }
        /// <summary>
        /// Function for refreshing recycle view, when you wanna update some cell in view but list wasnt changed
        /// </summary>
        /// <param name="onRefresh"></param>
        public override void Refresh(Action onRefresh = null)
        {
            for (int i = 0; i < _cachedCells.Count; i++)
            {
                DataSource.RefreshCell(_cachedCells[i]);
            }

            onRefresh?.Invoke();
        }
        /// <summary>
        /// Function for scroll to item with index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="onGoTo"></param>
        public override Vector2 ScrollToItem(int index)
        {
            if (Content.rect.size.y <= Viewport.rect.size.y) return Vector2.zero;

            float fixedIndex = Mathf.Clamp(index, 0, _itemCount);
            int rows = Mathf.CeilToInt((fixedIndex + 1) / _coloumns);
            float contentYPos = (rows - 1) * (_cellHeight + Spacing.y) + Padding.top - Viewport.rect.size.y / 2 + _cellHeight / 2;

            return Vector2.up * Mathf.Clamp(contentYPos, 0f, Content.rect.size.y - Viewport.rect.size.y);
        }
        /// <summary>
        /// Sets the uppper and lower bounds for recycling cells.
        /// </summary>
        private void SetRecyclingBounds()
        {
            Viewport.GetWorldCorners(_corners);
            float threshHold = RecyclingThreshold * (_corners[2].y - _corners[0].y);
            _recyclableViewBounds.min = new Vector3(_corners[0].x, _corners[0].y - threshHold);
            _recyclableViewBounds.max = new Vector3(_corners[2].x, _corners[2].y + threshHold);
        }
        /// <summary>
        /// Creates cell Pool for recycling, Caches ICells
        /// </summary>
        private void CreateCellPool()
        {
            //Reseting Pool
            if (_cacheCellPool != null)
            {
                _totalCellPool.ForEach((RectTransform item) => UnityEngine.Object.Destroy(item.gameObject));
                _totalCellPool.Clear();
                _cacheCellPool.Clear();
                _cachedCells.Clear();
            }
            else
            {
                _cachedCells = new List<ICell>();
                _cacheCellPool = new List<RectTransform>();
                _totalCellPool = new List<RectTransform>();
            }

            //Set the prototype cell active and set cell anchor as top 
            PrototypeCell.gameObject.SetActive(true);
            if (IsGrid)
            {
                SetTopLeftAnchor(PrototypeCell);
            }
            else
            {
                SetTopAnchor(PrototypeCell);
            }

            //set new cell size according to its aspect ratio
            _cellWidth = (Content.rect.width - Padding.left - Padding.right - (_coloumns - 1) * Spacing.x) / _coloumns;
            _cellHeight = PrototypeCell.sizeDelta.y / PrototypeCell.sizeDelta.x * _cellWidth;

            _itemCount = DataSource.GetItemCount();
            //Reset
            _TopMostCellColoumn = _BottomMostCellColoumn = 0;

            //Get the required pool coverage and mininum size for the Cell pool
            //Temps
            float currentPoolCoverage = 0;
            int poolSize = 0;
            int poolItem = 0;
            float posX = 0;
            float posY = -Padding.top;

            //Get the required pool coverage and mininum size for the Cell pool
            float requriedCoverage = MinPoolCoverage * Viewport.rect.height;
            int minPoolSize;

            minPoolSize = Math.Min(MinPoolSize, _itemCount);

            //create cells untill the Pool area is covered and pool size is the minimum required
            while ((poolSize < minPoolSize || currentPoolCoverage < requriedCoverage) && poolItem < _itemCount)
            {
                //Instantiate and add to Pool
                RectTransform item = (UnityEngine.Object.Instantiate(PrototypeCell.gameObject)).GetComponent<RectTransform>();
                item.name = "Cell " + poolSize.ToString();
                item.sizeDelta = new Vector2(_cellWidth, _cellHeight);
                _totalCellPool.Add(item);
                _cacheCellPool.Add(item);
                item.SetParent(Content, false);

                if (IsGrid)
                {
                    posX = _BottomMostCellColoumn * _cellWidth + _BottomMostCellColoumn * Spacing.x + Padding.left;
                    item.anchoredPosition = new Vector2(posX, posY);
                    if (++_BottomMostCellColoumn >= _coloumns)
                    {
                        _BottomMostCellColoumn = 0;
                        posY -= _cellHeight + Spacing.y;
                        currentPoolCoverage += item.rect.height + Spacing.y;
                    }
                }
                else
                {
                    posX = (Padding.left - Padding.right) / 2;
                    item.anchoredPosition = new Vector2(posX, posY);
                    posY = item.anchoredPosition.y - item.rect.height - Spacing.y;
                    currentPoolCoverage += item.rect.height + Spacing.y;
                }

                //Setting data for Cell
                _cachedCells.Add(item.GetComponent<ICell>());
                DataSource.SetCell(_cachedCells[_cachedCells.Count - 1], poolItem);

                //Update the Pool size
                poolSize++;
                poolItem++;
            }

            //TODO : you alrady have a _currentColoumn varaiable. Why this calculation?????
            if (IsGrid)
            {
                _BottomMostCellColoumn = (_BottomMostCellColoumn - 1 + _coloumns) % _coloumns;
            }

            //Deactivate prototype cell if it is not a prefab(i.e it's present in scene)
            if (PrototypeCell.gameObject.scene.IsValid())
            {
                PrototypeCell.gameObject.SetActive(false);
            }
        }
        /// <summary>
        /// Reset cell pool for recycling, cached cells
        /// </summary>
        private void ResetCellPool()
        {
            _cacheCellPool.ForEach((RectTransform item) => item.gameObject.SetActive(false));
            _cacheCellPool.Clear();
            _cachedCells.Clear();

            //Set the prototype cell active and set cell anchor as top 
            PrototypeCell.gameObject.SetActive(true);

            //set new cell size according to its aspect ratio
            _cellWidth = (Content.rect.width - Padding.left - Padding.right - (_coloumns - 1) * Spacing.y) / _coloumns;
            _cellHeight = PrototypeCell.sizeDelta.y / PrototypeCell.sizeDelta.x * _cellWidth;

            _itemCount = DataSource.GetItemCount();
            //Reset
            _TopMostCellColoumn = _BottomMostCellColoumn = 0;

            //Get the required pool coverage and mininum size for the Cell pool
            //Temps
            float currentPoolCoverage = 0;
            int poolSize = 0;
            int poolItem = 0;
            float posX = 0;
            float posY = -Padding.top;

            //Get the required pool coverage and mininum size for the Cell pool
            float requriedCoverage = MinPoolCoverage * Viewport.rect.height;
            int minPoolSize;

            minPoolSize = Math.Min(MinPoolSize, _itemCount);

            //create cells untill the Pool area is covered and pool size is the minimum required
            while ((poolSize < minPoolSize || currentPoolCoverage < requriedCoverage) && poolItem < _itemCount)
            {
                //Instantiate and add to Pool
                RectTransform item;

                if (poolSize >= _totalCellPool.Count)
                {
                    item = (UnityEngine.Object.Instantiate(PrototypeCell.gameObject)).GetComponent<RectTransform>();
                    item.name = "Cell " + poolSize.ToString();
                    item.sizeDelta = new Vector2(_cellWidth, _cellHeight);
                    _totalCellPool.Add(item);
                    _cacheCellPool.Add(item);
                    item.SetParent(Content, false);
                }
                else
                {
                    item = _totalCellPool[poolSize];
                    item.gameObject.SetActive(true);
                    item.sizeDelta = new Vector2(_cellWidth, _cellHeight);
                    _cacheCellPool.Add(item);
                }

                if (IsGrid)
                {
                    posX = _BottomMostCellColoumn * _cellWidth + _BottomMostCellColoumn * Spacing.x + Padding.left;
                    item.anchoredPosition = new Vector2(posX, posY);
                    if (++_BottomMostCellColoumn >= _coloumns)
                    {
                        _BottomMostCellColoumn = 0;
                        posY -= _cellHeight + Spacing.y;
                        currentPoolCoverage += item.rect.height + Spacing.y;
                    }
                }
                else
                {
                    posX = (Padding.left - Padding.right) / 2;
                    item.anchoredPosition = new Vector2(posX, posY);
                    posY = item.anchoredPosition.y - item.rect.height - Spacing.y;
                    currentPoolCoverage += item.rect.height + Spacing.y;
                }

                //Setting data for Cell
                _cachedCells.Add(item.GetComponent<ICell>());
                DataSource.SetCell(_cachedCells[_cachedCells.Count - 1], poolItem);

                //Update the Pool size
                poolSize++;
                poolItem++;
            }

            //TODO : you alrady have a _currentColoumn varaiable. Why this calculation?????
            if (IsGrid)
            {
                _BottomMostCellColoumn = (_BottomMostCellColoumn - 1 + _coloumns) % _coloumns;
            }

            //Deactivate prototype cell if it is not a prefab(i.e it's present in scene)
            if (PrototypeCell.gameObject.scene.IsValid())
            {
                PrototypeCell.gameObject.SetActive(false);
            }
        }
        #endregion

        #region RECYCLING
            /// <summary>
            /// Recyling entry point
            /// </summary>
            /// <param name="direction">scroll direction </param>
            /// <returns></returns>
        public override Vector2 OnValueChangedListener(Vector2 direction)
        {
            if (_recycling || _cacheCellPool == null || _cacheCellPool.Count == 0) return zeroVector;

            //Updating Recyclable view bounds since it can change with resolution changes.
            SetRecyclingBounds();

            if (direction.y > 0 && _cacheCellPool[_bottomMostCellIndex].MaxY() > _recyclableViewBounds.min.y)
            {
                return RecycleTopToBottom();
            }
            else if (direction.y < 0 && _cacheCellPool[_topMostCellIndex].MinY() < _recyclableViewBounds.max.y)
            {
                return RecycleBottomToTop();
            }

            return zeroVector;
        }

        /// <summary>
        /// Recycles cells from top to bottom in the List heirarchy
        /// </summary>
        private Vector2 RecycleTopToBottom()
        {
            _recycling = true;

            float posY = IsGrid ? _cacheCellPool[_bottomMostCellIndex].anchoredPosition.y : 0;
            float posX = 0;

            //Recycle until cell at Top is avaiable and current item count smaller than datasource
            bool canScroll = _currentTopItem + 1 < _itemCount;

            while (_cacheCellPool[_topMostCellIndex].MinY() > _recyclableViewBounds.max.y && canScroll)
            {
                _currentTopItem++;
                _currentBottomItem++;

                canScroll = _currentTopItem + 1 < _itemCount;

                //Cell for row at
                DataSource.SetCell(_cachedCells[_topMostCellIndex], _currentTopItem);

                if (IsGrid)
                {
                    if (_currentTopItem == 0)
                    {
                        _BottomMostCellColoumn = _FirstCellColoumn;
                        posY = _cacheCellPool[_bottomMostCellIndex].anchoredPosition.y - _cellHeight - Spacing.y;
                    }
                    else if (++_BottomMostCellColoumn >= _coloumns)
                    {
                        _BottomMostCellColoumn = 0;
                        posY = _cacheCellPool[_bottomMostCellIndex].anchoredPosition.y - _cellHeight - Spacing.y;
                    }

                    //Move top cell to bottom
                    posX = _BottomMostCellColoumn * _cellWidth + _BottomMostCellColoumn * Spacing.x + Padding.left;
                    _cacheCellPool[_topMostCellIndex].anchoredPosition = new Vector2(posX, posY);

                    if (_currentBottomItem == 0)
                    {
                        _TopMostCellColoumn = _FirstCellColoumn;
                    }
                    else if (++_TopMostCellColoumn >= _coloumns)
                    {
                        _TopMostCellColoumn = 0;
                    }
                }
                else
                {
                    //Move top cell to bottom
                    posY = _cacheCellPool[_bottomMostCellIndex].anchoredPosition.y - _cacheCellPool[_bottomMostCellIndex].sizeDelta.y - Spacing.y;
                    _cacheCellPool[_topMostCellIndex].anchoredPosition = new Vector2(_cacheCellPool[_topMostCellIndex].anchoredPosition.x, posY);
                }

                //set new indices
                _bottomMostCellIndex = _topMostCellIndex;
                _topMostCellIndex++;
                if (_topMostCellIndex >= _cacheCellPool.Count) _topMostCellIndex = 0;
            }

            _recycling = false;
            return Vector2.zero;

        }

        /// <summary>
        /// Recycles cells from bottom to top in the List heirarchy
        /// </summary>
        private Vector2 RecycleBottomToTop()
        {
            _recycling = true;

            float posY = IsGrid ? _cacheCellPool[_topMostCellIndex].anchoredPosition.y : 0;
            float posX = 0;

            //Recycle until cell at bottom is avaiable and current item count is greater than cellpool size
            bool canScroll = _currentBottomItem > 0;

            while (_cacheCellPool[_bottomMostCellIndex].MaxY() < _recyclableViewBounds.min.y && canScroll)
            {
                _currentTopItem--;
                _currentBottomItem--;

                canScroll = _currentBottomItem > 0;

                //Cell for row at
                DataSource.SetCell(_cachedCells[_bottomMostCellIndex], _currentBottomItem);

                if (IsGrid)
                {
                    if(_currentBottomItem == _itemCount - 1)
                    {
                        _TopMostCellColoumn = _LastCellColoumn;
                        posY = _cacheCellPool[_topMostCellIndex].anchoredPosition.y + _cellHeight + Spacing.y;
                    }
                    else if (--_TopMostCellColoumn < 0)
                    {
                        _TopMostCellColoumn = _coloumns - 1;
                        posY = _cacheCellPool[_topMostCellIndex].anchoredPosition.y + _cellHeight + Spacing.y;
                    }

                    //Move bottom cell to top
                    posX = _TopMostCellColoumn * _cellWidth + _TopMostCellColoumn * Spacing.x + Padding.left;
                    _cacheCellPool[_bottomMostCellIndex].anchoredPosition = new Vector2(posX, posY);

                    if(_currentTopItem == _itemCount - 1)
                    {
                        _BottomMostCellColoumn = _LastCellColoumn;
                    }
                    else if (--_BottomMostCellColoumn < 0)
                    {
                        _BottomMostCellColoumn = _coloumns - 1;
                    }
                }
                else
                {
                    //Move bottom cell to top
                    posY = _cacheCellPool[_topMostCellIndex].anchoredPosition.y + _cacheCellPool[_topMostCellIndex].sizeDelta.y + Spacing.y;
                    _cacheCellPool[_bottomMostCellIndex].anchoredPosition = new Vector2(_cacheCellPool[_bottomMostCellIndex].anchoredPosition.x, posY);
                }

                //set new indices
                _topMostCellIndex = _bottomMostCellIndex;
                _bottomMostCellIndex--;
                if (_bottomMostCellIndex < 0) _bottomMostCellIndex = _cacheCellPool.Count - 1;
            }

            _recycling = false;
            return Vector2.zero;
        }
        #endregion

        #region  HELPERS
        /// <summary>
        /// Anchoring cell and content rect transforms to top preset. Makes repositioning easy.
        /// </summary>
        /// <param name="rectTransform"></param>
        private void SetTopAnchor(RectTransform rectTransform)
        {
            //Saving to reapply after anchoring. Width and height changes if anchoring is change. 
            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;

            Vector2 pos = IsGrid ? new Vector2(0, 1) : new Vector2(0.5f, 1);

            //Setting top anchor 
            rectTransform.anchorMin = pos;
            rectTransform.anchorMax = pos;
            rectTransform.pivot = pos;

            //Reapply size
            rectTransform.sizeDelta = new Vector2(width, height);
        }

        private void SetTopLeftAnchor(RectTransform rectTransform)
        {
            //Saving to reapply after anchoring. Width and height changes if anchoring is change. 
            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;

            //Setting top anchor 
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);

            //Reapply size
            rectTransform.sizeDelta = new Vector2(width, height);
        }
        #endregion

        #region TESTING
        public override void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(_recyclableViewBounds.min - new Vector3(2000, 0), _recyclableViewBounds.min + new Vector3(2000, 0));
            Gizmos.color = Color.red;
            Gizmos.DrawLine(_recyclableViewBounds.max - new Vector3(2000, 0), _recyclableViewBounds.max + new Vector3(2000, 0));
        }
        #endregion

    }
}
