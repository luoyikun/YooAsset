﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace YooAsset.Editor
{
	public class AssetBundleGrouperSettingData
	{
		private static readonly Dictionary<string, System.Type> _cacheAddressRuleTypes = new Dictionary<string, System.Type>();
		private static readonly Dictionary<string, IAddressRule> _cacheAddressRuleInstance = new Dictionary<string, IAddressRule>();

		private static readonly Dictionary<string, System.Type> _cachePackRuleTypes = new Dictionary<string, System.Type>();
		private static readonly Dictionary<string, IPackRule> _cachePackRuleInstance = new Dictionary<string, IPackRule>();

		private static readonly Dictionary<string, System.Type> _cacheFilterRuleTypes = new Dictionary<string, System.Type>();
		private static readonly Dictionary<string, IFilterRule> _cacheFilterRuleInstance = new Dictionary<string, IFilterRule>();

		/// <summary>
		/// 配置数据是否被修改
		/// </summary>
		public static bool IsDirty { private set; get; } = false;


		private static AssetBundleGrouperSetting _setting = null;
		public static AssetBundleGrouperSetting Setting
		{
			get
			{
				if (_setting == null)
					LoadSettingData();
				return _setting;
			}
		}

		public static List<string> GetAddressRuleNames()
		{
			if (_setting == null)
				LoadSettingData();

			List<string> names = new List<string>();
			foreach (var pair in _cacheAddressRuleTypes)
			{
				names.Add(pair.Key);
			}
			return names;
		}
		public static List<string> GetPackRuleNames()
		{
			if (_setting == null)
				LoadSettingData();

			List<string> names = new List<string>();
			foreach (var pair in _cachePackRuleTypes)
			{
				names.Add(pair.Key);
			}
			return names;
		}
		public static List<string> GetFilterRuleNames()
		{
			if (_setting == null)
				LoadSettingData();

			List<string> names = new List<string>();
			foreach (var pair in _cacheFilterRuleTypes)
			{
				names.Add(pair.Key);
			}
			return names;
		}
		public static bool HasAddressRuleName(string ruleName)
		{
			foreach (var pair in _cacheAddressRuleTypes)
			{
				if (pair.Key == ruleName)
					return true;
			}
			return false;
		}
		public static bool HasPackRuleName(string ruleName)
		{
			foreach (var pair in _cachePackRuleTypes)
			{
				if (pair.Key == ruleName)
					return true;
			}
			return false;
		}
		public static bool HasFilterRuleName(string ruleName)
		{
			foreach (var pair in _cacheFilterRuleTypes)
			{
				if (pair.Key == ruleName)
					return true;
			}
			return false;
		}


		/// <summary>
		/// 加载配置文件
		/// </summary>
		private static void LoadSettingData()
		{
			// 加载配置文件
			string settingFilePath = $"{EditorTools.GetYooAssetSettingPath()}/{nameof(AssetBundleGrouperSetting)}.asset";
			_setting = AssetDatabase.LoadAssetAtPath<AssetBundleGrouperSetting>(settingFilePath);
			if (_setting == null)
			{
				Debug.LogWarning($"Create new {nameof(AssetBundleGrouperSetting)}.asset : {settingFilePath}");
				_setting = ScriptableObject.CreateInstance<AssetBundleGrouperSetting>();
				EditorTools.CreateFileDirectory(settingFilePath);
				AssetDatabase.CreateAsset(Setting, settingFilePath);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
			else
			{
				Debug.Log($"Load {nameof(AssetBundleGrouperSetting)}.asset ok");
			}

			// IPackRule
			{
				// 清空缓存集合
				_cachePackRuleTypes.Clear();
				_cachePackRuleInstance.Clear();

				// 获取所有类型
				List<Type> types = new List<Type>(100)
				{
					typeof(PackSeparately),
					typeof(PackDirectory),
					typeof(PackTopDirectory),
					typeof(PackCollector),
					typeof(PackGrouper),
					typeof(PackRawFile),
				};

				TypeCache.TypeCollection collection = TypeCache.GetTypesDerivedFrom<IPackRule>();
				var customTypes = collection.ToList();
				types.AddRange(customTypes);
				for (int i = 0; i < types.Count; i++)
				{
					Type type = types[i];
					if (_cachePackRuleTypes.ContainsKey(type.Name) == false)
						_cachePackRuleTypes.Add(type.Name, type);
				}
			}

			// IFilterRule
			{
				// 清空缓存集合
				_cacheFilterRuleTypes.Clear();
				_cacheFilterRuleInstance.Clear();

				// 获取所有类型
				List<Type> types = new List<Type>(100)
				{
					typeof(CollectAll),
					typeof(CollectScene),
					typeof(CollectPrefab),
					typeof(CollectSprite)
				};

				TypeCache.TypeCollection collection = TypeCache.GetTypesDerivedFrom<IFilterRule>();
				var customTypes = collection.ToList();
				types.AddRange(customTypes);
				for (int i = 0; i < types.Count; i++)
				{
					Type type = types[i];
					if (_cacheFilterRuleTypes.ContainsKey(type.Name) == false)
						_cacheFilterRuleTypes.Add(type.Name, type);
				}
			}

			// IAddressRule
			{
				// 清空缓存集合
				_cacheAddressRuleTypes.Clear();
				_cacheAddressRuleInstance.Clear();

				// 获取所有类型
				List<Type> types = new List<Type>(100)
				{
					typeof(AddressByFileName),
					typeof(AddressByCollectorAndFileName),
					typeof(AddressByGrouperAndFileName)
				};

				TypeCache.TypeCollection collection = TypeCache.GetTypesDerivedFrom<IAddressRule>();
				var customTypes = collection.ToList();
				types.AddRange(customTypes);
				for (int i = 0; i < types.Count; i++)
				{
					Type type = types[i];
					if (_cacheAddressRuleTypes.ContainsKey(type.Name) == false)
						_cacheAddressRuleTypes.Add(type.Name, type);
				}
			}
		}

		/// <summary>
		/// 存储文件
		/// </summary>
		public static void SaveFile()
		{
			if (Setting != null)
			{
				IsDirty = false;
				EditorUtility.SetDirty(Setting);
				AssetDatabase.SaveAssets();
				Debug.Log($"{nameof(AssetBundleGrouperSetting)}.asset is saved!");
			}
		}

		/// <summary>
		/// 清空所有数据
		/// </summary>
		public static void ClearAll()
		{
			Setting.AutoCollectShaders = false;
			Setting.ShadersBundleName = string.Empty;
			Setting.Groupers.Clear();
			SaveFile();
		}

		// 实例类相关
		public static IAddressRule GetAddressRuleInstance(string ruleName)
		{
			if (_cacheAddressRuleInstance.TryGetValue(ruleName, out IAddressRule instance))
				return instance;

			// 如果不存在创建类的实例
			if (_cacheAddressRuleTypes.TryGetValue(ruleName, out Type type))
			{
				instance = (IAddressRule)Activator.CreateInstance(type);
				_cacheAddressRuleInstance.Add(ruleName, instance);
				return instance;
			}
			else
			{
				throw new Exception($"{nameof(IAddressRule)}类型无效：{ruleName}");
			}
		}
		public static IPackRule GetPackRuleInstance(string ruleName)
		{
			if (_cachePackRuleInstance.TryGetValue(ruleName, out IPackRule instance))
				return instance;

			// 如果不存在创建类的实例
			if (_cachePackRuleTypes.TryGetValue(ruleName, out Type type))
			{
				instance = (IPackRule)Activator.CreateInstance(type);
				_cachePackRuleInstance.Add(ruleName, instance);
				return instance;
			}
			else
			{
				throw new Exception($"{nameof(IPackRule)}类型无效：{ruleName}");
			}
		}
		public static IFilterRule GetFilterRuleInstance(string ruleName)
		{
			if (_cacheFilterRuleInstance.TryGetValue(ruleName, out IFilterRule instance))
				return instance;

			// 如果不存在创建类的实例
			if (_cacheFilterRuleTypes.TryGetValue(ruleName, out Type type))
			{
				instance = (IFilterRule)Activator.CreateInstance(type);
				_cacheFilterRuleInstance.Add(ruleName, instance);
				return instance;
			}
			else
			{
				throw new Exception($"{nameof(IFilterRule)}类型无效：{ruleName}");
			}
		}

		// 可寻址编辑相关
		public static void ModifyAddressable(bool enableAddressable)
		{
			Setting.EnableAddressable = enableAddressable;
			IsDirty = true;
		}

		// 着色器编辑相关
		public static void ModifyShader(bool isCollectAllShaders, string shadersBundleName)
		{
			Setting.AutoCollectShaders = isCollectAllShaders;
			Setting.ShadersBundleName = shadersBundleName;
			IsDirty = true;
		}

		// 资源分组编辑相关
		public static void CreateGrouper(string grouperName)
		{
			AssetBundleGrouper grouper = new AssetBundleGrouper();
			grouper.GrouperName = grouperName;
			Setting.Groupers.Add(grouper);
			IsDirty = true;
		}
		public static void RemoveGrouper(AssetBundleGrouper grouper)
		{
			if (Setting.Groupers.Remove(grouper))
			{
				IsDirty = true;
			}
			else
			{
				Debug.LogWarning($"Failed remove grouper : {grouper.GrouperName}");
			}
		}
		public static void ModifyGrouper(AssetBundleGrouper grouper)
		{
			if (grouper != null)
			{
				IsDirty = true;
			}
		}

		// 资源收集器编辑相关
		public static void CreateCollector(AssetBundleGrouper grouper, string collectPath)
		{
			AssetBundleCollector collector = new AssetBundleCollector();
			collector.CollectPath = collectPath;
			grouper.Collectors.Add(collector);
			IsDirty = true;
		}
		public static void RemoveCollector(AssetBundleGrouper grouper, AssetBundleCollector collector)
		{
			if (grouper.Collectors.Remove(collector))
			{
				IsDirty = true;
			}
			else
			{
				Debug.LogWarning($"Failed remove collector : {collector.CollectPath}");
			}
		}
		public static void ModifyCollector(AssetBundleGrouper grouper, AssetBundleCollector collector)
		{
			if (grouper != null && collector != null)
			{
				IsDirty = true;
			}
		}
	}
}