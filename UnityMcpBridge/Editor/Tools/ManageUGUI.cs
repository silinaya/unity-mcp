using UnityEngine;
using UnityEngine.UI; // Required for UGUI elements like Canvas, Text, Button etc.
using UnityEditor;    // Required for AssetDatabase, PrefabUtility etc.
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System; // Required for Enum.Parse

namespace UnityMcpBridge.Editor.Tools
{
    public static class ManageUGUIHandler
    {
        public static object HandleCommand(JObject payload)
        {
            string commandName = payload["command"]?.ToString();
            if (string.IsNullOrEmpty(commandName))
            {
                return new Dictionary<string, object> {
                    { "success", false },
                    { "message", "Command not specified in payload for ManageUGUIHandler." }
                };
            }

            // Convert JObject to Dictionary for easier use in specific handlers
            // Some handlers might prefer to work with JObject directly if types are complex
            var commandData = payload.ToObject<Dictionary<string, object>>();

            switch (commandName)
            {
                case "create_canvas":
                    return CreateCanvas(commandData);
                case "create_panel":
                    return CreatePanel(commandData);
                case "create_image":
                    return CreateImage(commandData);
                case "create_text":
                    return CreateText(commandData);
                case "create_button":
                    return CreateButton(commandData);
                case "set_rect_transform":
                    return SetRectTransform(commandData);
                case "set_parent":
                    return SetParent(commandData);
                case "set_active":
                    return SetActive(commandData);
                case "delete_element":
                    return DeleteElement(commandData);
                case "set_text_content":
                    return SetTextContent(commandData);
                case "get_text_content":
                    return GetTextContent(commandData);
                case "set_image_sprite":
                    return SetImageSprite(commandData);
                case "set_ui_color":
                    return SetUIColor(commandData);
                case "create_inputfield":
                    return CreateInputField(commandData);
                case "create_slider":
                    return CreateSlider(commandData);
                case "create_toggle":
                    return CreateToggle(commandData);
                case "get_slider_value":
                    return GetSliderValue(commandData);
                case "set_slider_value":
                    return SetSliderValue(commandData);
                case "get_toggle_value":
                    return GetToggleValue(commandData);
                case "set_toggle_value":
                    return SetToggleValue(commandData);
                default:
                    return new Dictionary<string, object> {
                        { "success", false },
                        { "message", $"Unknown command '{commandName}' for ManageUGUIHandler." }
                    };
            }
        }

        private static void ApplyRectTransformProperties(RectTransform rTransform, Dictionary<string, object> props)
        {
            if (props == null || rTransform == null) return;

            if (props.TryGetValue("anchored_position", out var ap) && ap is JObject apJobj)
                rTransform.anchoredPosition = new Vector2(Convert.ToSingle(apJobj["x"]), Convert.ToSingle(apJobj["y"]));
            
            if (props.TryGetValue("size_delta", out var sd) && sd is JObject sdJobj)
                rTransform.sizeDelta = new Vector2(Convert.ToSingle(sdJobj["x"]), Convert.ToSingle(sdJobj["y"]));

            if (props.TryGetValue("anchor_min", out var amin) && amin is JObject aminJobj)
                rTransform.anchorMin = new Vector2(Convert.ToSingle(aminJobj["x"]), Convert.ToSingle(aminJobj["y"]));

            if (props.TryGetValue("anchor_max", out var amax) && amax is JObject amaxJobj)
                rTransform.anchorMax = new Vector2(Convert.ToSingle(amaxJobj["x"]), Convert.ToSingle(amaxJobj["y"]));

            if (props.TryGetValue("pivot", out var p) && p is JObject pJobj)
                rTransform.pivot = new Vector2(Convert.ToSingle(pJobj["x"]), Convert.ToSingle(pJobj["y"]));
            
            if (props.TryGetValue("rotation_euler", out var rot) && rot is JObject rotJobj)
                rTransform.localEulerAngles = new Vector3(Convert.ToSingle(rotJobj["x"]), Convert.ToSingle(rotJobj["y"]), Convert.ToSingle(rotJobj["z"]));

            if (props.TryGetValue("scale", out var s) && s is JObject sJobj)
                rTransform.localScale = new Vector3(Convert.ToSingle(sJobj["x"]), Convert.ToSingle(sJobj["y"]), Convert.ToSingle(sJobj["z"]));
        }

        private static RectTransform FindParentRectTransform(string parentPath, out string errorMessage)
        {
            errorMessage = null;
            if (string.IsNullOrEmpty(parentPath))
            {
                errorMessage = "Parent path cannot be empty for creating child UI elements.";
                return null;
            }
            GameObject parentGO = GameObject.Find(parentPath);
            if (parentGO == null)
            {
                errorMessage = $"Parent GameObject not found at path: {parentPath}";
                return null;
            }
            RectTransform parentRect = parentGO.GetComponent<RectTransform>();
            if (parentRect == null)
            {
                errorMessage = $"Parent GameObject at '{parentPath}' does not have a RectTransform component.";
                return null;
            }
            return parentRect;
        }

        private static Dictionary<string, object> CreateCanvas(Dictionary<string, object> command)
        {
            var response = new Dictionary<string, object>();
            try
            {
                string name = command.TryGetValue("name", out var n) ? (string)n : "NewCanvas";
                string renderModeStr = command.TryGetValue("render_mode", out var rm) ? (string)rm : "ScreenSpaceOverlay";
                int sortingOrder = command.TryGetValue("sorting_order", out var so) ? Convert.ToInt32(so) : 0;
                string cameraName = command.TryGetValue("camera_name", out var cn) ? (string)cn : null;

                GameObject canvasGO = new GameObject(name);
                Canvas canvas = canvasGO.AddComponent<Canvas>();
                
                RenderMode mode;
                if (!Enum.TryParse<RenderMode>(renderModeStr, true, out mode))
                {
                    if (int.TryParse(renderModeStr, out int renderModeInt))
                    {
                        if (Enum.IsDefined(typeof(RenderMode), renderModeInt)) mode = (RenderMode)renderModeInt;
                        else { response["message"] = $"Invalid integer RenderMode value: {renderModeInt}. Defaulting to ScreenSpaceOverlay."; mode = RenderMode.ScreenSpaceOverlay; }
                    }
                    else { response["message"] = $"Invalid RenderMode string: {renderModeStr}. Defaulting to ScreenSpaceOverlay."; mode = RenderMode.ScreenSpaceOverlay; }
                }
                canvas.renderMode = mode;

                if (mode == RenderMode.ScreenSpaceCamera || mode == RenderMode.WorldSpace)
                {
                    Camera camera = null;
                    if (!string.IsNullOrEmpty(cameraName))
                    {
                        GameObject camGO = GameObject.Find(cameraName);
                        if (camGO != null) camera = camGO.GetComponent<Camera>();
                    }
                    if (camera == null && mode == RenderMode.ScreenSpaceCamera) camera = Camera.main;
                    if (camera != null) canvas.worldCamera = camera;
                    else if (mode == RenderMode.ScreenSpaceCamera) response["warning"] = $"Camera '{cameraName ?? "MainCamera"}' not found for canvas mode {mode}. Canvas may not render correctly.";
                }
                canvas.sortingOrder = sortingOrder;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();

                if (command.TryGetValue("rect_properties", out var rectPropsObj) && rectPropsObj is JObject rectPropsJObj)
                {
                    ApplyRectTransformProperties(canvasGO.GetComponent<RectTransform>(), rectPropsJObj.ToObject<Dictionary<string, object>>());
                }
                
                Undo.RegisterCreatedObjectUndo(canvasGO, "Create UGUI Canvas");
                response["success"] = true;
                response["message"] = $"Canvas '{name}' created successfully.";
                response["element_path"] = GetGameObjectPath(canvasGO);
            }
            catch (Exception e)
            {
                response["success"] = false;
                response["message"] = $"Error creating Canvas: {e.Message} {e.StackTrace}";
            }
            return response;
        }

        private static Dictionary<string, object> CreatePanel(Dictionary<string, object> command)
        {
            var response = new Dictionary<string, object>();
            try
            {
                string parentPath = command["parent_path"] as string;
                RectTransform parentRect = FindParentRectTransform(parentPath, out string findParentError);
                if (parentRect == null) {
                    response["success"] = false;
                    response["message"] = findParentError;
                    return response;
                }

                string name = command.TryGetValue("name", out var n) ? (string)n : "NewPanel";
                GameObject panelGO = new GameObject(name);
                panelGO.transform.SetParent(parentRect, false);
                Image image = panelGO.AddComponent<Image>(); // RectTransform is added automatically

                image.color = new Color(1f, 1f, 1f, 0.39f); // Default panel color
                if (command.TryGetValue("color", out var c) && c is JObject colorJobj) {
                    image.color = new Color(
                        Convert.ToSingle(colorJobj["r"]), Convert.ToSingle(colorJobj["g"]),
                        Convert.ToSingle(colorJobj["b"]), Convert.ToSingle(colorJobj["a"])
                    );
                }
                
                if (command.TryGetValue("rect_properties", out var rectPropsObj) && rectPropsObj is JObject rectPropsJObj)
                {
                    ApplyRectTransformProperties(panelGO.GetComponent<RectTransform>(), rectPropsJObj.ToObject<Dictionary<string, object>>());
                } else { 
                    panelGO.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 200);
                }

                Undo.RegisterCreatedObjectUndo(panelGO, "Create UGUI Panel");
                response["success"] = true;
                response["message"] = $"Panel '{name}' created successfully under '{parentPath}'.";
                response["element_path"] = GetGameObjectPath(panelGO);
            }
            catch (Exception e) {
                response["success"] = false;
                response["message"] = $"Error creating Panel: {e.Message} {e.StackTrace}";
            }
            return response;
        }

        private static Dictionary<string, object> CreateImage(Dictionary<string, object> command)
        {
            var response = new Dictionary<string, object>();
            try
            {
                string parentPath = command["parent_path"] as string;
                RectTransform parentRect = FindParentRectTransform(parentPath, out string findParentError);
                if (parentRect == null) { 
                    response["success"] = false; 
                    response["message"] = findParentError; 
                    return response; 
                }

                string name = command.TryGetValue("name", out var n) ? (string)n : "NewImage";
                GameObject imageGO = new GameObject(name);
                imageGO.transform.SetParent(parentRect, false);
                Image image = imageGO.AddComponent<Image>();

                if (command.TryGetValue("sprite_path", out var spPath) && spPath is string spritePathStr && !string.IsNullOrEmpty(spritePathStr)) {
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePathStr);
                    if (sprite != null) image.sprite = sprite;
                    else response["warning"] = $"Sprite not found at path: {spritePathStr}";
                }

                if (command.TryGetValue("color", out var c) && c is JObject colorJobj) {
                     image.color = new Color(
                        Convert.ToSingle(colorJobj["r"]), Convert.ToSingle(colorJobj["g"]),
                        Convert.ToSingle(colorJobj["b"]), Convert.ToSingle(colorJobj["a"])
                    );
                }
                
                if (command.TryGetValue("rect_properties", out var rectPropsObj) && rectPropsObj is JObject rectPropsJObj) {
                    ApplyRectTransformProperties(imageGO.GetComponent<RectTransform>(), rectPropsJObj.ToObject<Dictionary<string, object>>());
                } else { imageGO.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100); }

                Undo.RegisterCreatedObjectUndo(imageGO, "Create UGUI Image");
                response["success"] = true;
                response["message"] = $"Image '{name}' created successfully under '{parentPath}'.";
                response["element_path"] = GetGameObjectPath(imageGO);
            }
            catch (Exception e) {
                response["success"] = false;
                response["message"] = $"Error creating Image: {e.Message} {e.StackTrace}";
            }
            return response;
        }

        private static Dictionary<string, object> CreateText(Dictionary<string, object> command)
        {
            var response = new Dictionary<string, object>();
            try
            {
                string parentPath = command["parent_path"] as string;
                RectTransform parentRect = FindParentRectTransform(parentPath, out string findParentError);
                if (parentRect == null) { 
                    response["success"] = false; 
                    response["message"] = findParentError; 
                    return response; 
                }

                string name = command.TryGetValue("name", out var n) ? (string)n : "NewText";
                GameObject textGO = new GameObject(name);
                textGO.transform.SetParent(parentRect, false);
                Text textComp = textGO.AddComponent<Text>();

                textComp.text = command.TryGetValue("text_content", out var tc) ? (string)tc : "New Text";
                if (command.TryGetValue("font_size", out var fs)) textComp.fontSize = Convert.ToInt32(fs);
                
                if (command.TryGetValue("color", out var c) && c is JObject colorJobj) {
                     textComp.color = new Color(
                        Convert.ToSingle(colorJobj["r"]), Convert.ToSingle(colorJobj["g"]),
                        Convert.ToSingle(colorJobj["b"]), Convert.ToSingle(colorJobj["a"])
                    );
                } else {
                    textComp.color = Color.black; // Default text color
                }

                if (command.TryGetValue("font_style", out var styleStr) && styleStr is string) {
                    if (Enum.TryParse<FontStyle>((string)styleStr, true, out var fStyle)) textComp.fontStyle = fStyle;
                }
                if (command.TryGetValue("alignment", out var alignStr) && alignStr is string) {
                    if (Enum.TryParse<TextAnchor>((string)alignStr, true, out var anchor)) textComp.alignment = anchor;
                }
                
                if (command.TryGetValue("rect_properties", out var rectPropsObj) && rectPropsObj is JObject rectPropsJObj) {
                    ApplyRectTransformProperties(textGO.GetComponent<RectTransform>(), rectPropsJObj.ToObject<Dictionary<string, object>>());
                } else { textGO.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 30); }
                
                Undo.RegisterCreatedObjectUndo(textGO, "Create UGUI Text");
                response["success"] = true;
                response["message"] = $"Text '{name}' created successfully under '{parentPath}'.";
                response["element_path"] = GetGameObjectPath(textGO);
            }
            catch (Exception e) {
                 response["success"] = false;
                 response["message"] = $"Error creating Text: {e.Message} {e.StackTrace}";
            }
            return response;
        }

        private static Dictionary<string, object> CreateButton(Dictionary<string, object> command)
        {
            var response = new Dictionary<string, object>();
            try
            {
                string parentPath = command["parent_path"] as string;
                RectTransform parentRect = FindParentRectTransform(parentPath, out string findParentError);
                if (parentRect == null) { 
                    response["success"] = false; 
                    response["message"] = findParentError; 
                    return response; 
                }

                string name = command.TryGetValue("name", out var n) ? (string)n : "NewButton";
                
                GameObject buttonGO = new GameObject(name);
                buttonGO.transform.SetParent(parentRect, false);
                Image buttonImage = buttonGO.AddComponent<Image>();
                buttonGO.AddComponent<Button>();

                if (command.TryGetValue("sprite_path", out var spPath) && spPath is string spritePathStr && !string.IsNullOrEmpty(spritePathStr)) {
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePathStr);
                    if (sprite != null) buttonImage.sprite = sprite;
                    else response["warning"] = $"Button sprite not found at: {spritePathStr}. Using default.";
                } else {
                     // For default look, Unity's Image component needs a sprite.
                     // If no path, it's a white square. For a typical button look, a default sprite is needed.
                     // Unity uses "UISprite" as default if available in project, or one can be bundled.
                     // For simplicity, we allow it to be a white square if no sprite provided.
                }
                if (command.TryGetValue("color", out var c) && c is JObject colorJobj) { 
                    buttonImage.color = new Color(
                        Convert.ToSingle(colorJobj["r"]), Convert.ToSingle(colorJobj["g"]),
                        Convert.ToSingle(colorJobj["b"]), Convert.ToSingle(colorJobj["a"])
                    );
                }

                GameObject textGO = new GameObject("Text");
                textGO.transform.SetParent(buttonGO.transform, false);
                Text textComp = textGO.AddComponent<Text>();
                textComp.text = command.TryGetValue("button_text_content", out var btc) ? (string)btc : "Button";
                textComp.alignment = TextAnchor.MiddleCenter;
                textComp.color = Color.black; 

                RectTransform textRect = textGO.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;

                if (command.TryGetValue("rect_properties", out var rectPropsObj) && rectPropsObj is JObject rectPropsJObj) {
                    ApplyRectTransformProperties(buttonGO.GetComponent<RectTransform>(), rectPropsJObj.ToObject<Dictionary<string, object>>());
                } else { buttonGO.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 30); }

                Undo.RegisterCreatedObjectUndo(buttonGO, "Create UGUI Button");
                response["success"] = true;
                response["message"] = $"Button '{name}' created successfully under '{parentPath}'.";
                response["element_path"] = GetGameObjectPath(buttonGO);
            }
            catch (Exception e) { 
                response["success"] = false;
                response["message"] = $"Error creating Button: {e.Message} {e.StackTrace}";
            }
            return response;
        }
        
        // Placeholder for a helper function to get GameObject's full path
        private static string GetGameObjectPath(GameObject obj)
        {
            if (obj == null) return null;
            string path = "/" + obj.name;
            Transform currentParent = obj.transform.parent;
            while (currentParent != null)
            {
                path = "/" + currentParent.name + path;
                currentParent = currentParent.parent;
            }
            return path.TrimStart('/'); // Remove leading slash if scene root isn't desired in path
        }

        private static Dictionary<string, object> SetRectTransform(Dictionary<string, object> command)
        {
            var response = new Dictionary<string, object>();
            try
            {
                string elementPath = command["element_path"] as string;
                if (string.IsNullOrEmpty(elementPath))
                {
                    response["success"] = false;
                    response["message"] = "Element path cannot be empty for SetRectTransform.";
                    return response;
                }
                GameObject elementGO = GameObject.Find(elementPath);
                if (elementGO == null) 
                {
                    response["success"] = false;
                    response["message"] = $"Element not found at path: {elementPath}";
                    return response;
                }

                RectTransform rTransform = elementGO.GetComponent<RectTransform>();
                if (rTransform == null) 
                {
                    response["success"] = false;
                    response["message"] = $"Element at '{elementPath}' does not have a RectTransform component.";
                    return response;
                }

                if (command.TryGetValue("rect_properties", out var rectPropsObj) && rectPropsObj is JObject rectPropsJObj)
                {
                    ApplyRectTransformProperties(rTransform, rectPropsJObj.ToObject<Dictionary<string, object>>());
                    EditorUtility.SetDirty(elementGO); // Mark as dirty to ensure changes are saved
                    response["success"] = true;
                    response["message"] = $"RectTransform properties applied to '{elementPath}'.";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "rect_properties not provided or in incorrect format for SetRectTransform.";
                }
            }
            catch (Exception e) 
            {
                response["success"] = false;
                response["message"] = $"Error in SetRectTransform: {e.Message} {e.StackTrace}";
            }
            return response;
        }

        private static Dictionary<string, object> SetParent(Dictionary<string, object> command)
        {
            var response = new Dictionary<string, object>();
            try
            {
                string elementPath = command["element_path"] as string;
                string newParentPath = command["new_parent_path"] as string;

                if (string.IsNullOrEmpty(elementPath) || string.IsNullOrEmpty(newParentPath))
                {
                    response["success"] = false;
                    response["message"] = "Element path and new parent path cannot be empty for SetParent.";
                    return response;
                }

                GameObject elementGO = GameObject.Find(elementPath);
                if (elementGO == null) 
                {
                    response["success"] = false;
                    response["message"] = $"Element to reparent not found at path: {elementPath}";
                    return response;
                }

                RectTransform parentRect = FindParentRectTransform(newParentPath, out string findParentError);
                if (parentRect == null) 
                {
                    response["success"] = false;
                    response["message"] = findParentError; // Error message from FindParentRectTransform
                    return response;
                }
                
                // Prevent parenting to self or child
                if (elementGO.transform == parentRect.transform || elementGO.transform.IsChildOf(parentRect.transform)) {
                     response["success"] = false;
                     response["message"] = "Cannot parent an element to itself or its own child.";
                     return response;
                }
                if (parentRect.transform.IsChildOf(elementGO.transform)) { // Check if new parent is a child of the element
                    response["success"] = false;
                    response["message"] = "Cannot parent an element to its own child (cyclic parenting).";
                    return response;
                }


                Undo.SetTransformParent(elementGO.transform, parentRect, "Reparent UGUI Element");
                // elementGO.transform.SetParent(parentRect, true); // worldPositionStays = true is often desired for UI.
                EditorUtility.SetDirty(elementGO);
                response["success"] = true;
                response["message"] = $"Element '{elementPath}' reparented to '{newParentPath}'.";
            }
            catch (Exception e) 
            {
                response["success"] = false;
                response["message"] = $"Error in SetParent: {e.Message} {e.StackTrace}";
            }
            return response;
        }

        private static Dictionary<string, object> SetActive(Dictionary<string, object> command)
        {
            var response = new Dictionary<string, object>();
            try
            {
                string elementPath = command["element_path"] as string;
                 if (string.IsNullOrEmpty(elementPath))
                {
                    response["success"] = false;
                    response["message"] = "Element path cannot be empty for SetActive.";
                    return response;
                }
                bool isActive = Convert.ToBoolean(command["is_active"]);

                GameObject elementGO = GameObject.Find(elementPath);
                if (elementGO == null) 
                {
                    response["success"] = false;
                    response["message"] = $"Element not found at path: {elementPath}";
                    return response;
                }
                
                if (elementGO.activeSelf != isActive) // Only change if different to avoid unnecessary Undo
                {
                    Undo.RecordObject(elementGO, isActive ? "Activate GameObject" : "Deactivate GameObject");
                    elementGO.SetActive(isActive);
                    EditorUtility.SetDirty(elementGO);
                }
                response["success"] = true;
                response["message"] = $"Element '{elementPath}' active state set to {isActive}.";
            }
            catch (Exception e) 
            {
                response["success"] = false;
                response["message"] = $"Error in SetActive: {e.Message} {e.StackTrace}";
            }
            return response;
        }

        private static Dictionary<string, object> DeleteElement(Dictionary<string, object> command)
        {
            var response = new Dictionary<string, object>();
            try
            {
                string elementPath = command["element_path"] as string;
                if (string.IsNullOrEmpty(elementPath))
                {
                    response["success"] = false;
                    response["message"] = "Element path cannot be empty for DeleteElement.";
                    return response;
                }
                GameObject elementGO = GameObject.Find(elementPath);
                if (elementGO == null) 
                {
                    response["success"] = false; // Or true if "already deleted" is success
                    response["message"] = $"Element '{elementPath}' not found for deletion.";
                    return response;
                }

                Undo.DestroyObjectImmediate(elementGO); // Use Undo for proper editor integration
                response["success"] = true;
                response["message"] = $"Element '{elementPath}' deleted successfully.";
            }
            catch (Exception e) 
            {
                response["success"] = false;
                response["message"] = $"Error in DeleteElement: {e.Message} {e.StackTrace}";
            }
            return response;
        }
        
        // Other UGUI handling methods (SetRectTransform etc.) will be added here.

        private static Dictionary<string, object> SetTextContent(Dictionary<string, object> command)
        {
            var response = new Dictionary<string, object>();
            try
            {
                string elementPath = command["element_path"] as string;
                if (string.IsNullOrEmpty(elementPath))
                {
                    response["success"] = false;
                    response["message"] = "Element path cannot be empty for SetTextContent.";
                    return response;
                }
                string textContent = command["text_content"] as string; // text_content can be null or empty, which is fine

                GameObject elementGO = GameObject.Find(elementPath);
                if (elementGO == null) { 
                    response["success"] = false;
                    response["message"] = $"Element not found at path: {elementPath}";
                    return response; 
                }

                Text textComp = elementGO.GetComponent<Text>();
                // TMPro.TMP_Text tmproTextComp = elementGO.GetComponent<TMPro.TMP_Text>(); // Future TMPro support

                if (textComp != null) {
                    Undo.RecordObject(textComp, "Set Text Content");
                    textComp.text = textContent;
                    EditorUtility.SetDirty(textComp);
                    response["success"] = true;
                    response["message"] = $"Text content set for '{elementPath}'.";
                } 
                // else if (tmproTextComp != null) { /* ... handle TextMeshPro ... */ }
                else { 
                    response["success"] = false;
                    response["message"] = $"Element at '{elementPath}' does not have a Text component.";
                    return response; 
                }
            }
            catch (Exception e) { 
                response["success"] = false;
                response["message"] = $"Error in SetTextContent: {e.Message} {e.StackTrace}";
            }
            return response;
        }

        private static Dictionary<string, object> GetTextContent(Dictionary<string, object> command)
        {
            var response = new Dictionary<string, object>();
            try
            {
                string elementPath = command["element_path"] as string;
                if (string.IsNullOrEmpty(elementPath))
                {
                    response["success"] = false;
                    response["message"] = "Element path cannot be empty for GetTextContent.";
                    return response;
                }
                GameObject elementGO = GameObject.Find(elementPath);
                if (elementGO == null) { 
                    response["success"] = false;
                    response["message"] = $"Element not found at path: {elementPath}";
                    return response; 
                }

                Text textComp = elementGO.GetComponent<Text>();
                // TMPro.TMP_Text tmproTextComp = elementGO.GetComponent<TMPro.TMP_Text>(); // Future TMPro support

                if (textComp != null) {
                    response["success"] = true;
                    response["text_content"] = textComp.text;
                    response["message"] = $"Text content retrieved for '{elementPath}'.";
                } 
                // else if (tmproTextComp != null) { /* ... handle TextMeshPro ... */ }
                else { 
                    response["success"] = false;
                    response["message"] = $"Element at '{elementPath}' does not have a Text component.";
                    return response; 
                }
            }
            catch (Exception e) { 
                response["success"] = false;
                response["message"] = $"Error in GetTextContent: {e.Message} {e.StackTrace}";
            }
            return response;
        }

        private static Dictionary<string, object> SetImageSprite(Dictionary<string, object> command)
        {
            var response = new Dictionary<string, object>();
            try
            {
                string elementPath = command["element_path"] as string;
                if (string.IsNullOrEmpty(elementPath))
                {
                    response["success"] = false;
                    response["message"] = "Element path cannot be empty for SetImageSprite.";
                    return response;
                }
                string spritePath = command.TryGetValue("sprite_path", out var sp) ? sp as string : null;


                GameObject elementGO = GameObject.Find(elementPath);
                if (elementGO == null) { 
                    response["success"] = false;
                    response["message"] = $"Element not found at path: {elementPath}";
                    return response; 
                }

                Image imageComp = elementGO.GetComponent<Image>();
                if (imageComp == null) { 
                    response["success"] = false;
                    response["message"] = $"Element at '{elementPath}' does not have an Image component.";
                    return response; 
                }
                
                Undo.RecordObject(imageComp, "Set Image Sprite");
                if (string.IsNullOrEmpty(spritePath)) { 
                    imageComp.sprite = null;
                    response["message"] = $"Sprite cleared for '{elementPath}'.";
                } else {
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    if (sprite == null) { 
                        response["success"] = false;
                        response["message"] = $"Sprite not found at path: {spritePath}";
                        return response; 
                    }
                    imageComp.sprite = sprite;
                    response["message"] = $"Sprite '{spritePath}' set for '{elementPath}'.";
                }
                EditorUtility.SetDirty(imageComp);
                response["success"] = true; 
            }
            catch (Exception e) { 
                response["success"] = false;
                response["message"] = $"Error in SetImageSprite: {e.Message} {e.StackTrace}";
            }
            return response;
        }
        
        private static Dictionary<string, object> SetUIColor(Dictionary<string, object> command)
        {
            var response = new Dictionary<string, object>();
            try
            {
                string elementPath = command["element_path"] as string;
                if (string.IsNullOrEmpty(elementPath))
                {
                    response["success"] = false;
                    response["message"] = "Element path cannot be empty for SetUIColor.";
                    return response;
                }
                JObject colorJobj = command["color"] as JObject;
                if (colorJobj == null || !colorJobj.ContainsKey("r") || !colorJobj.ContainsKey("g") || !colorJobj.ContainsKey("b") || !colorJobj.ContainsKey("a")) { 
                    response["success"] = false;
                    response["message"] = "Color data is missing or invalid (must be a JObject with r,g,b,a keys).";
                    return response; 
                }

                Color newColor = new Color(
                    Convert.ToSingle(colorJobj["r"]), Convert.ToSingle(colorJobj["g"]),
                    Convert.ToSingle(colorJobj["b"]), Convert.ToSingle(colorJobj["a"])
                );

                GameObject elementGO = GameObject.Find(elementPath);
                if (elementGO == null) { 
                    response["success"] = false;
                    response["message"] = $"Element not found at path: {elementPath}";
                    return response; 
                }
                
                MaskableGraphic graphic = elementGO.GetComponent<MaskableGraphic>(); 
                if (graphic != null) {
                    Undo.RecordObject(graphic, "Set UI Color");
                    graphic.color = newColor;
                    EditorUtility.SetDirty(graphic);
                    response["success"] = true;
                    response["message"] = $"Color set for '{elementPath}'.";
                } else { 
                    response["success"] = false;
                    response["message"] = $"Element at '{elementPath}' does not have a suitable graphic component (Text, Image, RawImage) for setting color.";
                    return response; 
                }
            }
            catch (Exception e) { 
                response["success"] = false;
                response["message"] = $"Error in SetUIColor: {e.Message} {e.StackTrace}";
            }
            return response;
        }

        private static Dictionary<string, object> CreateInputField(Dictionary<string, object> command) {
            var response = new Dictionary<string, object>();
            try {
                string parentPath = command["parent_path"] as string;
                RectTransform parentRect = FindParentRectTransform(parentPath, out string findParentError);
                if (parentRect == null) { 
                    response["success"] = false; 
                    response["message"] = findParentError; 
                    return response; 
                }

                string name = command.TryGetValue("name", out var n) ? (string)n : "InputField";
                GameObject inputFieldGO = new GameObject(name);
                inputFieldGO.transform.SetParent(parentRect, false);
                Image bgImage = inputFieldGO.AddComponent<Image>(); 
                bgImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"); 
                bgImage.type = Image.Type.Sliced;
                InputField inputFieldComp = inputFieldGO.AddComponent<InputField>();

                GameObject placeholderGO = new GameObject("Placeholder");
                placeholderGO.transform.SetParent(inputFieldGO.transform, false);
                Text placeholderText = placeholderGO.AddComponent<Text>();
                placeholderText.text = command.TryGetValue("placeholder_text", out var pt) ? (string)pt : "Enter text...";
                placeholderText.fontStyle = FontStyle.Italic;
                placeholderText.color = new Color(0.196f, 0.196f, 0.196f, 0.5f); 
                RectTransform placeholderRect = placeholderGO.GetComponent<RectTransform>();
                placeholderRect.anchorMin = Vector2.zero; placeholderRect.anchorMax = Vector2.one;
                placeholderRect.sizeDelta = Vector2.zero; placeholderRect.offsetMin = new Vector2(10,6); placeholderRect.offsetMax = new Vector2(-10,-7); 

                inputFieldComp.placeholder = placeholderText;

                GameObject textGO = new GameObject("Text");
                textGO.transform.SetParent(inputFieldGO.transform, false);
                Text actualText = textGO.AddComponent<Text>();
                actualText.text = command.TryGetValue("text_content", out var tc) ? (string)tc : "";
                actualText.supportRichText = false;
                actualText.color = Color.black;
                RectTransform textRect = textGO.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero; textRect.offsetMin = new Vector2(10,6); textRect.offsetMax = new Vector2(-10,-7);
                
                inputFieldComp.textComponent = actualText;
                inputFieldComp.targetGraphic = bgImage;

                if (command.TryGetValue("rect_properties", out var rectPropsObj) && rectPropsObj is JObject rectPropsJObj) {
                    ApplyRectTransformProperties(inputFieldGO.GetComponent<RectTransform>(), rectPropsJObj.ToObject<Dictionary<string, object>>());
                } else { inputFieldGO.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 30); }
                
                Undo.RegisterCreatedObjectUndo(inputFieldGO, "Create UGUI InputField");
                response["success"] = true;
                response["message"] = $"InputField '{name}' created successfully under '{parentPath}'.";
                response["element_path"] = GetGameObjectPath(inputFieldGO);
            } catch (Exception e) { 
                response["success"] = false;
                response["message"] = $"Error creating InputField: {e.Message} {e.StackTrace}";
            }
            return response;
        }

        private static Dictionary<string, object> CreateSlider(Dictionary<string, object> command) {
            var response = new Dictionary<string, object>();
            try {
                string parentPath = command["parent_path"] as string;
                RectTransform parentRect = FindParentRectTransform(parentPath, out string findParentError);
                if (parentRect == null) { 
                    response["success"] = false; 
                    response["message"] = findParentError; 
                    return response; 
                }
                string name = command.TryGetValue("name", out var n) ? (string)n : "NewSlider";
                GameObject sliderGO = new GameObject(name); 
                sliderGO.transform.SetParent(parentRect, false);
                Slider sliderComp = sliderGO.AddComponent<Slider>();
                
                // This simplified version does not create child elements for background, fill, handle.
                // A default Unity slider would require these for visual functionality.
                // Add a simple Image as a visual placeholder for the slider itself
                Image sliderImage = sliderGO.AddComponent<Image>();
                sliderImage.color = Color.gray; // Basic visual

                if (command.TryGetValue("min_value", out var minV)) sliderComp.minValue = Convert.ToSingle(minV);
                if (command.TryGetValue("max_value", out var maxV)) sliderComp.maxValue = Convert.ToSingle(maxV);
                if (command.TryGetValue("value", out var val)) sliderComp.value = Convert.ToSingle(val);
                
                if (command.TryGetValue("rect_properties", out var rectPropsObj) && rectPropsObj is JObject rectPropsJObj) {
                    ApplyRectTransformProperties(sliderGO.GetComponent<RectTransform>(), rectPropsJObj.ToObject<Dictionary<string, object>>());
                } else { sliderGO.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 20); }

                Undo.RegisterCreatedObjectUndo(sliderGO, "Create UGUI Slider");
                response["success"] = true;
                response["message"] = $"Simplified Slider '{name}' created successfully under '{parentPath}'. It may require child elements for full visual functionality.";
                response["element_path"] = GetGameObjectPath(sliderGO);
            } catch (Exception e) { 
                response["success"] = false;
                response["message"] = $"Error creating Slider: {e.Message} {e.StackTrace}";
            }
            return response;
        }

        private static Dictionary<string, object> CreateToggle(Dictionary<string, object> command) {
            var response = new Dictionary<string, object>();
            try {
                string parentPath = command["parent_path"] as string;
                RectTransform parentRect = FindParentRectTransform(parentPath, out string findParentError);
                if (parentRect == null) { 
                    response["success"] = false; 
                    response["message"] = findParentError; 
                    return response; 
                }
                string name = command.TryGetValue("name", out var n) ? (string)n : "NewToggle";
                GameObject toggleGO = new GameObject(name); 
                toggleGO.transform.SetParent(parentRect, false);
                Toggle toggleComp = toggleGO.AddComponent<Toggle>();

                // This simplified version does not create child elements for background, checkmark, label.
                // Add a simple Image as a visual placeholder for the toggle itself
                Image toggleImage = toggleGO.AddComponent<Image>();
                toggleImage.color = Color.gray; // Basic visual
                toggleComp.targetGraphic = toggleImage; // Need a target graphic

                if (command.TryGetValue("is_on", out var isOn)) toggleComp.isOn = Convert.ToBoolean(isOn);
                
                if (command.TryGetValue("rect_properties", out var rectPropsObj) && rectPropsObj is JObject rectPropsJObj) {
                    ApplyRectTransformProperties(toggleGO.GetComponent<RectTransform>(), rectPropsJObj.ToObject<Dictionary<string, object>>());
                } else { toggleGO.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 20); }

                Undo.RegisterCreatedObjectUndo(toggleGO, "Create UGUI Toggle");
                response["success"] = true;
                response["message"] = $"Simplified Toggle '{name}' created successfully under '{parentPath}'. It may require child elements for full visual functionality.";
                response["element_path"] = GetGameObjectPath(toggleGO);
            } catch (Exception e) { 
                response["success"] = false;
                response["message"] = $"Error creating Toggle: {e.Message} {e.StackTrace}";
            }
            return response;
        }

        private static Dictionary<string, object> GetSliderValue(Dictionary<string, object> command) { 
            var response = new Dictionary<string, object>();
            try {
                string elementPath = command["element_path"] as string;
                 if (string.IsNullOrEmpty(elementPath)) {
                    response["success"] = false; response["message"] = "Element path cannot be empty."; return response;
                }
                GameObject elementGO = GameObject.Find(elementPath);
                if (elementGO == null) {
                    response["success"] = false; response["message"] = $"Element not found at path: {elementPath}"; return response;
                }
                Slider sliderComp = elementGO.GetComponent<Slider>();
                if (sliderComp == null) {
                    response["success"] = false; response["message"] = $"Element at '{elementPath}' does not have a Slider component."; return response;
                }
                response["success"] = true;
                response["value"] = sliderComp.value;
                response["message"] = $"Slider value retrieved for '{elementPath}'.";
            } catch (Exception e) {
                response["success"] = false; response["message"] = $"Error in GetSliderValue: {e.Message}";
            }
            return response;
        }

        private static Dictionary<string, object> SetSliderValue(Dictionary<string, object> command) { 
            var response = new Dictionary<string, object>();
            try {
                string elementPath = command["element_path"] as string;
                if (string.IsNullOrEmpty(elementPath)) {
                    response["success"] = false; response["message"] = "Element path cannot be empty."; return response;
                }
                if (!command.TryGetValue("value", out var valObj) || valObj == null) {
                     response["success"] = false; response["message"] = "Value not provided for SetSliderValue."; return response;
                }
                float value = Convert.ToSingle(valObj);

                GameObject elementGO = GameObject.Find(elementPath);
                if (elementGO == null) {
                    response["success"] = false; response["message"] = $"Element not found at path: {elementPath}"; return response;
                }
                Slider sliderComp = elementGO.GetComponent<Slider>();
                if (sliderComp == null) {
                    response["success"] = false; response["message"] = $"Element at '{elementPath}' does not have a Slider component."; return response;
                }
                Undo.RecordObject(sliderComp, "Set Slider Value");
                sliderComp.value = value;
                EditorUtility.SetDirty(sliderComp);
                response["success"] = true;
                response["message"] = $"Slider value set to {value} for '{elementPath}'.";
            } catch (Exception e) {
                response["success"] = false; response["message"] = $"Error in SetSliderValue: {e.Message}";
            }
            return response;
        }

        private static Dictionary<string, object> GetToggleValue(Dictionary<string, object> command) { 
            var response = new Dictionary<string, object>();
            try {
                string elementPath = command["element_path"] as string;
                if (string.IsNullOrEmpty(elementPath)) {
                    response["success"] = false; response["message"] = "Element path cannot be empty."; return response;
                }
                GameObject elementGO = GameObject.Find(elementPath);
                if (elementGO == null) {
                    response["success"] = false; response["message"] = $"Element not found at path: {elementPath}"; return response;
                }
                Toggle toggleComp = elementGO.GetComponent<Toggle>();
                if (toggleComp == null) {
                    response["success"] = false; response["message"] = $"Element at '{elementPath}' does not have a Toggle component."; return response;
                }
                response["success"] = true;
                response["is_on"] = toggleComp.isOn;
                response["message"] = $"Toggle state retrieved for '{elementPath}'.";
            } catch (Exception e) {
                response["success"] = false; response["message"] = $"Error in GetToggleValue: {e.Message}";
            }
            return response;
        }
        
        private static Dictionary<string, object> SetToggleValue(Dictionary<string, object> command) { 
            var response = new Dictionary<string, object>();
            try {
                string elementPath = command["element_path"] as string;
                 if (string.IsNullOrEmpty(elementPath)) {
                    response["success"] = false; response["message"] = "Element path cannot be empty."; return response;
                }
                if (!command.TryGetValue("is_on", out var isOnObj) || isOnObj == null) {
                     response["success"] = false; response["message"] = "is_on value not provided for SetToggleValue."; return response;
                }
                bool isOn = Convert.ToBoolean(isOnObj);

                GameObject elementGO = GameObject.Find(elementPath);
                if (elementGO == null) {
                    response["success"] = false; response["message"] = $"Element not found at path: {elementPath}"; return response;
                }
                Toggle toggleComp = elementGO.GetComponent<Toggle>();
                if (toggleComp == null) {
                    response["success"] = false; response["message"] = $"Element at '{elementPath}' does not have a Toggle component."; return response;
                }
                Undo.RecordObject(toggleComp, "Set Toggle Value");
                toggleComp.isOn = isOn;
                EditorUtility.SetDirty(toggleComp);
                response["success"] = true;
                response["message"] = $"Toggle state set to {isOn} for '{elementPath}'.";
            } catch (Exception e) {
                response["success"] = false; response["message"] = $"Error in SetToggleValue: {e.Message}";
            }
            return response;
        }

    }
}
