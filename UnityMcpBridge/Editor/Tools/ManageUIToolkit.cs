using UnityEngine;
using UnityEngine.UIElements; // Required for UI Toolkit
using UnityEditor;
using UnityEditor.UIElements; // For editor-specific UI Toolkit features if needed
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System; // For Enum.Parse and Convert
using System.Linq; // For Linq operations if needed

namespace UnityMcpBridge.Editor.Tools
{
    public static class ManageUIToolkitHandler
    {
        public static object HandleCommand(JObject payload)
        {
            string commandName = payload["command"]?.ToString();
            if (string.IsNullOrEmpty(commandName))
            {
                return new Dictionary<string, object> {
                    { "success", false },
                    { "message", "Command not specified for ManageUIToolkitHandler." }
                };
            }

            var commandData = payload.ToObject<Dictionary<string, object>>();
            // It can be beneficial to pass the JObject payload directly to some handlers
            // if they need to parse complex nested objects or handle type variations flexibly.

            switch (commandName)
            {
                case "create_ui_toolkit_panel":
                    return CreateUIToolkitPanel(commandData);
                case "create_label": 
                    return CreateLabel(commandData);
                case "create_button": 
                    return CreateButton(commandData);
                case "create_textfield": 
                    return CreateTextField(commandData);
                case "set_element_style":
                    {
                        var response = new Dictionary<string, object>();
                        try {
                            VisualElement el = FindElementFromCommand(commandData, out string err);
                            if (el == null) { 
                                response["success"] = false;
                                response["message"] = err ?? "Element not found for set_element_style.";
                                return response; 
                            }
                            if (commandData.TryGetValue("style_properties", out var styles) && styles is JObject stylesJ) {
                                ApplyStyleProperties(el, stylesJ.ToObject<Dictionary<string, object>>());
                                response["success"] = true; 
                                response["message"] = "Styles applied successfully to element.";
                            } else { 
                                response["success"] = false;
                                response["message"] = "style_properties not provided or in incorrect format for set_element_style.";
                            }
                        } catch (Exception e) {
                            response["success"] = false;
                            response["message"] = $"Error in set_element_style: {e.Message} {e.StackTrace}";
                        }
                        return response;
                    }
                case "set_text": 
                    return SetText(commandData);
                case "add_class": 
                    return AddClass(commandData);
                case "remove_class": 
                    return RemoveClass(commandData);
                case "create_toggle":
                    return CreateToggle(commandData);
                case "create_slider":
                    return CreateSlider(commandData);
                case "set_toggle_value":
                    return SetToggleValue(commandData);
                case "get_toggle_value":
                    return GetToggleValue(commandData);
                case "set_slider_value":
                    return SetSliderValue(commandData);
                case "get_slider_value":
                    return GetSliderValue(commandData);
                case "load_uxml":
                    return LoadUXML(commandData);
                case "set_element_enabled":
                    return SetElementEnabled(commandData);
                case "set_element_visibility":
                    return SetElementVisibility(commandData);
                case "delete_element":
                    return DeleteElement(commandData);
                case "set_parent":
                    return SetParent(commandData);
                case "button_simulate_click":
                    return ButtonSimulateClick(commandData);
                default:
                    return new Dictionary<string, object> {
                        { "success", false },
                        { "message", $"Unknown command '{commandName}' for ManageUIToolkitHandler." }
                    };
            }
        }

        private static string GenerateQueryPath(VisualElement element) {
            if (element == null || string.IsNullOrEmpty(element.name)) return null;
            // Ensure names are usable as UQuery selectors (e.g. no spaces, starts with letter or # for ID)
            // For simplicity, we assume names are set to be valid selectors.
            // Prepending # to make it an ID selector is a good practice.
            return $"#{element.name}";
        }

        private static VisualElement GetRootVisualElement(string panelName, out string errorMessage, bool autoCreateIfMissing = false)
        {
            errorMessage = null;
            if (string.IsNullOrEmpty(panelName))
            {
                errorMessage = "Panel name is required to find UIDocument.";
                return null;
            }

            GameObject panelGO = GameObject.Find(panelName);
            if (panelGO == null)
            {
                if (autoCreateIfMissing) {
                    // This case is handled by CreateUIToolkitPanel, GetRootVisualElement is for existing panels.
                    errorMessage = $"GameObject '{panelName}' not found and autoCreateIfMissing is false for GetRootVisualElement.";
                    return null;
                }
                errorMessage = $"GameObject '{panelName}' not found.";
                return null;
            }

            UIDocument uiDocument = panelGO.GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                errorMessage = $"No UIDocument component found on GameObject '{panelName}'.";
                return null;
            }
            
            if (uiDocument.rootVisualElement == null) {
                 errorMessage = $"UIDocument on '{panelName}' has a null rootVisualElement. It might not be initialized yet.";
                 return null;
            }
            return uiDocument.rootVisualElement;
        }
        
        // Helper to find a VisualElement within a root, using a simple name query for now.
        // queryPath could be extended to support "#id" or ".class" later.
        private static VisualElement FindVisualElement(VisualElement root, string queryPath, out string errorMessage) {
            errorMessage = null;
            if (root == null || string.IsNullOrEmpty(queryPath)) {
                errorMessage = "Root VisualElement or query path is null/empty.";
                return null;
            }
            // Simple name query for now. Can be expanded.
            // For nested queries like "Parent/Child", this would need to be recursive.
            // For now, assume queryPath is a direct child name or an ID starting with #
            VisualElement foundElement = null;
            if (queryPath.StartsWith("#")) {
                 foundElement = root.Q(name: queryPath.Substring(1)); // Query by name if it's an ID like #myElement
            } else {
                 foundElement = root.Q<VisualElement>(queryPath); // Query by name or type
            }


            if (foundElement == null) {
                errorMessage = $"VisualElement not found for query '{queryPath}'.";
            }
            return foundElement;
        }


        private static Dictionary<string, object> CreateUIToolkitPanel(Dictionary<string, object> command)
        {
            var response = new Dictionary<string, object>();
            try
            {
                string panelGOName = command.TryGetValue("panel_name", out var pn) ? (string)pn : "NewUIToolkitPanel";
                string panelSettingsPath = command.TryGetValue("panel_settings_path", out var psp) ? (string)psp : null;
                string uxmlPath = command.TryGetValue("uxml_asset_path", out var up) ? (string)up : null;

                if (GameObject.Find(panelGOName) != null)
                {
                    response["success"] = false;
                    response["message"] = $"GameObject '{panelGOName}' already exists. Cannot create panel.";
                    return response;
                }

                GameObject panelGO = new GameObject(panelGOName);
                UIDocument uiDocument = panelGO.AddComponent<UIDocument>();

                if (!string.IsNullOrEmpty(panelSettingsPath)) {
                    PanelSettings settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelSettingsPath);
                    if (settings != null) uiDocument.panelSettings = settings;
                    else response["warning_panel_settings"] = $"PanelSettings asset not found at '{panelSettingsPath}'.";
                }

                if (!string.IsNullOrEmpty(uxmlPath)) {
                    VisualTreeAsset vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
                    if (vta != null) uiDocument.visualTreeAsset = vta; // This will clone the UXML into the rootVisualElement
                    else response["warning_uxml"] = $"VisualTreeAsset not found at '{uxmlPath}'.";
                }
                
                // Ensure rootVisualElement is available for further operations if no VTA was assigned or it was empty
                if (uiDocument.rootVisualElement == null && uxmlPath == null) {
                    // If no VTA is assigned, UIDocument might not create rootVisualElement immediately or it might be minimal.
                    // For a purely C# driven UI, we'd add to its root.
                    // It's generally ready after UIDocument.OnEnable. Forcing it might not be standard.
                    // Let's assume it's available for now or will be when elements are added.
                }

                Undo.RegisterCreatedObjectUndo(panelGO, "Create UI Toolkit Panel");
                response["success"] = true;
                response["message"] = $"UI Toolkit Panel (UIDocument on GameObject '{panelGOName}') created.";
                response["panel_gameobject_path"] = GetGameObjectPath(panelGO); // Use existing helper
            }
            catch (Exception e)
            {
                response["success"] = false;
                response["message"] = $"Error creating UI Toolkit Panel: {e.Message} {e.StackTrace}";
            }
            return response;
        }
        
        // Using the same GetGameObjectPath from ManageUGUIHandler (if in same assembly)
        // or define it here if it's separate. For this subtask, assume it might need to be redefined or made public.
        // For now, let's add a local copy for clarity for the subtask.
        private static string GetGameObjectPath(GameObject obj)
        {
            if (obj == null) return null;
            string path = "/" + obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path.TrimStart('/');
        }
        
        // Placeholder for ApplyStyleProperties, CreateLabel, CreateButton etc.

        private static StyleLength ConvertToStyleLength(object value)
        {
            if (value is string s) {
                if (s.ToLower() == "auto") return new StyleLength(StyleKeyword.Auto);
                if (s.EndsWith("px")) return new StyleLength(Convert.ToSingle(s.Replace("px", "")));
                if (s.EndsWith("%")) return new StyleLength(new Length(Convert.ToSingle(s.Replace("%", "")), LengthUnit.Percent));
                // Potentially add other units or keyword checks. Try to parse as float if no unit.
                if (float.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float fVal)) {
                    return new StyleLength(fVal);
                }
                Debug.LogWarning($"[ManageUIToolkitHandler] Could not parse '{s}' as StyleLength. Defaulting to Initial.");
                return StyleKeyword.Initial; 
            }
            if (value is JValue jVal && (jVal.Type == JTokenType.Integer || jVal.Type == JTokenType.Float)) {
                return new StyleLength(Convert.ToSingle(jVal.Value));
            }
            if (value is int || value is float || value is double) {
                return new StyleLength(Convert.ToSingle(value));
            }
            Debug.LogWarning($"[ManageUIToolkitHandler] Could not parse value of type '{value?.GetType()}' as StyleLength. Defaulting to Initial.");
            return StyleKeyword.Initial; // Default fallback
        }

        private static void ApplyStyleProperties(VisualElement element, Dictionary<string, object> styles)
        {
            if (element == null || styles == null) return;
            var style = element.style;

            foreach (var pair in styles)
            {
                try {
                    // Basic direct properties, simple types
                    if (pair.Key.Equals("width", StringComparison.OrdinalIgnoreCase) && pair.Value != null) style.width = ConvertToStyleLength(pair.Value);
                    else if (pair.Key.Equals("height", StringComparison.OrdinalIgnoreCase) && pair.Value != null) style.height = ConvertToStyleLength(pair.Value);
                    else if (pair.Key.Equals("minWidth", StringComparison.OrdinalIgnoreCase) && pair.Value != null) style.minWidth = ConvertToStyleLength(pair.Value);
                    else if (pair.Key.Equals("minHeight", StringComparison.OrdinalIgnoreCase) && pair.Value != null) style.minHeight = ConvertToStyleLength(pair.Value);
                    else if (pair.Key.Equals("maxWidth", StringComparison.OrdinalIgnoreCase) && pair.Value != null) style.maxWidth = ConvertToStyleLength(pair.Value);
                    else if (pair.Key.Equals("maxHeight", StringComparison.OrdinalIgnoreCase) && pair.Value != null) style.maxHeight = ConvertToStyleLength(pair.Value);
                    else if (pair.Key.Equals("flexGrow", StringComparison.OrdinalIgnoreCase) && pair.Value != null) style.flexGrow = Convert.ToSingle(pair.Value);
                    else if (pair.Key.Equals("flexShrink", StringComparison.OrdinalIgnoreCase) && pair.Value != null) style.flexShrink = Convert.ToSingle(pair.Value);
                    else if (pair.Key.Equals("flexBasis", StringComparison.OrdinalIgnoreCase) && pair.Value != null) style.flexBasis = ConvertToStyleLength(pair.Value);
                    else if (pair.Key.Equals("position", StringComparison.OrdinalIgnoreCase) && pair.Value is string posStr) {
                        if (Enum.TryParse<Position>(posStr, true, out var posEnum)) style.position = posEnum;
                    }
                    else if (pair.Key.Equals("left", StringComparison.OrdinalIgnoreCase) && pair.Value != null) style.left = ConvertToStyleLength(pair.Value);
                    else if (pair.Key.Equals("top", StringComparison.OrdinalIgnoreCase) && pair.Value != null) style.top = ConvertToStyleLength(pair.Value);
                    else if (pair.Key.Equals("right", StringComparison.OrdinalIgnoreCase) && pair.Value != null) style.right = ConvertToStyleLength(pair.Value);
                    else if (pair.Key.Equals("bottom", StringComparison.OrdinalIgnoreCase) && pair.Value != null) style.bottom = ConvertToStyleLength(pair.Value);
                    else if (pair.Key.Equals("backgroundColor", StringComparison.OrdinalIgnoreCase) && pair.Value is JObject cJobjBg) {
                        style.backgroundColor = new StyleColor(new Color(
                            Convert.ToSingle(cJobjBg["r"]), Convert.ToSingle(cJobjBg["g"]),
                            Convert.ToSingle(cJobjBg["b"]), Convert.ToSingle(cJobjBg["a"])
                        ));
                    }
                    else if (pair.Key.Equals("color", StringComparison.OrdinalIgnoreCase) && pair.Value is JObject cJobjText) { // For text color
                        style.color = new StyleColor(new Color(
                            Convert.ToSingle(cJobjText["r"]), Convert.ToSingle(cJobjText["g"]),
                            Convert.ToSingle(cJobjText["b"]), Convert.ToSingle(cJobjText["a"])
                        ));
                    }
                    else if (pair.Key.Equals("fontSize", StringComparison.OrdinalIgnoreCase) && pair.Value != null) style.fontSize = ConvertToStyleLength(pair.Value);
                    else if (pair.Key.Equals("unityTextAlign", StringComparison.OrdinalIgnoreCase) && pair.Value is string alignStr) {
                        if (Enum.TryParse<TextAnchor>(alignStr, true, out var alignEnum)) style.unityTextAlign = alignEnum;
                    }
                    // Add more style properties as needed (e.g., margin, padding, border, etc.)
                } catch (Exception ex) {
                    Debug.LogWarning($"[ManageUIToolkitHandler] Error applying style '{pair.Key}' with value '{pair.Value}': {ex.Message}");
                }
            }
        }

        private static VisualElement GetParentElement(Dictionary<string, object> command, out string errorMessage) {
            errorMessage = null;
            if (!command.TryGetValue("panel_name", out var panelNameObj) || !(panelNameObj is string panelName) || string.IsNullOrEmpty(panelName)) {
                errorMessage = "Panel name not provided or invalid.";
                return null;
            }

            VisualElement root = GetRootVisualElement(panelName, out errorMessage);
            if (root == null) return null; // errorMessage will be set by GetRootVisualElement
            
            VisualElement parentElement = root; // Default to root if no specific parent path
            if (command.TryGetValue("parent_element_query_path", out var parentQueryPathObj) && parentQueryPathObj is string parentQueryPath && !string.IsNullOrEmpty(parentQueryPath)) {
                parentElement = FindVisualElement(root, parentQueryPath, out errorMessage);
                if (parentElement == null) return null; // errorMessage will be set by FindVisualElement
            }
            return parentElement;
        }

        private static void ApplyStylesAndClasses(VisualElement element, Dictionary<string, object> command) {
            if (command.TryGetValue("style_properties", out var stylesObj) && stylesObj is JObject stylesJObj) {
                ApplyStyleProperties(element, stylesJObj.ToObject<Dictionary<string, object>>());
            }
            if (command.TryGetValue("classes", out var classesObj) && classesObj is JArray classesArray) {
                foreach (var classNameToken in classesArray) {
                    if (classNameToken is JValue classNameVal && classNameVal.Value is string classNameStr) {
                        element.AddToClassList(classNameStr);
                    }
                }
            }
        }
        
        private static VisualElement FindElementFromCommand(Dictionary<string, object> command, out string errorMessage) {
            errorMessage = null;
             if (!command.TryGetValue("panel_name", out var panelNameObj) || !(panelNameObj is string panelName) || string.IsNullOrEmpty(panelName)) {
                errorMessage = "Panel name not provided or invalid.";
                return null;
            }
            if (!command.TryGetValue("element_query_path", out var queryPathObj) || !(queryPathObj is string elementQueryPath) || string.IsNullOrEmpty(elementQueryPath)) {
                errorMessage = "Element query path not provided or invalid.";
                return null;
            }

            VisualElement root = GetRootVisualElement(panelName, out errorMessage);
            if (root == null) return null;
            
            return FindVisualElement(root, elementQueryPath, out errorMessage);
        }

        private static Dictionary<string, object> CreateLabel(Dictionary<string, object> command) {
            var response = new Dictionary<string, object>();
            try {
                VisualElement parent = GetParentElement(command, out string error);
                if (parent == null) { 
                    response["success"] = false;
                    response["message"] = error ?? "Failed to find parent element for Label.";
                    return response; 
                }

                string name = command.TryGetValue("name", out var n) ? (string)n : null;
                string text = command.TryGetValue("text", out var t) ? (string)t : "NewLabel";
                
                Label label = new Label(text);
                if (!string.IsNullOrEmpty(name)) {
                    label.name = name;
                } else {
                    label.name = "Label_" + Guid.NewGuid().ToString().Substring(0,8); // Assign a unique name if not provided
                }
                parent.Add(label);
                
                ApplyStylesAndClasses(label, command);
                response["success"] = true; 
                response["message"] = $"Label '{label.name}' created successfully.";
                response["element_query_path"] = GenerateQueryPath(label);
            } catch (Exception e) { 
                response["success"] = false;
                response["message"] = $"Error creating Label: {e.Message} {e.StackTrace}";
            }
            return response;
        }

        private static Dictionary<string, object> CreateButton(Dictionary<string, object> command) {
            var response = new Dictionary<string, object>();
            try {
                VisualElement parent = GetParentElement(command, out string error);
                if (parent == null) { 
                    response["success"] = false;
                    response["message"] = error ?? "Failed to find parent element for Button.";
                    return response; 
                }

                string name = command.TryGetValue("name", out var n) ? (string)n : null;
                string text = command.TryGetValue("text", out var t) ? (string)t : "NewButton";

                Button button = new Button(() => { Debug.Log($"Button '{name ?? "Unnamed"}' clicked via MCP (debug log)."); });
                button.text = text;
                if (!string.IsNullOrEmpty(name)) {
                     button.name = name;
                } else {
                    button.name = "Button_" + Guid.NewGuid().ToString().Substring(0,8);
                }
                parent.Add(button);

                ApplyStylesAndClasses(button, command);
                response["success"] = true; 
                response["message"] = $"Button '{button.name}' created successfully.";
                response["element_query_path"] = GenerateQueryPath(button);
            } catch (Exception e) { 
                response["success"] = false;
                response["message"] = $"Error creating Button: {e.Message} {e.StackTrace}";
            }
            return response;
        }

        private static Dictionary<string, object> CreateTextField(Dictionary<string, object> command) {
            var response = new Dictionary<string, object>();
            try {
                VisualElement parent = GetParentElement(command, out string error);
                if (parent == null) { 
                    response["success"] = false;
                    response["message"] = error ?? "Failed to find parent element for TextField.";
                    return response; 
                }

                string name = command.TryGetValue("name", out var n) ? (string)n : null;
                string labelText = command.TryGetValue("label_text", out var lt) ? (string)lt : null;
                string initialValue = command.TryGetValue("initial_value_str", out var iv) ? (string)iv : "";
                bool multiline = command.TryGetValue("multiline", out var ml) ? Convert.ToBoolean(ml) : false;

                TextField textField = new TextField(labelText); // Label can be null
                textField.value = initialValue;
                textField.multiline = multiline;

                if (!string.IsNullOrEmpty(name)) {
                    textField.name = name;
                } else {
                    textField.name = "TextField_" + Guid.NewGuid().ToString().Substring(0,8);
                }
                parent.Add(textField);
                
                ApplyStylesAndClasses(textField, command);
                response["success"] = true; 
                response["message"] = $"TextField '{textField.name}' created successfully.";
                response["element_query_path"] = GenerateQueryPath(textField);
            } catch (Exception e) { 
                response["success"] = false;
                response["message"] = $"Error creating TextField: {e.Message} {e.StackTrace}";
            }
            return response;
        }
        
        private static Dictionary<string, object> SetText(Dictionary<string, object> command) {
            var response = new Dictionary<string, object>();
            try {
                VisualElement element = FindElementFromCommand(command, out string error);
                if (element == null) { 
                    response["success"] = false;
                    response["message"] = error ?? "Element not found for SetText.";
                    return response; 
                }
                
                if (!command.TryGetValue("text", out var textObj) || !(textObj is string text)) {
                    response["success"] = false;
                    response["message"] = "Text content not provided or invalid for SetText.";
                    return response;
                }

                if (element is TextElement te) { // Covers Label, Button
                    te.text = text;
                    response["success"] = true; 
                    response["message"] = $"Text set for element '{element.name}'.";
                } else if (element is TextField tf) {
                    tf.value = text;
                    response["success"] = true; 
                    response["message"] = $"Value set for TextField '{element.name}'.";
                } else { 
                    response["success"] = false;
                    response["message"] = $"Element '{element.name}' is not a TextElement or TextField, cannot set text.";
                }
            } catch (Exception e) { 
                response["success"] = false;
                response["message"] = $"Error in SetText: {e.Message} {e.StackTrace}";
            }
            return response;
        }

        private static Dictionary<string, object> AddClass(Dictionary<string, object> command) {
            var response = new Dictionary<string, object>();
            try {
                VisualElement element = FindElementFromCommand(command, out string error);
                if (element == null) { 
                    response["success"] = false;
                    response["message"] = error ?? "Element not found for AddClass.";
                    return response; 
                }
                if (!command.TryGetValue("class_name", out var classNameObj) || !(classNameObj is string className) || string.IsNullOrEmpty(className)) {
                    response["success"] = false;
                    response["message"] = "Class name not provided or empty for AddClass.";
                    return response;
                }
                
                element.AddToClassList(className);
                response["success"] = true; 
                response["message"] = $"Class '{className}' added to element '{element.name}'.";
            } catch (Exception e) { 
                response["success"] = false;
                response["message"] = $"Error in AddClass: {e.Message} {e.StackTrace}";
            }
            return response;
        }

        private static Dictionary<string, object> RemoveClass(Dictionary<string, object> command) {
            var response = new Dictionary<string, object>();
            try {
                VisualElement element = FindElementFromCommand(command, out string error);
                if (element == null) { 
                    response["success"] = false;
                    response["message"] = error ?? "Element not found for RemoveClass.";
                    return response; 
                }
                if (!command.TryGetValue("class_name", out var classNameObj) || !(classNameObj is string className) || string.IsNullOrEmpty(className)) {
                    response["success"] = false;
                    response["message"] = "Class name not provided or empty for RemoveClass.";
                    return response;
                }

                element.RemoveFromClassList(className);
                response["success"] = true; 
                response["message"] = $"Class '{className}' removed from element '{element.name}'.";
            } catch (Exception e) { 
                response["success"] = false;
                response["message"] = $"Error in RemoveClass: {e.Message} {e.StackTrace}";
            }
            return response;
        }

    }

    // Add CreateToggle, CreateSlider, SetToggleValue, GetToggleValue, SetSliderValue, GetSliderValue methods here
    private static Dictionary<string, object> CreateToggle(Dictionary<string, object> command) {
        var response = new Dictionary<string, object>();
        try {
            VisualElement parent = GetParentElement(command, out string error);
            if (parent == null) { 
                response["success"] = false;
                response["message"] = error ?? "Failed to find parent element for Toggle.";
                return response; 
            }

            string name = command.TryGetValue("name", out var n) ? (string)n : null;
            string labelText = command.TryGetValue("label_text", out var lt) ? (string)lt : "Toggle";
            bool initialValue = command.TryGetValue("initial_value_bool", out var iv) ? Convert.ToBoolean(iv) : false;

            Toggle toggle = new Toggle(labelText);
            toggle.value = initialValue;
            if (!string.IsNullOrEmpty(name)) {
                toggle.name = name;
            } else {
                toggle.name = "Toggle_" + Guid.NewGuid().ToString().Substring(0,8);
            }
            parent.Add(toggle);
            
            ApplyStylesAndClasses(toggle, command);
            response["success"] = true;
            response["message"] = $"Toggle '{toggle.name}' created with label '{toggle.label}'.";
            response["element_query_path"] = GenerateQueryPath(toggle);
        } catch (Exception e) { 
            response["success"] = false;
            response["message"] = $"Error creating Toggle: {e.Message} {e.StackTrace}";
        }
        return response;
    }

    private static Dictionary<string, object> CreateSlider(Dictionary<string, object> command) {
        var response = new Dictionary<string, object>();
        try {
            VisualElement parent = GetParentElement(command, out string error);
            if (parent == null) { 
                response["success"] = false;
                response["message"] = error ?? "Failed to find parent element for Slider.";
                return response; 
            }

            string name = command.TryGetValue("name", out var n) ? (string)n : null;
            string labelText = command.TryGetValue("label_text", out var lt) ? (string)lt : "Slider";
            float lowVal = command.TryGetValue("low_value", out var lv) ? Convert.ToSingle(lv) : 0f;
            float highVal = command.TryGetValue("high_value", out var hv) ? Convert.ToSingle(hv) : 10f;
            float initialValue = command.TryGetValue("initial_value_float", out var iv) ? Convert.ToSingle(iv) : lowVal;
            string directionStr = command.TryGetValue("direction", out var d) ? (string)d : "Horizontal";
            
            if(!Enum.TryParse<SliderDirection>(directionStr, true, out SliderDirection dirEnum)) {
                dirEnum = SliderDirection.Horizontal; // Default if parse fails
                response["warning"] = $"Invalid Slider direction: '{directionStr}'. Defaulting to Horizontal.";
            }

            Slider slider = new Slider(labelText, lowVal, highVal, dirEnum, Slider.kDefaultPageSize);
            slider.value = initialValue;
            if (!string.IsNullOrEmpty(name)) {
                slider.name = name;
            } else {
                slider.name = "Slider_" + Guid.NewGuid().ToString().Substring(0,8);
            }
            parent.Add(slider);
            
            ApplyStylesAndClasses(slider, command);
            response["success"] = true;
            response["message"] = $"Slider '{slider.name}' created.";
            response["element_query_path"] = GenerateQueryPath(slider);
        } catch (Exception e) { 
            response["success"] = false;
            response["message"] = $"Error creating Slider: {e.Message} {e.StackTrace}";
        }
        return response;
    }

    private static Dictionary<string, object> SetToggleValue(Dictionary<string, object> command) {
        var response = new Dictionary<string, object>();
        try {
            Toggle toggle = FindElementFromCommand(command, out string error) as Toggle;
            if (toggle == null) { 
                response["success"] = false;
                response["message"] = error ?? $"Element not found or not a Toggle for SetToggleValue.";
                return response; 
            }
            
            if (!command.TryGetValue("initial_value_bool", out var valObj) || valObj == null) {
                 response["success"] = false; response["message"] = "initial_value_bool not provided for SetToggleValue."; return response;
            }
            bool value = Convert.ToBoolean(valObj);
            toggle.value = value;
            response["success"] = true; 
            response["message"] = $"Toggle '{toggle.name}' value set to {value}.";
        } catch (Exception e) { 
            response["success"] = false;
            response["message"] = $"Error in SetToggleValue: {e.Message} {e.StackTrace}";
        }
        return response;
    }

    private static Dictionary<string, object> GetToggleValue(Dictionary<string, object> command) {
        var response = new Dictionary<string, object>();
        try {
            Toggle toggle = FindElementFromCommand(command, out string error) as Toggle;
            if (toggle == null) { 
                response["success"] = false;
                response["message"] = error ?? $"Element not found or not a Toggle for GetToggleValue.";
                return response; 
            }
            response["success"] = true;
            response["value"] = toggle.value; // Python side expects "value", but for bool, "is_on" might be clearer mapping. For now, stick to "value".
            response["message"] = $"Toggle '{toggle.name}' value retrieved.";
        } catch (Exception e) { 
            response["success"] = false;
            response["message"] = $"Error in GetToggleValue: {e.Message} {e.StackTrace}";
        }
        return response;
    }

    private static Dictionary<string, object> SetSliderValue(Dictionary<string, object> command) {
        var response = new Dictionary<string, object>();
        try {
            Slider slider = FindElementFromCommand(command, out string error) as Slider;
            if (slider == null) { 
                response["success"] = false;
                response["message"] = error ?? $"Element not found or not a Slider for SetSliderValue.";
                return response; 
            }
            
            if (!command.TryGetValue("initial_value_float", out var valObj) || valObj == null) {
                 response["success"] = false; response["message"] = "initial_value_float not provided for SetSliderValue."; return response;
            }
            slider.value = Convert.ToSingle(valObj);
            response["success"] = true; 
            response["message"] = $"Slider '{slider.name}' value set to {slider.value}.";
        } catch (Exception e) { 
            response["success"] = false;
            response["message"] = $"Error in SetSliderValue: {e.Message} {e.StackTrace}";
        }
        return response;
    }

    private static Dictionary<string, object> GetSliderValue(Dictionary<string, object> command) {
        var response = new Dictionary<string, object>();
        try {
            Slider slider = FindElementFromCommand(command, out string error) as Slider;
            if (slider == null) { 
                response["success"] = false;
                response["message"] = error ?? $"Element not found or not a Slider for GetSliderValue.";
                return response; 
            }
            response["success"] = true;
            response["value"] = slider.value;
            response["message"] = $"Slider '{slider.name}' value retrieved.";
        } catch (Exception e) { 
            response["success"] = false;
            response["message"] = $"Error in GetSliderValue: {e.Message} {e.StackTrace}";
        }
        return response;
    }

    private static Dictionary<string, object> LoadUXML(Dictionary<string, object> command) {
        var response = new Dictionary<string, object>();
        try {
            VisualElement parent = GetParentElement(command, out string error);
            if (parent == null) { 
                response["success"] = false;
                response["message"] = error ?? "Parent element not found for LoadUXML.";
                return response; 
            }

            if (!command.TryGetValue("uxml_asset_path", out var uxmlPathObj) || !(uxmlPathObj is string uxmlPath) || string.IsNullOrEmpty(uxmlPath)) {
                 response["success"] = false;
                 response["message"] = "UXML asset path not provided or empty for LoadUXML.";
                 return response;
            }

            VisualTreeAsset vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (vta == null) { 
                response["success"] = false;
                response["message"] = $"VisualTreeAsset not found at path: {uxmlPath}";
                return response; 
            }

            VisualElement clonedTree = vta.Instantiate(); 
            if (clonedTree != null) {
                parent.Add(clonedTree);
                response["success"] = true;
                response["message"] = $"UXML from '{uxmlPath}' loaded into parent '{parent.name}'.";
                // If the cloned tree's root has a name, we can return a query path for it.
                // This is helpful if the UXML defines a single root element with a name.
                if (!string.IsNullOrEmpty(clonedTree.name)) {
                    response["element_query_path"] = GenerateQueryPath(clonedTree);
                } else if (clonedTree.childCount > 0 && !string.IsNullOrEmpty(clonedTree[0].name)) {
                     // Fallback if the template is wrapped in a container without a name itself
                    response["element_query_path"] = GenerateQueryPath(clonedTree[0]);
                     response["message"] += " Query path refers to the first named child of the UXML root.";
                }

            } else { 
                response["success"] = false;
                response["message"] = $"Failed to instantiate UXML from '{uxmlPath}'.";
            }
        } catch (Exception e) { 
            response["success"] = false;
            response["message"] = $"Error in LoadUXML: {e.Message} {e.StackTrace}";
        }
        return response;
    }

    private static Dictionary<string, object> SetElementEnabled(Dictionary<string, object> command) {
        var response = new Dictionary<string, object>();
        try {
            VisualElement element = FindElementFromCommand(command, out string error);
            if (element == null) { 
                response["success"] = false;
                response["message"] = error ?? "Element not found for SetElementEnabled.";
                return response; 
            }
            
            if (!command.TryGetValue("is_enabled", out var isEnabledObj) || isEnabledObj == null) {
                response["success"] = false;
                response["message"] = "is_enabled flag not provided for SetElementEnabled.";
                return response;
            }
            bool isEnabled = Convert.ToBoolean(isEnabledObj);
            element.SetEnabled(isEnabled);
            response["success"] = true; 
            response["message"] = $"Element '{element.name}' enabled state set to {isEnabled}.";
        } catch (Exception e) { 
            response["success"] = false;
            response["message"] = $"Error in SetElementEnabled: {e.Message} {e.StackTrace}";
        }
        return response;
    }

    private static Dictionary<string, object> SetElementVisibility(Dictionary<string, object> command) {
        var response = new Dictionary<string, object>();
        try {
            VisualElement element = FindElementFromCommand(command, out string error);
            if (element == null) { 
                response["success"] = false;
                response["message"] = error ?? "Element not found for SetElementVisibility.";
                return response; 
            }

            if (!command.TryGetValue("is_visible", out var isVisibleObj) || isVisibleObj == null) {
                response["success"] = false;
                response["message"] = "is_visible flag not provided for SetElementVisibility.";
                return response;
            }
            bool isVisible = Convert.ToBoolean(isVisibleObj);
            element.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            response["success"] = true; 
            response["message"] = $"Element '{element.name}' visibility set to {isVisible}.";
        } catch (Exception e) { 
            response["success"] = false;
            response["message"] = $"Error in SetElementVisibility: {e.Message} {e.StackTrace}";
        }
        return response;
    }

    private static Dictionary<string, object> DeleteElement(Dictionary<string, object> command) {
        var response = new Dictionary<string, object>();
        try {
            VisualElement element = FindElementFromCommand(command, out string error);
            if (element == null) { 
                response["success"] = false; // Or true if "already deleted" is success
                response["message"] = error ?? "Element not found for deletion.";
                return response; 
            }

            if (element.parent != null) {
                element.RemoveFromHierarchy(); 
                response["success"] = true; 
                response["message"] = $"Element '{element.name}' removed from hierarchy.";
            } else { 
                response["success"] = false;
                response["message"] = $"Element '{element.name}' has no parent or is not in a hierarchy. Cannot remove.";
            }
        } catch (Exception e) { 
            response["success"] = false;
            response["message"] = $"Error in DeleteElement: {e.Message} {e.StackTrace}";
        }
        return response;
    }

    private static Dictionary<string, object> SetParent(Dictionary<string, object> command) {
        var response = new Dictionary<string, object>();
        try {
            VisualElement element = FindElementFromCommand(command, out string errorElement);
            if (element == null) { 
                response["success"] = false;
                response["message"] = errorElement ?? "Element to reparent not found.";
                return response; 
            }

            if (!command.TryGetValue("panel_name", out var panelNameObj) || !(panelNameObj is string panelName) || string.IsNullOrEmpty(panelName)) {
                response["success"] = false;
                response["message"] = "Panel name not provided for SetParent operation.";
                return response;
            }
            if (!command.TryGetValue("new_parent_query_path", out var newParentQueryPathObj) || !(newParentQueryPathObj is string newParentQueryPath) || string.IsNullOrEmpty(newParentQueryPath)) {
                response["success"] = false;
                response["message"] = "New parent query path not provided for SetParent operation.";
                return response;
            }
            
            VisualElement root = GetRootVisualElement(panelName, out string errorRoot);
            if (root == null) { 
                response["success"] = false;
                response["message"] = errorRoot ?? $"Root for panel '{panelName}' not found.";
                return response; 
            }
            
            VisualElement newParent = FindVisualElement(root, newParentQueryPath, out string errorNewParent);
            if (newParent == null) { 
                response["success"] = false;
                response["message"] = errorNewParent ?? $"New parent element not found with query '{newParentQueryPath}'.";
                return response; 
            }

            if (element == newParent || element.Contains(newParent)) {
                response["success"] = false;
                response["message"] = "Cannot parent an element to itself or its own descendant.";
                return response;
            }

            newParent.Add(element); // Add will move it if it's already parented elsewhere in the same hierarchy.
            response["success"] = true; 
            response["message"] = $"Element '{element.name}' successfully reparented to '{newParent.name}'.";
        } catch (Exception e) { 
            response["success"] = false;
            response["message"] = $"Error in SetParent: {e.Message} {e.StackTrace}";
        }
        return response;
    }

    private static Dictionary<string, object> ButtonSimulateClick(Dictionary<string, object> command) {
        var response = new Dictionary<string, object>();
        try {
            Button button = FindElementFromCommand(command, out string error) as Button;
            if (button == null) { 
                response["success"] = false;
                response["message"] = error ?? $"Element not found or not a Button for ButtonSimulateClick.";
                return response; 
            }

            using (var evt = ClickEvent.GetPooled()) {
                evt.target = button;
                button.SendEvent(evt);
            }
            response["success"] = true;
            response["message"] = $"Simulated click on button '{button.name}'.";

        } catch (Exception e) { 
            response["success"] = false;
            response["message"] = $"Error in ButtonSimulateClick: {e.Message} {e.StackTrace}";
        }
        return response;
    }
}
