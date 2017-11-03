using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Pop
{
	#if UNITY_EDITOR
	public class PixelEditorWindow : EditorWindow
	{		
		// Add menu item named "My Window" to the Window menu
		[MenuItem("Window/Pixel Editor")]
		public static void ShowWindow()
		{
			//Show existing window instance. If one doesn't exist, make one.
			EditorWindow.GetWindow(typeof(PixelEditorWindow));
		}

		const int ViewDragButton = 2;	//	0left 1right 2middle
		Texture2D	CurrentTexture = null;
		bool		CurrentTextureDirty = false;
		Rect?		LastScreenRect = null;
		Rect?		LastTextureRect = null;

		[Range(1,10)]
		public float	Zoom = 1;
		public float	ScrollX = 0;
		public float	ScrollY = 0;
		bool			ModifyAlpha = false;

		void Awake()
		{
			this.wantsMouseMove = true;
		}

		void OnSelectionChange()
		{
			var SelectedTextures = GetSelectedTextures ();
			Texture2D SelectedTexture = null;
			try
			{
				SelectedTexture = SelectedTextures[0];
			}
			catch {
			}

			SetCurrentTexture (SelectedTexture);
		}

		void OnMouseEvent(Event MouseEvent)
		{
			if (MouseEvent.isScrollWheel) {
				Zoom -= MouseEvent.delta.y;
				OnGuiChanged ();
				return;
			}

			if ( MouseEvent.type == EventType.mouseDrag )
			{
				if (MouseEvent.button == ViewDragButton) {
					ScrollX -= MouseEvent.delta.x / Zoom;
					ScrollY -= MouseEvent.delta.y / Zoom;
					OnGuiChanged ();
					return;
				}
			}

			if ( MouseEvent.type != EventType.mouseMove )
				Debug.Log("Current detected event: " + MouseEvent);
		}

		List<Texture2D> GetSelectedTextures()
		{
			var Textures = new List<Texture2D> ();
			
			var AssetGuids = UnityEditor.Selection.assetGUIDs;
			foreach (var AssetGuid in AssetGuids) {
				var AssetPath = AssetDatabase.GUIDToAssetPath (AssetGuid);
				var Asset = AssetDatabase.LoadAssetAtPath<Texture2D> (AssetPath);
				if (Asset == null)
					continue;
				if (!(Asset is Texture2D))
					continue;

				Textures.Add (Asset);
			}
			return Textures;
		}

		void OnGuiChanged()
		{
			this.Repaint ();
		}

		void PromptUserToSaveChanges()
		{
		}

		void SaveChanges()
		{
			CurrentTextureDirty = false;
		}

		void SetCurrentTexture(Texture2D NewTexture)
		{
			if (CurrentTexture == NewTexture)
				return;

			if (CurrentTextureDirty) {
				PromptUserToSaveChanges ();
			}

			//	change to new texture
			CurrentTexture = NewTexture;
			CurrentTextureDirty = false;
			LastScreenRect = null;
			LastTextureRect = null;
		
			OnGuiChanged ();
		}

		//	gr: maybe support multiple?
		Texture2D GetEditingTexture()
		{
			return CurrentTexture;
		}

		static ATTR GetAttribute<ATTR,PARENT>(PARENT Parent,string PropertyName)
		{
			//	get attributes
			object[] Attributes = null;
			
			var Prop = typeof(PARENT).GetProperty(PropertyName);
			if (Prop != null) 
				Attributes = Prop.GetCustomAttributes (typeof(ATTR), false);

			var Field = typeof(PARENT).GetField (PropertyName);
			if ( Field != null )
				Attributes = Field.GetCustomAttributes (typeof(ATTR), false);

			var Members = typeof(PARENT).GetMember (PropertyName);
			foreach (var Member in Members) {
				var Attr = Member.GetCustomAttributes (typeof(ATTR), false);
				if (Attr != null)
					Attributes = Attr;
			}

			try
			{
				var Attribute = (ATTR)Attributes [0];
				return Attribute;
			}
			catch {
				return default(ATTR);
			}
		}

			
		static Vector2 GetRangeAttributeRange<T>(T Parent,string PropertyName)
		{
			var Attrib = GetAttribute<RangeAttribute,T>( Parent, PropertyName );
			if (Attrib == null)
				throw new System.Exception ("No range attrib on " + PropertyName + " found");
			return new Vector2 (Attrib.min, Attrib.max);
		}


		Rect GetTextureViewRect(float TextureWidth,float TextureHeight)
		{
			var ViewRect = new Rect (0, 0, TextureWidth, TextureHeight);

			ViewRect.x = ScrollX;
			ViewRect.y = ScrollY;
			ViewRect.width /= Zoom;
			ViewRect.height /= Zoom;

			return ViewRect;
		}

		//	normalise and flip
		Rect PixelRectToViewportRect(Rect PixelRect,float ViewportWidth,float ViewportHeight)
		{
			//	normalize
			PixelRect.x /= ViewportWidth;
			PixelRect.y /= ViewportHeight;
			PixelRect.width /= ViewportWidth;
			PixelRect.height /= ViewportHeight;

			PixelRect.y = 1 - PixelRect.y - PixelRect.height;
			return PixelRect;
		}



		void DrawTexture(Texture2D Texture,Rect ScreenRect,Rect TextureRect)
		{
			LastScreenRect = ScreenRect;
			LastTextureRect = TextureRect;

			var ViewRect = PixelRectToViewportRect (TextureRect, Texture.width, Texture.height);
			var Border = 0;

			//	supposed to only render in a repaint event
			//	https://docs.unity3d.com/ScriptReference/Graphics.DrawTexture.html
			if (Event.current.type == EventType.Repaint)
				Graphics.DrawTexture (ScreenRect, Texture, ViewRect, Border,Border,Border,Border );
		}

		void OnTextureGui(Texture2D Texture)
		{
			EditorGUILayout.HelpBox ("Editing " + Texture.name + "", MessageType.Info);

			//	render options
			try
			{
				var Range = GetRangeAttributeRange( this, "Zoom" );
				Zoom = EditorGUILayout.Slider( "Zoom", Zoom, Range.x, Range.y, null );
			}
			catch {
				Zoom = EditorGUILayout.FloatField ("Zoom",Zoom);
			}

			ScrollX = EditorGUILayout.Slider( "Scroll X", ScrollX, 0, Texture.width, null );
			ScrollY = EditorGUILayout.Slider( "Scroll Y", ScrollY, 0, Texture.height, null );


			ModifyAlpha = EditorGUILayout.Toggle ("Modify Alpha", ModifyAlpha,  new GUILayoutOption[]{});

			var Options = new GUILayoutOption[]{	GUILayout.ExpandWidth (true), GUILayout.ExpandHeight (true) };
			var ScreenRect = EditorGUILayout.GetControlRect (Options);
			var ViewRect = GetTextureViewRect (Texture.width, Texture.height);

			//	modify rendering rects to preserve aspect
			var Ratio = Texture.width / Texture.height;
			if (Ratio > 1) {
				ScreenRect.height = ScreenRect.width / Ratio;
				ViewRect.height = ViewRect.width / Ratio;
			} else {
				ScreenRect.width = ScreenRect.height / Ratio;
				ViewRect.width = ViewRect.height / Ratio;
			}

			DrawTexture (Texture, ScreenRect, ViewRect);

			EditorGUILayout.HelpBox ("Footer.", MessageType.Info);


			/*
			GUILayout.Label ("Base Settings", EditorStyles.boldLabel);
			myString = EditorGUILayout.TextField ("Text Field", myString);

			groupEnabled = EditorGUILayout.BeginToggleGroup ("Optional Settings", groupEnabled);
			myBool = EditorGUILayout.Toggle ("Toggle", myBool);
			myFloat = EditorGUILayout.Slider ("Slider", myFloat, -3, 3);
			EditorGUILayout.EndToggleGroup ();
			*/
		}

		void OnGUI()
		{
			var MouseEvent = Event.current;
			if (MouseEvent.isMouse || MouseEvent.isScrollWheel)
				OnMouseEvent (MouseEvent);


			var EditingTexture = GetEditingTexture ();

			if (EditingTexture != null) {
				OnTextureGui (EditingTexture);
			} else {
				EditorGUILayout.HelpBox ("Select a texture asset", MessageType.Error);
			}

		}


	}
	#endif
}
