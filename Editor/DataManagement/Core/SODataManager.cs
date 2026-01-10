using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScriptableObjectDataManagement
{
    /// <summary>
    /// ScriptableObject 数据管理器。单例模式，管理所有扫描到的 ScriptableObject 数据。
    /// </summary>
    public sealed class SODataManager
    {
        private static SODataManager _instance;
        private static readonly object _lock = new object();

        private SOScanResult _currentResult;
        private bool _isScanning;

        /// <summary>
        /// 获取单例实例。
        /// </summary>
        public static SODataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new SODataManager();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 当前扫描结果。
        /// </summary>
        public SOScanResult CurrentResult => _currentResult;

        /// <summary>
        /// 是否正在扫描。
        /// </summary>
        public bool IsScanning => _isScanning;

        /// <summary>
        /// 扫描完成事件。参数为扫描结果。
        /// </summary>
        public event Action<SOScanResult> ScanCompleted;

        /// <summary>
        /// 扫描开始事件。
        /// </summary>
        public event Action ScanStarted;

        /// <summary>
        /// 私有构造函数。
        /// </summary>
        private SODataManager() { }

        /// <summary>
        /// 执行扫描。
        /// </summary>
        public void Scan()
        {
            if (_isScanning)
                return;

            _isScanning = true;
            ScanStarted?.Invoke();

            try
            {
                // 调用扫描服务
                _currentResult = SOScanService.ScanAll();
                ScanCompleted?.Invoke(_currentResult);
            }
            finally
            {
                _isScanning = false;
            }
        }

        /// <summary>
        /// 获取指定类型的所有资产。
        /// </summary>
        public IReadOnlyList<ScriptableObject> GetAssetsByType(Type type)
        {
            if (_currentResult == null)
                return Array.Empty<ScriptableObject>();

            return _currentResult.GetAssetsOfType(type);
        }

        /// <summary>
        /// 获取指定类型的所有资产（泛型版本）。
        /// </summary>
        public IReadOnlyList<T> GetAssetsByType<T>() where T : ScriptableObject
        {
            if (_currentResult == null)
                return Array.Empty<T>();

            return _currentResult.GetAssetsOfType<T>();
        }

        /// <summary>
        /// 按名称查找资产。
        /// </summary>
        public ScriptableObject FindByName(string name)
        {
            if (_currentResult == null)
                return null;

            return _currentResult.FindByName(name);
        }

        /// <summary>
        /// 按名称查找指定类型的资产。
        /// </summary>
        public T FindByName<T>(string name) where T : ScriptableObject
        {
            if (_currentResult == null)
                return null;

            var result = _currentResult.FindByName(name);
            return result as T;
        }

        /// <summary>
        /// 获取所有 ScriptableObject 类型。
        /// </summary>
        public IEnumerable<Type> GetAllTypes()
        {
            if (_currentResult == null)
                return Enumerable.Empty<Type>();

            return _currentResult.GetAllTypes();
        }

        /// <summary>
        /// 根据谓词查找资产。
        /// </summary>
        public IEnumerable<ScriptableObject> FindAssets(Predicate<ScriptableObject> predicate)
        {
            if (_currentResult == null)
                return Enumerable.Empty<ScriptableObject>();

            return _currentResult.AssetsByType
                .Values
                .SelectMany(list => list)
                .Where(so => predicate(so));
        }

        /// <summary>
        /// 按命名空间获取类型。
        /// </summary>
        public IEnumerable<Type> GetTypesInNamespace(string namespacePattern)
        {
            if (_currentResult == null)
                return Enumerable.Empty<Type>();

            return _currentResult.GetTypesInNamespace(namespacePattern);
        }

        /// <summary>
        /// 获取所有分类名称。
        /// </summary>
        public IEnumerable<string> GetCategories()
        {
            if (_currentResult == null)
                return Enumerable.Empty<string>();

            return _currentResult.CategoryTree.Select(node => node.DisplayName);
        }

        /// <summary>
        /// 获取分类树。
        /// </summary>
        public IReadOnlyList<SOTypeNode> GetCategoryTree()
        {
            if (_currentResult == null)
                return Array.Empty<SOTypeNode>();

            return _currentResult.CategoryTree;
        }

        /// <summary>
        /// 清除当前扫描结果。
        /// </summary>
        public void Clear()
        {
            _currentResult = null;
        }
    }
}
