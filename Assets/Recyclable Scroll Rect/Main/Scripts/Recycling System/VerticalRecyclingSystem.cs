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
        private int _itemCount;
        private int _currentTopItem, _currentBottomItem;
        private int _topMostCellIndex, _bottomMostCellIndex; //Topmost and bottommost cell in the heirarchy
        private int _TopMostCellColoumn, _BottomMostCellColoumn; // used for recyling in Grid layout. top-most and bottom-most coloumn
        private int _FirstCellColoumn, _LastCellColoumn; //used for loop recycling in grid layout.

        //Cached zero vector 
        private Vector2 zeroVector = Vector2.zero;

        #region INIT
        public VerticalRecyclingSystem(RectTransform prototypeCell, RectTransform viewport, RectTransform content, RectOffset padding, float spacing, IRecyclableScrollRectDataSource dataSource, bool isGrid,bool isLoop,bool isReverse, int coloumns)
        {
            PrototypeCell = prototypeCell;
            Viewport = viewport;
            Content = content;
            Padding = padding;
            Spacing = spacing;
            DataSource = dataSource;
            IsGrid = isGrid;
            IsLoop = isLoop;
            IsReverse = isReverse;

            _coloumns = isGrid ? coloumns : 1;
            _recyclableViewBounds = new Bounds();
        }

        /// <summary>
        /// Corotuine for initiazation.
        /// Using coroutine for init because few UI stuff requires a frame to update
        /// </summary>
        /// <param name="onInitialized">callback when init done</param>
        /// <returns></returns>>
        public override void Init(System.Action onInitialized)
        {
            SetTopAnchor(Content);
            Content.anchoredPosition = Vector3.zero;
            SetRecyclingBounds();

            //Cell Poool
            CreateCellPool();

            _currentTopItem = (_cellPool.Count > _itemCount ? _cellPool.Count % _itemCount : _cellPool.Count) - 1;
            _currentBottomItem = 0;

            _FirstCellColoumn = 0;
            _LastCellColoumn = (_itemCount % _coloumns) - 1;

            if (_LastCellColoumn == -1) _LastCellColoumn = _coloumns - 1;

            _topMostCellIndex = 0;
            _bottomMostCellIndex = _cellPool.Count - 1;

            //Set content height according to no of rows
            int rows;

            if (IsLoop && IsGrid && _itemCount < _cellPool.Count)
            {
                int rowPerCluster = Mathf.CeilToInt((float)_itemCount / _coloumns);

                int residual = _cellPool.Count % _itemCount;

                int row1 = Mathf.CeilToInt(((float)(_cellPool.Count - residual) * rowPerCluster) / _itemCount);
                int row2 = Mathf.CeilToInt((float)residual / _coloumns);

                rows = row1 + row2;
            }
            else
            {
                rows = Mathf.CeilToInt(((float)_cellPool.Count) / _coloumns);
            }

            float contentYSize = rows * _cellHeight + (rows - 1) * Spacing + Padding.top + Padding.bottom;
            Content.sizeDelta = new Vector2(Content.sizeDelta.x, contentYSize);
            SetTopAnchor(Content);

            if (onInitialized != null) onInitialized();
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
            if (_cellPool != null)
            {
                _cellPool.ForEach((RectTransform item) => UnityEngine.Object.Destroy(item.gameObject));
                _cellPool.Clear();
                _cachedCells.Clear();
            }
            else
            {
                _cachedCells = new List<ICell>();
                _cellPool = new List<RectTransform>();
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
            _cellWidth = (Content.rect.width - Padding.left - Padding.right - (_coloumns - 1) * Spacing) / _coloumns;
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
            bool isReset;
            int minPoolSize;

            if (IsLoop)
            {
                minPoolSize = IsGrid ? MinPoolSize * _coloumns : MinPoolSize;

                //create cells untill the Pool area is covered and pool size is the minimum required
                while (poolSize < minPoolSize && currentPoolCoverage < requriedCoverage)
                {
                    //Instantiate and add to Pool
                    RectTransform item = (UnityEngine.Object.Instantiate(PrototypeCell.gameObject)).GetComponent<RectTransform>();
                    item.name = "Cell " + poolSize.ToString();
                    item.sizeDelta = new Vector2(_cellWidth, _cellHeight);
                    _cellPool.Add(item);
                    item.SetParent(Content, false);

                    isReset = poolItem + 1 >= _itemCount;

                    if (IsGrid)
                    {
                        posX = _BottomMostCellColoumn * _cellWidth + _BottomMostCellColoumn * Spacing + Padding.left;
                        item.anchoredPosition = new Vector2(posX, posY);
                        if (++_BottomMostCellColoumn >= _coloumns || poolItem + 1 >= _itemCount)
                        {
                            _BottomMostCellColoumn = _FirstCellColoumn;
                            posY -= _cellHeight + Spacing;
                            currentPoolCoverage += item.rect.height + Spacing;
                        }
                    }
                    else
                    {
                        posX = (Padding.left - Padding.right) / 2;
                        item.anchoredPosition = new Vector2(posX, posY);
                        posY = item.anchoredPosition.y - item.rect.height - Spacing;
                        currentPoolCoverage += item.rect.height + Spacing;
                    }

                    //Setting data for Cell
                    _cachedCells.Add(item.GetComponent<ICell>());
                    DataSource.SetCell(_cachedCells[_cachedCells.Count - 1], poolItem);

                    //Update the Pool size
                    poolSize++;
                    poolItem++;

                    if (poolItem >= _itemCount) poolItem = 0;
                }
            }
            else
            {
                minPoolSize = Math.Min(MinPoolSize, _itemCount);

                //create cells untill the Pool area is covered and pool size is the minimum required
                while ((poolSize < minPoolSize || currentPoolCoverage < requriedCoverage) && poolItem < _itemCount)
                {
                    //Instantiate and add to Pool
                    RectTransform item = (UnityEngine.Object.Instantiate(PrototypeCell.gameObject)).GetComponent<RectTransform>();
                    item.name = "Cell " + poolSize.ToString();
                    item.sizeDelta = new Vector2(_cellWidth, _cellHeight);
                    _cellPool.Add(item);
                    item.SetParent(Content, false);

                    if (IsGrid)
                    {
                        posX = _BottomMostCellColoumn * _cellWidth + _BottomMostCellColoumn * Spacing + Padding.left;
                        item.anchoredPosition = new Vector2(posX, posY);
                        if (++_BottomMostCellColoumn >= _coloumns)
                        {
                            _BottomMostCellColoumn = 0;
                            posY -= _cellHeight + Spacing;
                            currentPoolCoverage += item.rect.height + Spacing;
                        }
                    }
                    else
                    {
                        posX = (Padding.left - Padding.right) / 2;
                        item.anchoredPosition = new Vector2(posX, posY);
                        posY = item.anchoredPosition.y - item.rect.height - Spacing;
                        currentPoolCoverage += item.rect.height + Spacing;
                    }

                    //Setting data for Cell
                    _cachedCells.Add(item.GetComponent<ICell>());
                    DataSource.SetCell(_cachedCells[_cachedCells.Count - 1], poolItem);

                    //Update the Pool size
                    poolSize++;
                    poolItem++;
                }
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
            if (_recycling || _cellPool == null || _cellPool.Count == 0) return zeroVector;

            //Updating Recyclable view bounds since it can change with resolution changes.
            SetRecyclingBounds();

            if (direction.y > 0 && _cellPool[_bottomMostCellIndex].MaxY() > _recyclableViewBounds.min.y)
            {
                return RecycleTopToBottom();
            }
            else if (direction.y < 0 && _cellPool[_topMostCellIndex].MinY() < _recyclableViewBounds.max.y)
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

            int n = 0;
            float posY = IsGrid ? _cellPool[_bottomMostCellIndex].anchoredPosition.y : 0;
            float posX = 0;

            //to determine if content size needs to be updated
            int additionalRows = 0;

            //Recycle until cell at Top is avaiable and current item count smaller than datasource
            bool canScroll = IsLoop ? true : _currentTopItem + 1 < _itemCount;

            while (_cellPool[_topMostCellIndex].MinY() > _recyclableViewBounds.max.y && canScroll)
            {
                _currentTopItem++;
                _currentBottomItem++;

                if (IsLoop)
                {
                    canScroll = true;
                    if (_currentTopItem >= _itemCount) _currentTopItem = 0;
                    if (_currentBottomItem >= _itemCount) _currentBottomItem = 0;
                }
                else
                {
                    canScroll = _currentTopItem + 1 < _itemCount;
                }

                //Cell for row at
                DataSource.SetCell(_cachedCells[_topMostCellIndex], _currentTopItem);

                if (IsGrid)
                {
                    if (_currentTopItem == 0)
                    {
                        n++;
                        _BottomMostCellColoumn = _FirstCellColoumn;
                        posY = _cellPool[_bottomMostCellIndex].anchoredPosition.y - _cellHeight - Spacing;
                        additionalRows++;
                    }
                    else if (++_BottomMostCellColoumn >= _coloumns)
                    {
                        n++;
                        _BottomMostCellColoumn = 0;
                        posY = _cellPool[_bottomMostCellIndex].anchoredPosition.y - _cellHeight - Spacing;
                        additionalRows++;
                    }

                    //Move top cell to bottom
                    posX = _BottomMostCellColoumn * _cellWidth + _BottomMostCellColoumn * Spacing + Padding.left;
                    _cellPool[_topMostCellIndex].anchoredPosition = new Vector2(posX, posY);

                    if (_currentBottomItem == 0)
                    {
                        _TopMostCellColoumn = _FirstCellColoumn;
                        additionalRows--;
                    }
                    else if (++_TopMostCellColoumn >= _coloumns)
                    {
                        _TopMostCellColoumn = 0;
                        additionalRows--;
                    }

                    Debug.Log(_TopMostCellColoumn);
                }
                else
                {
                    //Move top cell to bottom
                    posY = _cellPool[_bottomMostCellIndex].anchoredPosition.y - _cellPool[_bottomMostCellIndex].sizeDelta.y - Spacing;
                    _cellPool[_topMostCellIndex].anchoredPosition = new Vector2(_cellPool[_topMostCellIndex].anchoredPosition.x, posY);
                }

                //set new indices
                _bottomMostCellIndex = _topMostCellIndex;
                _topMostCellIndex++;
                if (_topMostCellIndex >= _cellPool.Count) _topMostCellIndex = 0;

                if (!IsGrid) n++;
            }

            //Content size adjustment 
            if (IsGrid)
            {
                Content.sizeDelta += additionalRows * Vector2.up * (_cellHeight + Spacing);
                //TODO : check if it is supposed to be done only when > 0
                if (additionalRows > 0 || _currentBottomItem == 0 || _currentTopItem == _itemCount - 1)
                {
                    n -= additionalRows;
                }
            }

            //Content anchor position adjustment.
            _cellPool.ForEach((RectTransform cell) => cell.anchoredPosition += n * Vector2.up * (_cellPool[_topMostCellIndex].sizeDelta.y + Spacing));
            Content.anchoredPosition -= n * Vector2.up * ( _cellPool[_topMostCellIndex].sizeDelta.y +  Spacing);
            _recycling = false;
            return -new Vector2(0, n * (_cellPool[_topMostCellIndex].sizeDelta.y + Spacing));

        }

        /// <summary>
        /// Recycles cells from bottom to top in the List heirarchy
        /// </summary>
        private Vector2 RecycleBottomToTop()
        {
            _recycling = true;

            int n = 0;
            float posY = IsGrid ? _cellPool[_topMostCellIndex].anchoredPosition.y : 0;
            float posX = 0;

            //to determine if content size needs to be updated
            int additionalRows = 0;
            //Recycle until cell at bottom is avaiable and current item count is greater than cellpool size
            bool canScroll = IsLoop ? true : _currentBottomItem > 0;

            while (_cellPool[_bottomMostCellIndex].MaxY() < _recyclableViewBounds.min.y && canScroll)
            {
                _currentTopItem--;
                _currentBottomItem--;

                if (IsLoop)
                {
                    canScroll = true;
                    if (_currentTopItem < 0) _currentTopItem = _itemCount - 1;
                    if (_currentBottomItem < 0) _currentBottomItem = _itemCount - 1;
                }
                else
                {
                    canScroll = _currentBottomItem > 0;
                }

                //Cell for row at
                DataSource.SetCell(_cachedCells[_bottomMostCellIndex], _currentBottomItem);

                if (IsGrid)
                {
                    if(_currentBottomItem == _itemCount - 1)
                    {
                        n++;
                        _TopMostCellColoumn = _LastCellColoumn;
                        posY = _cellPool[_topMostCellIndex].anchoredPosition.y + _cellHeight + Spacing;
                        additionalRows++;
                    }
                    else if (--_TopMostCellColoumn < 0)
                    {
                        n++;
                        _TopMostCellColoumn = _coloumns - 1;
                        posY = _cellPool[_topMostCellIndex].anchoredPosition.y + _cellHeight + Spacing;
                        additionalRows++;
                    }

                    //Move bottom cell to top
                    posX = _TopMostCellColoumn * _cellWidth + _TopMostCellColoumn * Spacing + Padding.left;
                    _cellPool[_bottomMostCellIndex].anchoredPosition = new Vector2(posX, posY);

                    if(_currentTopItem == _itemCount - 1)
                    {
                        _BottomMostCellColoumn = _LastCellColoumn;
                        additionalRows--;
                    }
                    else if (--_BottomMostCellColoumn < 0)
                    {
                        _BottomMostCellColoumn = _coloumns - 1;
                        additionalRows--;
                    }
                }
                else
                {
                    //Move bottom cell to top
                    posY = _cellPool[_topMostCellIndex].anchoredPosition.y + _cellPool[_topMostCellIndex].sizeDelta.y + Spacing;
                    _cellPool[_bottomMostCellIndex].anchoredPosition = new Vector2(_cellPool[_bottomMostCellIndex].anchoredPosition.x, posY);
                    n++;
                }

                //set new indices
                _topMostCellIndex = _bottomMostCellIndex;
                _bottomMostCellIndex--;
                if (_bottomMostCellIndex < 0) _bottomMostCellIndex = _cellPool.Count - 1;
            }

            if (IsGrid)
            {
                Content.sizeDelta += additionalRows * Vector2.up * (_cellHeight + Spacing);
                //TODOL : check if it is supposed to be done only when > 0
                //if (additionalRows > 0)
                //{
                //    n -= additionalRows;
                //}
            }

            _cellPool.ForEach((RectTransform cell) => cell.anchoredPosition -= n * Vector2.up * (_cellPool[_topMostCellIndex].sizeDelta.y + Spacing));
            Content.anchoredPosition += n * Vector2.up * (_cellPool[_topMostCellIndex].sizeDelta.y + Spacing);
            _recycling = false;
            return new Vector2(0, n * _cellPool[_topMostCellIndex].sizeDelta.y + n * Spacing);
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

            //Setting top anchor 
            rectTransform.anchorMin = new Vector2(0.5f, 1);
            rectTransform.anchorMax = new Vector2(0.5f, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);

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
        public void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(_recyclableViewBounds.min - new Vector3(2000, 0), _recyclableViewBounds.min + new Vector3(2000, 0));
            Gizmos.color = Color.red;
            Gizmos.DrawLine(_recyclableViewBounds.max - new Vector3(2000, 0), _recyclableViewBounds.max + new Vector3(2000, 0));
        }
        #endregion

    }
}
