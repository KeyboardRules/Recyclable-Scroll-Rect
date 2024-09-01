//MIT License
//Copyright (c) 2020 Mohammed Iqubal Hussain
//Website : Polyandcode.com 

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PolyAndCode.UI
{
    /// <summary>
    /// Recyling system for Loop Horizontal type.
    /// </summary>
    public class LoopHorizontalRecyclingSystem : RecyclingSystem
    {

        //Assigned by constructor
        private readonly int _rows;

        //Trackers
        private int _leftMostCellIndex, _rightMostCellIndex; //Topmost and bottommost cell in the List
        private int _LeftMostCellRow, _RightMostCellRow; // used for recyling in Grid layout. leftmost and rightmost row
        private int _FirstCellRow, _LastCellRow; //used for loop recycling in grid layout.

        //Cached zero vector 
        private Vector2 zeroVector = Vector2.zero;
        #region INIT
        public LoopHorizontalRecyclingSystem(RectTransform prototypeCell, RectTransform viewport, RectTransform content, RectOffset padding, Vector2 spacing, IRecyclableScrollRectDataSource dataSource, bool isGrid, bool isReverse, int rows)
        {
            PrototypeCell = prototypeCell;
            Viewport = viewport;
            Content = content;
            Padding = padding;
            Spacing = spacing;
            DataSource = dataSource;
            IsGrid = isGrid;
            IsReverse = isReverse;

            _rows = isGrid ? rows : 1;
            _recyclableViewBounds = new Bounds();
        }

        /// <summary>
        /// Corotuine for initiazation.
        /// Using coroutine for init because few UI stuff requires a frame to update
        /// </summary>
        /// <param name="onInitialized">callback when init done</param>
        /// <returns></returns>
        public override void Init(Action onInitialized)
        {
            //Setting up container and bounds
            SetLeftAnchor(Content);
            Content.anchoredPosition = Vector3.zero;
            SetRecyclingBounds();

            //Cell Poool
            CreateCellPool();

            _currentTopItem = (_cacheCellPool.Count > _itemCount ? _cacheCellPool.Count % _itemCount : _cacheCellPool.Count) - 1;
            _currentBottomItem = 0;

            _FirstCellRow = 0;
            _LastCellRow = (_itemCount % _rows) - 1;

            if (_LastCellRow == -1) _LastCellRow = _rows - 1;

            _leftMostCellIndex = 0;
            _rightMostCellIndex = _cacheCellPool.Count - 1;


            //Set content width according to no of colomns
            int coloums;

            if (IsGrid && _itemCount < _cacheCellPool.Count)
            {
                int colPerCluster = Mathf.CeilToInt((float)_itemCount / _rows);

                int residual = _cacheCellPool.Count % _itemCount;

                int col1 = Mathf.CeilToInt(((float)(_cacheCellPool.Count - residual) * colPerCluster) / _itemCount);
                int col2 = Mathf.CeilToInt((float)residual / _rows);

                coloums = col1 + col2;
            }
            else
            {
                coloums = Mathf.CeilToInt(((float)_cacheCellPool.Count) / _rows);
            }

            float contentXSize = coloums * _cellWidth + (coloums - 1) * Spacing.x + Padding.left + Padding.right;
            Content.sizeDelta = new Vector2(contentXSize, Content.sizeDelta.y);
            SetLeftAnchor(Content);

            if (onInitialized != null) onInitialized();
        }
        /// <summary>
        /// Function for reseting recycle view, when you have things like search bar and you wanna update view list, use this
        /// </summary>
        /// <param name="onReset"></param>
        public override void Reset(Action onReset = null)
        {
            //Setting up container and bounds
            SetLeftAnchor(Content);
            Content.anchoredPosition = Vector3.zero;
            SetRecyclingBounds();

            ResetCellPool();

            _currentTopItem = (_cacheCellPool.Count > _itemCount ? _cacheCellPool.Count % _itemCount : _cacheCellPool.Count) - 1;
            _currentBottomItem = 0;

            _FirstCellRow = 0;
            _LastCellRow = (_itemCount % _rows) - 1;

            if (_LastCellRow == -1) _LastCellRow = _rows - 1;

            _leftMostCellIndex = 0;
            _rightMostCellIndex = _cacheCellPool.Count - 1;


            //Set content width according to no of colomns
            int coloums;

            if (IsGrid && _itemCount < _cacheCellPool.Count)
            {
                int colPerCluster = Mathf.CeilToInt((float)_itemCount / _rows);

                int residual = _cacheCellPool.Count % _itemCount;

                int col1 = Mathf.CeilToInt(((float)(_cacheCellPool.Count - residual) * colPerCluster) / _itemCount);
                int col2 = Mathf.CeilToInt((float)residual / _rows);

                coloums = col1 + col2;
            }
            else
            {
                coloums = Mathf.CeilToInt(((float)_cacheCellPool.Count) / _rows);
            }

            float contentXSize = coloums * _cellWidth + (coloums - 1) * Spacing.x + Padding.left + Padding.right;
            Content.sizeDelta = new Vector2(contentXSize, Content.sizeDelta.y);
            SetLeftAnchor(Content);

            if (onReset != null) onReset();
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
        }
        /// <summary>
        /// This function is not supported in loop mode (yet!)
        /// </summary>
        /// <param name="index"></param>
        /// <param name="onGoTo"></param>
        public override Vector2 ScrollToItem(int index)
        {
            return Vector2.zero;
        }
        /// <summary>
        /// Sets the uppper and lower bounds for recycling cells.
        /// </summary>
        private void SetRecyclingBounds()
        {
            Viewport.GetWorldCorners(_corners);
            float threshHold = RecyclingThreshold * (_corners[2].x - _corners[0].x);
            _recyclableViewBounds.min = new Vector3(_corners[0].x - threshHold, _corners[0].y);
            _recyclableViewBounds.max = new Vector3(_corners[2].x + threshHold, _corners[2].y);
        }

        /// <summary>
        /// Creates cell pool for recycling, cached cells
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
                _totalCellPool = new List<RectTransform>();
                _cacheCellPool = new List<RectTransform>();
            }

            //Set the prototype cell active and set cell anchor as top 
            PrototypeCell.gameObject.SetActive(true);
            SetLeftAnchor(PrototypeCell);

            //set new cell size according to its aspect ratio
            _cellHeight = ((Content.rect.height - Padding.top - Padding.bottom - (_rows - 1) * Spacing.y) / _rows);
            _cellWidth = PrototypeCell.sizeDelta.x / PrototypeCell.sizeDelta.y * _cellHeight;

            _itemCount = DataSource.GetItemCount();
            //Reset
            _LeftMostCellRow = _RightMostCellRow = 0;

            //Get the required pool coverage and mininum size for the Cell pool
            //Temps
            float currentPoolCoverage = 0;
            int poolSize = 0;
            int poolItem = 0;
            float posX = Padding.left;
            float posY = 0;

            //Get the required pool coverage and mininum size for the Cell pool
            float requriedCoverage = MinPoolCoverage * Viewport.rect.width;
            bool isReset;
            int minPoolSize;

            minPoolSize = IsGrid ? MinPoolSize * _rows : MinPoolSize;

            //create cells untill the Pool area is covered and pool size is the minimum required
            while (poolSize < minPoolSize || currentPoolCoverage < requriedCoverage)
            {
                //Instantiate and add to Pool
                RectTransform item = (UnityEngine.Object.Instantiate(PrototypeCell.gameObject)).GetComponent<RectTransform>();
                item.name = "Cell " + poolSize.ToString();
                item.sizeDelta = new Vector2(_cellWidth, _cellHeight);
                _totalCellPool.Add(item);
                _cacheCellPool.Add(item);
                item.SetParent(Content, false);

                isReset = poolItem + 1 >= _itemCount;

                if (IsGrid)
                {
                    posY = -_RightMostCellRow * _cellHeight - _RightMostCellRow * Spacing.y - Padding.top;
                    item.anchoredPosition = new Vector2(posX, posY);
                    if (++_RightMostCellRow >= _rows || poolItem + 1 >= _itemCount)
                    {
                        _RightMostCellRow = _FirstCellRow;
                        posX += _cellWidth + Spacing.x;
                        currentPoolCoverage += item.rect.width + Spacing.x;
                    }
                }
                else
                {
                    posY = (Padding.bottom - Padding.top) / 2;
                    item.anchoredPosition = new Vector2(posX, posY);
                    posX = item.anchoredPosition.x + item.rect.width + Spacing.x;
                    currentPoolCoverage += item.rect.width + Spacing.x;
                }

                //Setting data for Cell
                _cachedCells.Add(item.GetComponent<ICell>());
                DataSource.SetCell(_cachedCells[_cachedCells.Count - 1], poolItem);

                //Update the Pool size
                poolSize++;
                poolItem++;

                if (poolItem >= _itemCount) poolItem = 0;
            }

            if (IsGrid)
            {
                _RightMostCellRow = (_RightMostCellRow - 1 + _rows) % _rows;
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

            //set new cell size according to its aspect ratio
            _cellHeight = ((Content.rect.height - Padding.top - Padding.bottom - (_rows - 1) * Spacing.y) / _rows);
            _cellWidth = PrototypeCell.sizeDelta.x / PrototypeCell.sizeDelta.y * _cellHeight;

            _itemCount = DataSource.GetItemCount();
            //Reset
            _LeftMostCellRow = _RightMostCellRow = 0;

            //Get the required pool coverage and mininum size for the Cell pool
            //Temps
            float currentPoolCoverage = 0;
            int poolSize = 0;
            int poolItem = 0;
            float posX = Padding.left;
            float posY = 0;

            //Get the required pool coverage and mininum size for the Cell pool
            float requriedCoverage = MinPoolCoverage * Viewport.rect.width;
            bool isReset;
            int minPoolSize;

            //Set the prototype cell active and set cell anchor as top 
            PrototypeCell.gameObject.SetActive(true);

            minPoolSize = IsGrid ? MinPoolSize * _rows : MinPoolSize;

            //create cells untill the Pool area is covered and pool size is the minimum required
            while (poolSize < minPoolSize || currentPoolCoverage < requriedCoverage)
            {
                //Reset and add to cached pool
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


                isReset = poolItem + 1 >= _itemCount;

                if (IsGrid)
                {
                    posY = -_RightMostCellRow * _cellHeight - _RightMostCellRow * Spacing.y - Padding.top;
                    item.anchoredPosition = new Vector2(posX, posY);
                    if (++_RightMostCellRow >= _rows || poolItem + 1 >= _itemCount)
                    {
                        _RightMostCellRow = _FirstCellRow;
                        posX += _cellWidth + Spacing.x;
                        currentPoolCoverage += item.rect.width + Spacing.x;
                    }
                }
                else
                {
                    posY = (Padding.bottom - Padding.top) / 2;
                    item.anchoredPosition = new Vector2(posX, posY);
                    posX = item.anchoredPosition.x + item.rect.width + Spacing.x;
                    currentPoolCoverage += item.rect.width + Spacing.x;
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

            if (direction.x < 0 && _cacheCellPool[_rightMostCellIndex].MinX() < _recyclableViewBounds.max.x)
            {
                return RecycleLeftToRight();
            }
            else if (direction.x > 0 && _cacheCellPool[_leftMostCellIndex].MaxX() > _recyclableViewBounds.min.x)
            {
                return RecycleRightToleft();
            }
            return zeroVector;
        }

        /// <summary>
        /// Recycles cells from Left to Right in the List heirarchy
        /// </summary>
        private Vector2 RecycleLeftToRight()
        {
            _recycling = true;

            int n = 0;
            float posX = IsGrid ? _cacheCellPool[_rightMostCellIndex].anchoredPosition.x : 0;
            float posY = 0;

            //to determine if content size needs to be updated
            int additionalColoums = 0;

            while (_cacheCellPool[_leftMostCellIndex].MaxX() < _recyclableViewBounds.min.x)
            {
                _currentTopItem++;
                _currentBottomItem++;

                if (_currentTopItem >= _itemCount) _currentTopItem = 0;
                if (_currentBottomItem >= _itemCount) _currentBottomItem = 0;

                //Cell for row at
                DataSource.SetCell(_cachedCells[_leftMostCellIndex], _currentTopItem);

                if (IsGrid)
                {
                    if (_currentTopItem == 0)
                    {
                        n++;
                        _RightMostCellRow = _FirstCellRow;
                        posX = _cacheCellPool[_rightMostCellIndex].anchoredPosition.x + _cellWidth + Spacing.x;
                        additionalColoums++;
                    }
                    else if (++_RightMostCellRow >= _rows)
                    {
                        n++;
                        _RightMostCellRow = _FirstCellRow;
                        posX = _cacheCellPool[_rightMostCellIndex].anchoredPosition.x + _cellWidth + Spacing.x;
                        additionalColoums++;
                    }

                    //Move Left most cell to right
                    posY = -_RightMostCellRow * _cellHeight - _RightMostCellRow * Spacing.y - Padding.top;
                    _cacheCellPool[_leftMostCellIndex].anchoredPosition = new Vector2(posX, posY);

                    if (_currentBottomItem == 0)
                    {
                        _LeftMostCellRow = _FirstCellRow;
                        additionalColoums--;
                    }
                    else if (++_LeftMostCellRow >= _rows)
                    {
                        _LeftMostCellRow = _FirstCellRow;
                        additionalColoums--;
                    }
                }
                else
                {
                    //Move Left most cell to right
                    posX = _cacheCellPool[_rightMostCellIndex].anchoredPosition.x + _cacheCellPool[_rightMostCellIndex].sizeDelta.x + Spacing.x;
                    _cacheCellPool[_leftMostCellIndex].anchoredPosition = new Vector2(posX, _cacheCellPool[_leftMostCellIndex].anchoredPosition.y);
                }

                //set new indices
                _rightMostCellIndex = _leftMostCellIndex;
                _leftMostCellIndex++;
                if (_leftMostCellIndex >= _cacheCellPool.Count) _leftMostCellIndex = 0;

                if (!IsGrid) n++;
            }

            //Content size adjustment 
            if (IsGrid)
            {
                Content.sizeDelta += additionalColoums * Vector2.right * (_cellWidth + Spacing.x);
                if (additionalColoums > 0 || _currentBottomItem == 0 || _currentTopItem == _itemCount - 1)
                {
                    n -= additionalColoums;
                }
            }

            //Content anchor position adjustment.
            _cacheCellPool.ForEach((RectTransform cell) => cell.anchoredPosition -= n * Vector2.right * (_cacheCellPool[_leftMostCellIndex].sizeDelta.x + Spacing.x));
            Content.anchoredPosition += n * Vector2.right * (_cacheCellPool[_leftMostCellIndex].sizeDelta.x + Spacing.x);
            _recycling = false;
            return n * Vector2.right * (_cacheCellPool[_leftMostCellIndex].sizeDelta.x + Spacing.x);

        }

        /// <summary>
        /// Recycles cells from Right to Left in the List heirarchy
        /// </summary>
        private Vector2 RecycleRightToleft()
        {
            _recycling = true;

            int n = 0;
            float posX = IsGrid ? _cacheCellPool[_leftMostCellIndex].anchoredPosition.x : 0;
            float posY = 0;

            //to determine if content size needs to be updated
            int additionalColoums = 0;
            while (_cacheCellPool[_rightMostCellIndex].MinX() > _recyclableViewBounds.max.x)
            {
                _currentTopItem--;
                _currentBottomItem--;

                if (_currentTopItem < 0) _currentTopItem = _itemCount - 1;
                if (_currentBottomItem < 0) _currentBottomItem = _itemCount - 1;

                //Cell for row at
                DataSource.SetCell(_cachedCells[_rightMostCellIndex], _currentBottomItem);

                if (IsGrid)
                {
                    if (_currentBottomItem == _itemCount - 1)
                    {
                        n++;
                        _LeftMostCellRow = _LastCellRow;
                        posX = _cacheCellPool[_leftMostCellIndex].anchoredPosition.x - _cellWidth - Spacing.x;
                        additionalColoums++;
                    }
                    else if (--_LeftMostCellRow < 0)
                    {
                        n++;
                        _LeftMostCellRow = _rows - 1;
                        posX = _cacheCellPool[_leftMostCellIndex].anchoredPosition.x - _cellWidth - Spacing.x;
                        additionalColoums++;
                    }

                    //Move Right most cell to left
                    posY = -_LeftMostCellRow * _cellHeight - _LeftMostCellRow * Spacing.y - Padding.top;
                    _cacheCellPool[_rightMostCellIndex].anchoredPosition = new Vector2(posX, posY);

                    if (_currentTopItem == _itemCount - 1)
                    {
                        _RightMostCellRow = _LastCellRow;
                        additionalColoums--;
                    }
                    else if (--_RightMostCellRow < 0)
                    {
                        _RightMostCellRow = _rows - 1;
                        additionalColoums--;
                    }
                }
                else
                {
                    //Move Right most cell to left
                    posX = _cacheCellPool[_leftMostCellIndex].anchoredPosition.x - _cacheCellPool[_leftMostCellIndex].sizeDelta.x - Spacing.x;
                    _cacheCellPool[_rightMostCellIndex].anchoredPosition = new Vector2(posX, _cacheCellPool[_rightMostCellIndex].anchoredPosition.y);
                    n++;
                }

                //set new indices
                _leftMostCellIndex = _rightMostCellIndex;
                _rightMostCellIndex--;
                if (_rightMostCellIndex < 0) _rightMostCellIndex = _cacheCellPool.Count - 1;
            }

            //Content size adjustment
            if (IsGrid)
            {
                Content.sizeDelta += additionalColoums * Vector2.right * (_cellWidth + Spacing.x);
                //if (additionalColoums > 0)
                //{
                //    n -= additionalColoums;
                //}
            }

            //Content anchor position adjustment.
            _cacheCellPool.ForEach((RectTransform cell) => cell.anchoredPosition += n * Vector2.right * (_cacheCellPool[_leftMostCellIndex].sizeDelta.x + Spacing.x));
            Content.anchoredPosition -= n * Vector2.right * (_cacheCellPool[_leftMostCellIndex].sizeDelta.x + Spacing.x);
            _recycling = false;
            return n * Vector2.left * (_cacheCellPool[_leftMostCellIndex].sizeDelta.x + Spacing.x);
        }
        #endregion

        #region  HELPERS
        /// <summary>
        /// Anchoring cell and content rect transforms to top preset. Makes repositioning easy.
        /// </summary>
        /// <param name="rectTransform"></param>
        private void SetLeftAnchor(RectTransform rectTransform)
        {
            //Saving to reapply after anchoring. Width and height changes if anchoring is change. 
            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;

            Vector2 pos = IsGrid ? new Vector2(0, 1) : new Vector2(0, 0.5f);

            //Setting top anchor 
            rectTransform.anchorMin = pos;
            rectTransform.anchorMax = pos;
            rectTransform.pivot = pos;

            //Reapply size
            rectTransform.sizeDelta = new Vector2(width, height);
        }

        #endregion

        #region  TESTING
        public override void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(_recyclableViewBounds.min - new Vector3(0, 2000), _recyclableViewBounds.min + new Vector3(0, 2000));
            Gizmos.color = Color.red;
            Gizmos.DrawLine(_recyclableViewBounds.max - new Vector3(0, 2000), _recyclableViewBounds.max + new Vector3(0, 2000));
        }
        #endregion
    }

}
