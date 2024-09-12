﻿using UtilLibs;

namespace CustomGameSettingsModifier
{
	internal class STRINGS
	{
		public class UI
		{
			public class CUSTOMGAMESETTINGSCHANGER
			{
				public static LocString BUTTONTEXT = (LocString)"Change Difficulty Settings";
				public static LocString CHANGEWARNING = (LocString)UIUtils.ColorText("Most of these changes require a reload!", "fec315");
				public static LocString CHANGEWARNINGTOOLTIP = (LocString)"Changing the majority of these settings wont have an immediate effect.\nTo activate the effects:\n1. Change settings to your desired configuration.\n2. Save the game.\n3. Load that save game.\n4. Your changes should now be active.";
				public class TITLE
				{
					public static LocString TITLETEXT = (LocString)"Difficulty Settings";
				}
				public class CLOSE
				{
					public static LocString TEXT = (LocString)"Close";
				}
			}
		}
	}
}
