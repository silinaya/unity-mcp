from mcp.server.fastmcp import FastMCP, Context
from typing import Dict, Any, Optional
from unity_connection import get_unity_connection
# import base64 # Not needed for this tool

@mcp.tool()
def manage_material(
    ctx: Context,
    action: str,
    material_name: Optional[str] = None,
    shader_name: Optional[str] = None,
    material_path: Optional[str] = None,
    property_name: Optional[str] = None,
    property_type: Optional[str] = None, # "color", "float", "texture", "vector"
    value: Optional[Any] = None # This can be a dict for color/vector, string for texture path, float for float
) -> Dict[str, Any]:
    """
    Manages materials in Unity.

    Actions:
    - 'create_material': Creates a new material.
        Required Args: material_name (str)
        Optional Args: shader_name (str)
    - 'set_material_property': Sets a property on a material.
        Required Args: material_path (str), property_name (str), property_type (str), value (Any)
    - 'get_material_property': Gets a property from a material.
        Required Args: material_path (str), property_name (str), property_type (str)
    - 'assign_shader': Assigns a shader to a material.
        Required Args: material_path (str), shader_name (str)
    """
    try:
        params_for_unity = {"command": action}
        missing_args = []

        if action == "create_material":
            if material_name:
                params_for_unity["material_name"] = material_name
            else:
                missing_args.append("material_name")
            if shader_name: # Optional for create_material
                params_for_unity["shader_name"] = shader_name
        elif action == "set_material_property":
            if material_path: params_for_unity["material_path"] = material_path
            else: missing_args.append("material_path")
            if property_name: params_for_unity["property_name"] = property_name
            else: missing_args.append("property_name")
            if property_type: params_for_unity["property_type"] = property_type
            else: missing_args.append("property_type")
            # Value can be None (e.g. for unsetting a texture), so check explicitly for it being passed
            if "value" in ctx.raw_arguments: # Check if 'value' was explicitly passed
                 params_for_unity["value"] = value
            else:
                 missing_args.append("value")

        elif action == "get_material_property":
            if material_path: params_for_unity["material_path"] = material_path
            else: missing_args.append("material_path")
            if property_name: params_for_unity["property_name"] = property_name
            else: missing_args.append("property_name")
            if property_type: params_for_unity["property_type"] = property_type
            else: missing_args.append("property_type")
        elif action == "assign_shader":
            if material_path: params_for_unity["material_path"] = material_path
            else: missing_args.append("material_path")
            if shader_name: params_for_unity["shader_name"] = shader_name
            else: missing_args.append("shader_name")
        else:
            return {"success": False, "message": f"Unknown action for manage_material: {action}"}

        if missing_args:
            return {"success": False, "message": f"Missing required arguments for action '{action}': {', '.join(missing_args)}"}

        # Filter out None values for optional parameters not provided
        # This is mostly for shader_name in create_material if not given
        final_params_for_unity = {k: v for k, v in params_for_unity.items() if v is not None or k == "value"}
        # Special handling for 'value': if it's explicitly provided as None (e.g. for unsetting texture), it should be sent.
        if "value" in params_for_unity and params_for_unity["value"] is None and action == "set_material_property":
            final_params_for_unity["value"] = None


        unity_com = get_unity_connection()
        if not unity_com:
            return {"success": False, "message": "Unity connection not available."}
            
        response = unity_com.send_command("HandleManageMaterial", final_params_for_unity)

        if response and response.get("success"):
            # For get_material_property, the value is in response["value"]
            # For create_material, asset_path is in response["asset_path"]
            # We can put these into a "data" field for consistency or return them at top level.
            # The prompt suggests response.get("data"), so let's try to populate that.
            data_payload = {}
            if "value" in response: # From get_material_property
                data_payload["value"] = response["value"]
            if "asset_path" in response: # From create_material
                data_payload["asset_path"] = response["asset_path"]
            
            # Include any other relevant fields from response if necessary
            # For now, only specific fields are moved to data_payload
            
            return_payload = {
                "success": True, 
                "message": response.get("message", "Operation successful.")
            }
            if data_payload:
                return_payload["data"] = data_payload
            # Also include any other top-level keys from response that aren't success/message/value/asset_path
            for key, val in response.items():
                if key not in ["success", "message", "value", "asset_path"]:
                    if "data" not in return_payload:
                         return_payload["data"] = {}
                    return_payload["data"][key] = val
            return return_payload
        else:
            error_message = "An unknown error occurred on the Unity side."
            if response:
                error_message = response.get("message") or response.get("error", error_message)
            return {"success": False, "message": error_message}

    except Exception as e:
        # Log the full traceback for server-side debugging
        # import traceback
        # ctx.get_logger().error(f"Python error in manage_material: {str(e)}\n{traceback.format_exc()}")
        return {"success": False, "message": f"Python error in manage_material: {str(e)}"}

def register_manage_material_tools(mcp: FastMCP):
    """Registers the manage_material tool with the MCP server."""
    # The @mcp.tool() decorator should handle the registration.
    # This function is here to be imported by tools/__init__.py
    pass
