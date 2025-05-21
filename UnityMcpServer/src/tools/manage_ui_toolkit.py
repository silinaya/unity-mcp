from mcp.server.fastmcp import FastMCP, Context
from typing import Dict, Any, Optional, List

from unity_connection import get_unity_connection

@mcp.tool()
def manage_ui_toolkit_elements(
    ctx: Context,
    action: str,

    # Panel/Root identification
    panel_name: Optional[str] = None, # Name of the GameObject hosting the UIDocument

    # Element identification (for manipulation, or as parent)
    # Path might be like "ElementName" or "ParentName/ElementName" or "#element-id"
    # The C# side will resolve this within a given panel's rootVisualElement.
    element_query_path: Optional[str] = None, 
    parent_element_query_path: Optional[str] = None, # To specify parent for new elements

    # Element properties for creation / manipulation
    name: Optional[str] = None,          # For VisualElement.name
    text: Optional[str] = None,          # For Label, Button, TextField value
    label_text: Optional[str] = None,    # For TextField's own label, Toggle, Slider
    initial_value_str: Optional[str] = None, # For TextField initial value
    initial_value_bool: Optional[bool] = None,# For Toggle initial value
    initial_value_float: Optional[float] = None,# For Slider initial value
    
    # TextField specific
    multiline: Optional[bool] = None,

    # Slider specific
    low_value: Optional[float] = None,
    high_value: Optional[float] = None,
    direction: Optional[str] = None, # "Horizontal", "Vertical"

    # Styling
    classes: Optional[List[str]] = None, # List of USS class names
    style_properties: Optional[Dict[str, Any]] = None, # e.g., {"width": 100, "height": "50px", "backgroundColor": {"r":1,"g":0,"b":0,"a":1}, "position": "absolute", "left": "10px"}
                                                      # C# side will need to parse color dicts if passed this way for styles.
                                                      # Alternatively, colors could be strings like "#FF0000" if C# handles StyleColor.FromString

    # UXML / PanelSettings specific for creation
    panel_settings_path: Optional[str] = None,
    uxml_asset_path: Optional[str] = None,

    # General manipulation
    is_enabled: Optional[bool] = None,
    is_visible: Optional[bool] = None, # For style.display
    new_parent_query_path: Optional[str] = None,
    class_name: Optional[str] = None # For add/remove single class

) -> Dict[str, Any]:
    """
    Manages UI Toolkit elements in Unity.

    Panel Context:
    Most actions operate within the context of a 'panel_name' (GameObject with UIDocument).
    'element_query_path' is used to find elements within that panel (e.g., "ElementName", "#element-id", "Parent/Child").

    Actions:
    - 'create_ui_toolkit_panel': Creates a GameObject with a UIDocument.
        Args: panel_name (str, for GameObject), panel_settings_path (Optional[str]), uxml_asset_path (Optional[str])
    
    - 'create_visual_element': Creates a generic VisualElement.
        Args: panel_name (str), parent_element_query_path (str), name (str), classes (Optional[List[str]]), style_properties (Optional[Dict])
    - 'create_label': Creates a Label.
        Args: panel_name (str), parent_element_query_path (str), name (str), text (str), classes (Optional[List[str]]), style_properties (Optional[Dict])
    - 'create_button': Creates a Button.
        Args: panel_name (str), parent_element_query_path (str), name (str), text (str), classes (Optional[List[str]]), style_properties (Optional[Dict])
    - 'create_textfield': Creates a TextField.
        Args: panel_name (str), parent_element_query_path (str), name (str), label_text (Optional[str]), 
              initial_value_str (Optional[str]), multiline (Optional[bool]), classes (Optional[List[str]]), style_properties (Optional[Dict])
    - 'create_toggle': Creates a Toggle.
        Args: panel_name (str), parent_element_query_path (str), name (str), label_text (Optional[str]), 
              initial_value_bool (Optional[bool]), classes (Optional[List[str]]), style_properties (Optional[Dict])
    - 'create_slider': Creates a Slider.
        Args: panel_name (str), parent_element_query_path (str), name (str), label_text (Optional[str]), 
              low_value (float), high_value (float), initial_value_float (Optional[float]), 
              direction (Optional[str]), classes (Optional[List[str]]), style_properties (Optional[Dict])

    - 'load_uxml': Clones a UXML template and adds it to a parent VisualElement.
        Args: panel_name (str), parent_element_query_path (str), uxml_asset_path (str)

    - 'set_element_style': Sets multiple inline style properties for an element.
        Args: panel_name (str), element_query_path (str), style_properties (Dict)
    - 'add_class': Adds a USS class to an element.
        Args: panel_name (str), element_query_path (str), class_name (str)
    - 'remove_class': Removes a USS class from an element.
        Args: panel_name (str), element_query_path (str), class_name (str)
    
    - 'set_text': Sets the text for Label, Button, TextField.
        Args: panel_name (str), element_query_path (str), text (str)
    - 'get_text': Gets the text from Label, Button, TextField.
        Args: panel_name (str), element_query_path (str)
    - 'set_textfield_value': Alias for set_text specifically for TextField.
        Args: panel_name (str), element_query_path (str), text (str)
    - 'get_textfield_value': Alias for get_text specifically for TextField.
        Args: panel_name (str), element_query_path (str)

    - 'set_toggle_value': Sets the value of a Toggle.
        Args: panel_name (str), element_query_path (str), initial_value_bool (bool)
    - 'get_toggle_value': Gets the value of a Toggle.
        Args: panel_name (str), element_query_path (str)
    - 'set_slider_value': Sets the value of a Slider.
        Args: panel_name (str), element_query_path (str), initial_value_float (float)
    - 'get_slider_value': Gets the value of a Slider.
        Args: panel_name (str), element_query_path (str)

    - 'set_element_enabled': Sets the enabled state of an element.
        Args: panel_name (str), element_query_path (str), is_enabled (bool)
    - 'set_element_visibility': Sets the visibility of an element (style.display).
        Args: panel_name (str), element_query_path (str), is_visible (bool)
    - 'delete_element': Removes an element from its parent.
        Args: panel_name (str), element_query_path (str)
    - 'set_parent': Moves an element to a new parent.
        Args: panel_name (str), element_query_path (str), new_parent_query_path (str)
    - 'button_simulate_click': (Experimental) Simulates a click on a button.
        Args: panel_name (str), element_query_path (str)
    """
    try:
        params_for_unity = {"command": action}

        # Consolidate all optional parameters
        if panel_name is not None: params_for_unity["panel_name"] = panel_name
        if element_query_path is not None: params_for_unity["element_query_path"] = element_query_path
        if parent_element_query_path is not None: params_for_unity["parent_element_query_path"] = parent_element_query_path
        if name is not None: params_for_unity["name"] = name
        if text is not None: params_for_unity["text"] = text
        if label_text is not None: params_for_unity["label_text"] = label_text
        if initial_value_str is not None: params_for_unity["initial_value_str"] = initial_value_str
        if initial_value_bool is not None: params_for_unity["initial_value_bool"] = initial_value_bool
        if initial_value_float is not None: params_for_unity["initial_value_float"] = initial_value_float
        if multiline is not None: params_for_unity["multiline"] = multiline
        if low_value is not None: params_for_unity["low_value"] = low_value
        if high_value is not None: params_for_unity["high_value"] = high_value
        if direction is not None: params_for_unity["direction"] = direction
        if classes is not None: params_for_unity["classes"] = classes
        if style_properties is not None: params_for_unity["style_properties"] = style_properties
        if panel_settings_path is not None: params_for_unity["panel_settings_path"] = panel_settings_path
        if uxml_asset_path is not None: params_for_unity["uxml_asset_path"] = uxml_asset_path
        if is_enabled is not None: params_for_unity["is_enabled"] = is_enabled
        if is_visible is not None: params_for_unity["is_visible"] = is_visible
        if new_parent_query_path is not None: params_for_unity["new_parent_query_path"] = new_parent_query_path
        if class_name is not None: params_for_unity["class_name"] = class_name
        
        unity_com = get_unity_connection()
        if not unity_com:
            return {"success": False, "message": "Unity connection not available."}
            
        response = unity_com.send_command("HandleManageUIToolkit", params_for_unity)

        if response and response.get("success"):
            return {"success": True, "message": response.get("message", "Operation successful."), "data": response.get("data")}
        else:
            error_msg = "An unknown error occurred on the Unity side."
            if response and (response.get("message") or response.get("error")):
                error_msg = response.get("message") or response.get("error")
            return {"success": False, "message": error_msg}

    except Exception as e:
        return {"success": False, "message": f"Python error in manage_ui_toolkit_elements: {str(e)}"}

def register_manage_ui_toolkit_tools(mcp: FastMCP):
    """Registers the manage_ui_toolkit_elements tool with the MCP server."""
    # The @mcp.tool() decorator should handle the registration.
    pass
