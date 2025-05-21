using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO; // Required for Path
using Newtonsoft.Json.Linq; // Added for JObject

public class ManageMaterialHandler
{
    public static object HandleCommand(JObject payload)
    {
        string commandName = payload["command"]?.ToString();
        if (string.IsNullOrEmpty(commandName))
        {
            return new Dictionary<string, object>
            {
                { "success", false },
                { "message", "Command not specified in payload for ManageMaterialHandler." }
            };
        }

        // Convert JObject to Dictionary for the specific handlers
        Dictionary<string, object> commandData = payload.ToObject<Dictionary<string, object>>();

        switch (commandName)
        {
            case "create_material":
                return CreateMaterial(commandData);
            case "set_material_property":
                return SetMaterialProperty(commandData);
            case "get_material_property":
                return GetMaterialProperty(commandData);
            case "assign_shader":
                return AssignShader(commandData);
            default:
                return new Dictionary<string, object>
                {
                    { "success", false },
                    { "message", $"Unknown command '{commandName}' for ManageMaterialHandler." }
                };
        }
    }

    public static Dictionary<string, object> CreateMaterial(Dictionary<string, object> command)
    {
        var response = new Dictionary<string, object>();
        string materialName = command.ContainsKey("material_name") ? command["material_name"] as string : "NewMaterial";
        string shaderName = command.ContainsKey("shader_name") ? command["shader_name"] as string : null;

        // Default path for new materials
        string materialsFolderPath = "Assets/Materials";
        if (!Directory.Exists(Path.Combine(Application.dataPath, "Materials"))) // Application.dataPath points to Assets folder
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
        
        string materialPath = Path.Combine(materialsFolderPath, materialName + ".mat");
        materialPath = AssetDatabase.GenerateUniqueAssetPath(materialPath);

        Material material = new Material(Shader.Find("Standard")); // Default to Standard shader

        if (!string.IsNullOrEmpty(shaderName))
        {
            Shader shader = Shader.Find(shaderName);
            if (shader != null)
            {
                material.shader = shader;
            }
            else
            {
                response["success"] = false;
                response["message"] = $"Shader '{shaderName}' not found. Material created with default shader.";
                // Continue to create the material with the default shader
            }
        }
        
        AssetDatabase.CreateAsset(material, materialPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        response["success"] = true;
        response["message"] = $"Material '{Path.GetFileNameWithoutExtension(materialPath)}' created successfully at {materialPath}.";
        response["asset_path"] = materialPath;
        
        return response;
    }

    public static Dictionary<string, object> SetMaterialProperty(Dictionary<string, object> command)
    {
        var response = new Dictionary<string, object>();
        string materialPath = command["material_path"] as string;
        string propertyName = command["property_name"] as string;
        string propertyType = command["property_type"] as string;
        object value = command["value"];

        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            response["success"] = false;
            response["message"] = $"Material not found at path: {materialPath}";
            return response;
        }

        if (!material.HasProperty(propertyName))
        {
            response["success"] = false;
            response["message"] = $"Material '{material.name}' does not have property named '{propertyName}'.";
            return response;
        }

        try
        {
            switch (propertyType.ToLower())
            {
                case "color":
                    if (value is Dictionary<string, object> colorDict)
                    {
                        Color color = new Color(
                            System.Convert.ToSingle(colorDict["r"]),
                            System.Convert.ToSingle(colorDict["g"]),
                            System.Convert.ToSingle(colorDict["b"]),
                            System.Convert.ToSingle(colorDict["a"])
                        );
                        material.SetColor(propertyName, color);
                    }
                    else
                    {
                        throw new System.ArgumentException("Color value must be a dictionary with r,g,b,a keys.");
                    }
                    break;
                case "float":
                    material.SetFloat(propertyName, System.Convert.ToSingle(value));
                    break;
                case "texture":
                    string texturePath = value as string;
                    if (string.IsNullOrEmpty(texturePath)) { // Allow unsetting texture
                        material.SetTexture(propertyName, null);
                    } else {
                        Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
                        if (texture == null && !string.IsNullOrEmpty(texturePath)) // only error if a path was given but not found
                        {
                            response["success"] = false;
                            response["message"] = $"Texture not found at path: {texturePath} for material {materialPath} property {propertyName}.";
                            EditorUtility.SetDirty(material); // Mark material as changed for save.
                            AssetDatabase.SaveAssets();
                            return response;
                        }
                        material.SetTexture(propertyName, texture);
                    }
                    break;
                case "vector":
                    if (value is Dictionary<string, object> vecDict)
                    {
                        Vector4 vector = new Vector4(
                            System.Convert.ToSingle(vecDict["x"]),
                            System.Convert.ToSingle(vecDict["y"]),
                            System.Convert.ToSingle(vecDict["z"]),
                            System.Convert.ToSingle(vecDict["w"])
                        );
                        material.SetVector(propertyName, vector);
                    }
                    else
                    {
                        throw new System.ArgumentException("Vector value must be a dictionary with x,y,z,w keys.");
                    }
                    break;
                default:
                    response["success"] = false;
                    response["message"] = $"Unsupported property type: {propertyType}";
                    return response;
            }
            EditorUtility.SetDirty(material); // Mark material as changed for save.
            AssetDatabase.SaveAssets(); // Save changes to the material asset
            response["success"] = true;
            response["message"] = $"Property '{propertyName}' set on material '{material.name}'.";
        }
        catch (System.Exception e)
        {
            response["success"] = false;
            response["message"] = $"Error setting property '{propertyName}': {e.Message}";
        }
        return response;
    }

    public static Dictionary<string, object> GetMaterialProperty(Dictionary<string, object> command)
    {
        var response = new Dictionary<string, object>();
        string materialPath = command["material_path"] as string;
        string propertyName = command["property_name"] as string;
        string propertyType = command["property_type"] as string;

        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            response["success"] = false;
            response["message"] = $"Material not found at path: {materialPath}";
            return response;
        }

        if (!material.HasProperty(propertyName))
        {
            response["success"] = false;
            response["message"] = $"Material '{material.name}' does not have property named '{propertyName}'.";
            return response;
        }

        try
        {
            object value = null;
            switch (propertyType.ToLower())
            {
                case "color":
                    Color color = material.GetColor(propertyName);
                    value = new Dictionary<string, object> { { "r", color.r }, { "g", color.g }, { "b", color.b }, { "a", color.a } };
                    break;
                case "float":
                    value = material.GetFloat(propertyName);
                    break;
                case "texture":
                    Texture texture = material.GetTexture(propertyName);
                    value = texture != null ? AssetDatabase.GetAssetPath(texture) : null;
                    break;
                case "vector":
                    Vector4 vector = material.GetVector(propertyName);
                    value = new Dictionary<string, object> { { "x", vector.x }, { "y", vector.y }, { "z", vector.z }, { "w", vector.w } };
                    break;
                default:
                    response["success"] = false;
                    response["message"] = $"Unsupported property type: {propertyType}";
                    return response;
            }
            response["success"] = true;
            response["value"] = value;
            response["message"] = $"Property '{propertyName}' retrieved from material '{material.name}'.";
        }
        catch (System.Exception e)
        {
            response["success"] = false;
            response["message"] = $"Error getting property '{propertyName}': {e.Message}";
        }
        return response;
    }

    public static Dictionary<string, object> AssignShader(Dictionary<string, object> command)
    {
        var response = new Dictionary<string, object>();
        string materialPath = command["material_path"] as string;
        string shaderName = command["shader_name"] as string;

        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            response["success"] = false;
            response["message"] = $"Material not found at path: {materialPath}";
            return response;
        }

        if (string.IsNullOrEmpty(shaderName))
        {
            response["success"] = false;
            response["message"] = "Shader name cannot be empty.";
            return response;
        }

        Shader shader = Shader.Find(shaderName);
        if (shader == null)
        {
            response["success"] = false;
            response["message"] = $"Shader '{shaderName}' not found.";
            return response;
        }

        try
        {
            material.shader = shader;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            response["success"] = true;
            response["message"] = $"Shader '{shaderName}' assigned to material '{material.name}' successfully.";
        }
        catch (System.Exception e)
        {
            response["success"] = false;
            response["message"] = $"Error assigning shader '{shaderName}' to material '{material.name}': {e.Message}";
        }
        
        return response;
    }
}
