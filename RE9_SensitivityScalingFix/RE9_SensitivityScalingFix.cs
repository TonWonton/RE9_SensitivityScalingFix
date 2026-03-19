#nullable enable
using System;
using Hexa.NET.ImGui;
using REFrameworkNET.Callbacks;
using REFrameworkNET.Attributes;
using REFrameworkNET;
using REFrameworkNETPluginConfig;
using REFrameworkNETPluginConfig.Utility;
using app;
using via;


namespace RE9_SensitivityScalingFix
{
	public class SensitivityScalingFixPlugin
	{
		#region PLUGIN_INFO

		/*PLUGIN INFO*/
		public const string PLUGIN_NAME = "RE9_SensitivityScalingFix";
		public const string COPYRIGHT = "";
		public const string COMPANY = "https://github.com/TonWonton/RE9_SensitivityScalingFix";

		public const string GUID = "RE9_SensitivityScalingFix";
		public const string VERSION = "1.0.1";

		public const string GUID_AND_V_VERSION = GUID + " v" + VERSION;

		#endregion



		/* VARIABLES */
		//Const
		public const float BASE_SENSITIVITY = 7f; //Game's internal sensitivity divisor
		public const float SCOPE_CAMERA_360_SPEED_RATE = 0.57f; //Estimated scope camera speed rate multiplier that matches 360 distance
		public const float BASE_FOV = 40f; //Use 40 FOV as base since both characters use 40 FOV in 3rd person
		public const float BASE_FOV_RAD = BASE_FOV * MathF.PI / 180f;
		private static readonly float _baseFOVRadDiv2Tan = MathF.Tan(BASE_FOV_RAD / 2f);

		//Config
		private static Config _config = new Config(GUID);
		private static ConfigEntry<bool> _enabled = _config.Add("Enabled", true);
		private static ConfigEntry<bool> _useCustomSensitivity = _config.Add("Use custom sensitivity", false);
		private static ConfigEntry<float> _sensitivity = _config.Add("Sensitivity", 1f);
		private static ConfigEntry<float> _scopeSensitivityMultiplier = _config.Add("Scope sensitivity multiplier", 1f);
		private static ConfigEntry<float> _monitorDistanceHorizontal = _config.Add("Monitor distance horizontal (MDH)", 1f);

		//Variables
		private static CameraOptionSubSystem? _cameraOptionSubSystem;
		private static Camera? _mainCamera;
		private static float _originalScopeCameraSpeedRate;
		private static bool _initialized = false;



		/* METHODS */
		[MethodHook(typeof(CameraInputSubSystem), "get_MouseSensitivity", MethodHookType.Post)]
		public static void PostGetMouseSensitivity(ref ulong retVal)
		{
			if (_enabled.Value && _initialized)
			{
				//Get sensitivity
				float desiredSensitivity = _useCustomSensitivity.Value
					? BASE_SENSITIVITY / _sensitivity.Value //Divide since the sensitivity is a divisor
					: BitConverter.Int32BitsToSingle((int)retVal)
				;

				//Get FOV and aspect ratio
				Camera mainCamera = _mainCamera!;
				float cameraFOV = mainCamera.FOV; //FOV is vertical
				float cameraFOVRad = cameraFOV * MathF.PI / 180f;
				float aspectRatio = mainCamera.AspectRatio;

				//Calculate new sensitivity
				float coefficient = _monitorDistanceHorizontal.Value * aspectRatio;
				float baseFOVRadDiv2Tan = _baseFOVRadDiv2Tan;
				float scale = coefficient > 0f
					? MathF.Atan(coefficient * MathF.Tan(cameraFOVRad / 2f)) / MathF.Atan(coefficient * baseFOVRadDiv2Tan)
					: MathF.Tan(cameraFOVRad / 2f) / baseFOVRadDiv2Tan
				;

				//Set new sensitivity
				float newSensitivity = desiredSensitivity / scale; //Divide since the sensitivity is a divisor
				retVal = (retVal & 0xFFFFFFFF00000000) | (uint)BitConverter.SingleToInt32Bits(newSensitivity);
			}
		}

		[Callback(typeof(UpdateBehavior), CallbackType.Pre)]
		public static void PreUpdateBehavior()
		{
			if (_initialized == false)
			{
				CameraSystem? cameraSystem = API.GetManagedSingletonT<CameraSystem>();
				if (cameraSystem == null) return;

				//Get camera option subsystem if null
				if (_cameraOptionSubSystem == null)
				{
					_cameraOptionSubSystem = cameraSystem._OptionSubSystem;
				}

				//Get main camera if null
				if (_mainCamera == null)
				{
					GameObject? cameraGameObject = cameraSystem.getCameraObject(CameraDefine.Role.Main);
					if (cameraGameObject != null)
					{
						_mainCamera = cameraGameObject.TryGetComponent<Camera>("via.Camera");
					}
				}

				if (_cameraOptionSubSystem != null && _mainCamera != null)
				{
					_originalScopeCameraSpeedRate = _cameraOptionSubSystem._ScopeCameraSpeedRate;

					_initialized = true;
					TrySetScopeCameraSpeedRate();

					Log.Info("Initialized");
				}
			}
		}

		private static void TrySetScopeCameraSpeedRate()
		{
			if (_initialized)
			{
				CameraOptionSubSystem cameraOptionSubSystem = _cameraOptionSubSystem!;

				if (_enabled.Value)
				{
					//Set scope camera speed rate so it matches 360 distance.
					//Every sensitivity should now be the same 360 distance
					//since the game uses 360 distance for everything and only a multiplier for scope sensitivity
					cameraOptionSubSystem._ScopeCameraSpeedRate = _useCustomSensitivity.Value
						? SCOPE_CAMERA_360_SPEED_RATE * _scopeSensitivityMultiplier.Value
						: SCOPE_CAMERA_360_SPEED_RATE
					;
				}
				else
				{
					//Revert scope camera speed rate
					cameraOptionSubSystem._ScopeCameraSpeedRate = _originalScopeCameraSpeedRate;
				}
			}
		}



		/* EVENT HANDLING */
		private static void OnSettingsChanged()
		{
			_config.SaveToJson();
			TrySetScopeCameraSpeedRate();
		}



		/* INITIALIZATION */
		[PluginEntryPoint]
		private static void Load()
		{
			RegisterConfigEvents();
			_config.LoadFromJson();
			Log.Info("Loaded " + VERSION);
		}

		[PluginExitPoint]
		private static void Unload()
		{
			UnregisterConfigEvents();

			if (_initialized)
			{
				//Revert scope camera speed rate
				_cameraOptionSubSystem!._ScopeCameraSpeedRate = _originalScopeCameraSpeedRate;
			}

			Log.Info("Unloaded " + VERSION);
		}

		private static void RegisterConfigEvents()
		{
			foreach (ConfigEntryBase configEntry in _config.Values)
			{
				configEntry.ValueChanged += OnSettingsChanged;
			}
		}

		private static void UnregisterConfigEvents()
		{
			foreach (ConfigEntryBase configEntry in _config.Values)
			{
				configEntry.ValueChanged -= OnSettingsChanged;
			}
		}



		/* PLUGIN GENERATED UI */

		[Callback(typeof(ImGuiDrawUI), CallbackType.Pre)]
		public static void PreImGuiDrawUI()
		{
			if (API.IsDrawingUI() && ImGui.TreeNode(GUID_AND_V_VERSION))
			{
				const float SLIDER_STEP_0p001 = 0.001f;
				int labelNr = 0;

				ImGuiF.Category("General");
				_enabled.Checkbox().ResetButton(ref labelNr);
				_monitorDistanceHorizontal.DragFloat(SLIDER_STEP_0p001, 0f, 10f).ResetButton(ref labelNr);

				ImGuiF.Category("Custom sensitivity");
				_useCustomSensitivity.Checkbox().ResetButton(ref labelNr).GetValue(out bool useCustomSensitivity);
				bool isUseCustomSensitivityDisabled = useCustomSensitivity == false;
				_sensitivity.BeginDisabled(isUseCustomSensitivityDisabled).DragFloat(SLIDER_STEP_0p001, 0.001f, 100f).EndDisabled().ResetButton(ref labelNr);
				_scopeSensitivityMultiplier.BeginDisabled(isUseCustomSensitivityDisabled).DragFloat(SLIDER_STEP_0p001, 0.001f, 10f).EndDisabled().ResetButton(ref labelNr);

				ImGui.TreePop();
			}
		}
	}



	public static class Extensions
	{
		public static T? TryGetComponent<T>(this GameObject gameObject, string typeName) where T : class
		{
			_System.Type? type = _System.Type.GetType(typeName);
			if (type != null)
			{
				Component? componentFromGameObject = gameObject.getComponent(type);
				if (componentFromGameObject != null)
				{
					if (componentFromGameObject is IObject componentIObject)
					{
						return componentIObject.TryAs<T>();
					}
				}
			}

			return null;
		}
	}



	public static class Log
	{
		private const string PREFIX = "[" + SensitivityScalingFixPlugin.GUID + "] ";
		public static void Info(string message)
		{
			API.LogInfo(PREFIX + message);
		}

		public static void Warning(string message)
		{
			API.LogWarning(PREFIX + message);
		}

		public static void Error(string message)
		{
			API.LogError(PREFIX + message);
		}
	}
}