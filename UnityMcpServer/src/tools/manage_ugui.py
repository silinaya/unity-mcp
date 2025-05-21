from mcp.server.fastmcp import FastMCP, Context
from typing import Dict, Any, Optional, List # Added List for potential future use

from unity_connection import get_unity_connection

# Helper to construct RectTransform properties dictionary
def _rect_props(
    anchored_position: Optional[Dict[str, float]] = None, # {"x": float, "y": float}
    size_delta: Optional[Dict[str, float]] = None,      # {"x": float, "y": float}
    anchor_min: Optional[Dict[str, float]] = None,      # {"x": float, "y": float}
    anchor_max: Optional[Dict[str, float]] = None,      # {"x": float, "y": float}
    pivot: Optional[Dict[str, float]] = None,           # {"x": float, "y": float}
    rotation_euler: Optional[Dict[str, float]] = None,  # {"x": float, "y": float, "z": float}
    scale: Optional[Dict[str, float]] = None            # {"x": float, "y": float, "z": float}
) -> Optional[Dict[str, Any]]:
    props = {}
    if anchored_position: props["anchored_position"] = anchored_position
    if size_delta: props["size_delta"] = size_delta
    if anchor_min: props["anchor_min"] = anchor_min
    if anchor_max: props["anchor_max"] = anchor_max
    if pivot: props["pivot"] = pivot
    if rotation_euler: props["rotation_euler"] = rotation_euler
    if scale: props["scale"] = scale
    return props if props else None

@mcp.tool()
def manage_ugui_elements(
    ctx: Context,
    action: str,
    element_path: Optional[str] = None, # Path to existing UI element for manipulation/deletion
    parent_path: Optional[str] = None,  # Path to parent RectTransform for creation
    name: Optional[str] = None,         # Name for new UI elements

    # Canvas specific
    render_mode: Optional[str] = None, # "ScreenSpaceOverlay", "ScreenSpaceCamera", "WorldSpace"
    sorting_order: Optional[int] = None,
    camera_name: Optional[str] = None,

    # RectTransform properties (passed as a dictionary)
    rect_properties: Optional[Dict[str, Any]] = None,

    # Element content/properties
    text_content: Optional[str] = None,
    font_size: Optional[int] = None,
    font_style: Optional[str] = None, # "Normal", "Bold", "Italic", "BoldAndItalic"
    alignment: Optional[str] = None, # e.g., "MiddleCenter"
    color: Optional[Dict[str, float]] = None, # {"r": float, "g": float, "b": float, "a": float}
    sprite_path: Optional[str] = None,    # Asset path for Image/Button sprite
    texture_path: Optional[str] = None,   # Asset path for RawImage texture
    placeholder_text: Optional[str] = None,
    button_text_content: Optional[str] = None, # Text for default button's child Text
    
    # Slider/Toggle specific
    min_value: Optional[float] = None,
    max_value: Optional[float] = None,
    value: Optional[float] = None, # For Slider
    is_on: Optional[bool] = None,  # For Toggle
    label_text: Optional[str] = None, # For Toggle label

    # General properties
    is_active: Optional[bool] = None,
    new_parent_path: Optional[str] = None

) -> Dict[str, Any]:
    """
    Manages UGUI elements in Unity.

    Actions:
    - 'create_canvas': Creates a new Canvas.
        Args: name, render_mode ("ScreenSpaceOverlay", "ScreenSpaceCamera", "WorldSpace"), 
              sorting_order (Optional[int]), camera_name (Optional[str] for relevant modes),
              rect_properties (Optional[Dict])
    - 'create_panel': Creates a Panel (Image) under a parent.
        Args: parent_path, name, rect_properties (Optional[Dict]), color (Optional[Dict])
    - 'create_image': Creates an Image under a parent.
        Args: parent_path, name, rect_properties (Optional[Dict]), sprite_path (Optional[str]), color (Optional[Dict])
    - 'create_rawimage': Creates a RawImage under a parent.
        Args: parent_path, name, rect_properties (Optional[Dict]), texture_path (Optional[str]), color (Optional[Dict])
    - 'create_text': Creates a Text element under a parent.
        Args: parent_path, name, rect_properties (Optional[Dict]), text_content, font_size (Optional[int]), 
              color (Optional[Dict]), font_style (Optional[str]), alignment (Optional[str])
    - 'create_button': Creates a Button under a parent.
        Args: parent_path, name, rect_properties (Optional[Dict]), button_text_content (Optional[str]), 
              sprite_path (Optional[str] for button image), color (Optional[Dict] for button image color)
    - 'create_inputfield': Creates an InputField under a parent.
        Args: parent_path, name, rect_properties (Optional[Dict]), 
              placeholder_text (Optional[str]), text_content (Optional[str] for initial text)
    - 'create_slider': Creates a Slider under a parent.
        Args: parent_path, name, rect_properties (Optional[Dict]), 
              min_value (Optional[float]), max_value (Optional[float]), value (Optional[float] for initial value)
    - 'create_toggle': Creates a Toggle under a parent.
        Args: parent_path, name, rect_properties (Optional[Dict]), label_text (Optional[str]), is_on (Optional[bool])
    
    - 'set_rect_transform': Sets RectTransform properties for an element.
        Args: element_path, rect_properties (Dict)
    - 'set_parent': Changes the parent of a UI element.
        Args: element_path, new_parent_path
    - 'set_active': Activates/deactivates a UI element.
        Args: element_path, is_active (bool)
    - 'delete_element': Deletes a UI element.
        Args: element_path
    - 'set_text_content': Sets the text of a Text or InputField element.
        Args: element_path, text_content (str)
    - 'get_text_content': Gets the text of a Text or InputField element.
        Args: element_path
    - 'set_image_sprite': Sets the sprite for an Image component.
        Args: element_path, sprite_path (str)
    - 'set_ui_color': Sets the color for Text, Image, RawImage.
        Args: element_path, color (Dict)
    - 'get_slider_value': Gets the value of a Slider.
        Args: element_path
    - 'set_slider_value': Sets the value of a Slider.
        Args: element_path, value (float)
    - 'get_toggle_value': Gets the state of a Toggle.
        Args: element_path
    - 'set_toggle_value': Sets the state of a Toggle.
        Args: element_path, is_on (bool)
    - 'button_simulate_click': (Experimental) Tries to invoke a button's onClick event.
        Args: element_path

    (More actions can be added for other properties and elements)
    """
    try:
        params_for_unity = {"command": action}

        # Common parameters
        if element_path: params_for_unity["element_path"] = element_path
        if parent_path: params_for_unity["parent_path"] = parent_path
        if name: params_for_unity["name"] = name
        if rect_properties: params_for_unity["rect_properties"] = rect_properties
        
        # Content/Style parameters
        if text_content is not None: params_for_unity["text_content"] = text_content # Allow empty string
        if font_size: params_for_unity["font_size"] = font_size
        if font_style: params_for_unity["font_style"] = font_style
        if alignment: params_for_unity["alignment"] = alignment
        if color: params_for_unity["color"] = color
        if sprite_path: params_for_unity["sprite_path"] = sprite_path
        if texture_path: params_for_unity["texture_path"] = texture_path
        if placeholder_text is not None: params_for_unity["placeholder_text"] = placeholder_text
        if button_text_content is not None: params_for_unity["button_text_content"] = button_text_content

        # Canvas specific
        if render_mode: params_for_unity["render_mode"] = render_mode
        if sorting_order is not None: params_for_unity["sorting_order"] = sorting_order
        if camera_name: params_for_unity["camera_name"] = camera_name

        # Slider/Toggle specific
        if min_value is not None: params_for_unity["min_value"] = min_value
        if max_value is not None: params_for_unity["max_value"] = max_value
        if value is not None: params_for_unity["value"] = value # Could be for slider or other controls
        if is_on is not None: params_for_unity["is_on"] = is_on
        if label_text is not None: params_for_unity["label_text"] = label_text
        
        # General properties
        if is_active is not None: params_for_unity["is_active"] = is_active
        if new_parent_path: params_for_unity["new_parent_path"] = new_parent_path

        # This function should ideally validate if required params for a given action are present.
        # For brevity in this subtask, we assume the LLM will call correctly or C# side handles missing params.

        unity_com = get_unity_connection()
        if not unity_com:
            return {"success": False, "message": "Unity connection not available."}

        response = unity_com.send_command("HandleManageUGUI", params_for_unity)

        if response and response.get("success"):
            return {"success": True, "message": response.get("message", "Operation successful."), "data": response.get("data")}
        else:
            error_msg = "An unknown error occurred on the Unity side."
            if response and (response.get("message") or response.get("error")):
                error_msg = response.get("message") or response.get("error")
            return {"success": False, "message": error_msg}

    except Exception as e:
        return {"success": False, "message": f"Python error in manage_ugui_elements: {str(e)}"}

def register_manage_ugui_tools(mcp: FastMCP):
    """Registers the manage_ugui_elements tool with the MCP server."""
    # The @mcp.tool() decorator should handle the registration.
    pass
