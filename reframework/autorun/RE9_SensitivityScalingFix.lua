--SCRIPT INFO
local s_GUID = "RE9_SensitivityScalingFix"
local s_version = "1.0.1"
local s_GUIDAndVVersion = s_GUID .. " v" .. s_version
local s_logPrefix = "[" .. s_GUIDAndVVersion .. "] "
local s_configFileName = s_GUID .. ".lua.json"



--CONFIG
local tbl_config =
{
	b_enabled = true,
	b_useCustomSensitivity = false,
	f_customSensitivity = 1.0,
	f_scopeSensitivityMultiplier = 1.0,
	f_monitorDistanceHorizontal = 1.0,
}



--HELPER FUNCTIONS
local function get_component(game_object, type_name)
    local t = sdk.typeof(type_name)
    if t ~= nil then
    	return game_object:call("getComponent(System.Type)", t)
	end

	return nil
end

local function GenerateEnum(typename, double_ended)
    local double_ended = double_ended or false

    local t = sdk.find_type_definition(typename)
    if not t then return {} end

    local fields = t:get_fields()
    local enum = {}

    for i, field in ipairs(fields) do
        if field:is_static() then
            local name = field:get_name()
            local raw_value = field:get_data(nil)

            --log.info(name .. " = " .. tostring(raw_value))

            enum[name] = raw_value

            if double_ended then
                enum[raw_value] = name
            end
        end
    end

    return enum
end

local function LoadFromJson()
	local tbl_loadedConfig = json.load_file(s_configFileName)

	if tbl_loadedConfig ~= nil then
        for key, val in pairs(tbl_loadedConfig) do
            tbl_config[key] = val
        end
    end
end

local function SaveToJson()
	json.dump_file(s_configFileName, tbl_config)
end



--LOG
local function LogInfo(message)
	log.info(s_logPrefix .. message)
end



--VARIABLES
local fn_mathTan = math.tan
local fn_mathAtan = math.atan

local f_pi = math.pi
local f_degToRad = f_pi / 180.0

local f_baseSensitivity = 7.0 --Game's internal sensitivity divisor
local f_scopeCamera360SpeedRate = 0.57
local f_baseFOV = 40.0 --Use 40 FOV as base since both characters use 40 FOV in 3rd person
local f_baseFOVRad = f_baseFOV * f_degToRad
local f_baseFOVRadDiv2Tan = fn_mathTan(f_baseFOVRad / 2.0)

local e_appCameraDefineRole = nil

local c_mainCamera = nil
local c_cameraOptionSubSystem = nil
local f_originalScopeCameraSpeedRate = nil
local b_initialized = false



--HOOKS
local function PreGetMouseSensitivity(args)
end

local function PostGetMouseSensitivity(retval)
	local tblConfig = tbl_config

	if tblConfig.b_enabled and b_initialized then
		--Get sensitivity
		local fDesiredSensitivity 
		if tblConfig.b_useCustomSensitivity then
			fDesiredSensitivity = f_baseSensitivity / tblConfig.f_customSensitivity --Divide since the sensitivity is a divisor
		else
			fDesiredSensitivity = sdk.to_float(retval)
		end
		
		--Get FOV and aspect ratio
		local cMainCamera = c_mainCamera
		local fCameraFOV = cMainCamera:call("get_FOV") --FOV is vertical
		local fCameraFOVRad = fCameraFOV * f_degToRad
		local fAspectRatio = cMainCamera:call("get_AspectRatio")

		--Calculate new sensitivity
		local fnMathTan = fn_mathTan
		local fnMathAtan = fn_mathAtan
		
		local fCoefficient = tblConfig.f_monitorDistanceHorizontal * fAspectRatio
		local fBaseFOVRadDiv2Tan = f_baseFOVRadDiv2Tan
		local fScale
		if fCoefficient > 0.0 then
			fScale = fnMathAtan(fCoefficient * fnMathTan(fCameraFOVRad / 2.0)) / fnMathAtan(fCoefficient * fBaseFOVRadDiv2Tan)
		else
			fScale = fnMathTan(fCameraFOVRad / 2.0) / fBaseFOVRadDiv2Tan
		end

		--Set new sensitivity
		local fNewSensitivity = fDesiredSensitivity / fScale --Divide since the sensitivity is a divisor
		return sdk.float_to_ptr(fNewSensitivity)
	end

	return retval
end



--FUNCTIONS
local function TrySetScopeCameraSpeedRate()
	if b_initialized then
		local cCameraOptionSubSystem = c_cameraOptionSubSystem
		local tblConfig = tbl_config
		local fScopeCamera360SpeedRate = f_scopeCamera360SpeedRate
		
		if tblConfig.b_enabled then
			if tblConfig.b_useCustomSensitivity then
				cCameraOptionSubSystem._ScopeCameraSpeedRate = fScopeCamera360SpeedRate * tblConfig.f_scopeSensitivityMultiplier
			else
				cCameraOptionSubSystem._ScopeCameraSpeedRate = fScopeCamera360SpeedRate
			end
		else
			if f_originalScopeCameraSpeedRate ~= nil then
				cCameraOptionSubSystem._ScopeCameraSpeedRate = f_originalScopeCameraSpeedRate
			end
		end
	end
end

local function OnSettingsChanged()
	SaveToJson()
	TrySetScopeCameraSpeedRate()
end



--CALLBACKS
local function PreUpdateBehavior()
	if b_initialized == false then
		--Get CameraSystem
		local cCameraSystem = sdk.get_managed_singleton("app.CameraSystem")
		if cCameraSystem == nil then return end

		--Generate enum after singleton get if nil
		if e_appCameraDefineRole == nil then
			e_appCameraDefineRole = GenerateEnum("app.AppCameraDefine.Role", true)
		end

		if e_appCameraDefineRole == nil then return end

		--Get main Camera if nil
		if c_mainCamera == nil then
			local cCameraGameObject = cCameraSystem:call("getCameraObject", e_appCameraDefineRole.Main)
			if cCameraGameObject ~= nil then
				c_mainCamera = get_component(cCameraGameObject, "via.Camera")
			end
		end

		--Get CameraOptionSubSystem
		if c_cameraOptionSubSystem == nil then
			c_cameraOptionSubSystem = cCameraSystem:call("get_OptionSubSystem")
		end

		--Get hook info
		local tAppCameraInputSubSystem = sdk.find_type_definition("app.CameraInputSubSystem")
		local mGetMouseSensitivity = nil
		if tAppCameraInputSubSystem ~= nil then
			mGetMouseSensitivity = tAppCameraInputSubSystem:get_method("get_MouseSensitivity")
		end

		if c_mainCamera ~= nil and c_cameraOptionSubSystem ~= nil and mGetMouseSensitivity ~= nil then
			f_originalScopeCameraSpeedRate = c_cameraOptionSubSystem:call("get_ScopeCameraSpeedRate")
			b_initialized = true
			TrySetScopeCameraSpeedRate()

			--Create hook
			sdk.hook(mGetMouseSensitivity, PreGetMouseSensitivity, PostGetMouseSensitivity)

			LogInfo("Initialized")
		end
	end
end

local function Unload()
	if b_initialized and f_originalScopeCameraSpeedRate ~= nil then
		c_cameraOptionSubSystem._ScopeCameraSpeedRate = f_originalScopeCameraSpeedRate
	end
end



--MAIN
LoadFromJson()
re.on_pre_application_entry("UpdateBehavior", PreUpdateBehavior)
re.on_script_reset(Unload)
LogInfo("Loaded")



--SCRIPT GENERATED UI
re.on_draw_ui(function()
	if imgui.tree_node(s_GUIDAndVVersion) then
		local bChanged = false
		local tblConfig = tbl_config

		--General
		imgui.text("General")
		bChanged, tblConfig.b_enabled = imgui.checkbox("Enabled", tblConfig.b_enabled)
		if bChanged == true then OnSettingsChanged() end

		bChanged, tblConfig.f_monitorDistanceHorizontal = imgui.drag_float("Monitor Distance Horizontal (MDH)", tblConfig.f_monitorDistanceHorizontal, 0.001, 0.0, 10.0)
		if bChanged == true then OnSettingsChanged() end

		--Custom sensitivity
		imgui.text("Custom sensitivity")
		bChanged, tblConfig.b_useCustomSensitivity = imgui.checkbox("Use Custom Sensitivity", tblConfig.b_useCustomSensitivity)
		if bChanged == true then OnSettingsChanged() end
		local bIsUseCustomSensitivity = tblConfig.b_useCustomSensitivity

		imgui.begin_disabled(bIsUseCustomSensitivity == false)
		bChanged, tblConfig.f_customSensitivity = imgui.drag_float("Custom Sensitivity", tblConfig.f_customSensitivity, 0.001, 0.001, 100.0)
		if bChanged == true then OnSettingsChanged() end

		bChanged, tblConfig.f_scopeSensitivityMultiplier = imgui.drag_float("Scope Sensitivity Multiplier", tblConfig.f_scopeSensitivityMultiplier, 0.001, 0.001, 10.0)
		if bChanged == true then OnSettingsChanged() end 
		imgui.end_disabled()

		imgui.tree_pop()
	end
end)