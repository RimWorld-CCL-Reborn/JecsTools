using UnityEngine;
using Verse;

namespace CompToggleDef
{
    public class ToggleDefCardUtility
    {
        // RimWorld.CharacterCardUtility
        public static Vector2 CardSize = new Vector2(395f, 536f);

        public static float ButtonSize = 40f;

        public static float ForceButtonSize = 46f;

        public static float ForceButtonPointSize = 24f;

        public static float HeaderSize = 32f;

        public static float TextSize = 22f;

        public static float Padding = 3f;

        public static float SpacingOffset = 15f;

        public static float SectionOffset = 8f;

        public static float ColumnSize = 245f;

        public static float SkillsColumnHeight = 113f;

        public static float SkillsColumnDivider = 114f;

        public static float SkillsTextWidth = 138f;

        public static float SkillsBoxSize = 18f;

        public static float PowersColumnHeight = 195f;

        public static float PowersColumnWidth = 123f;

        public static bool isfirst = true;

        // RimWorld.CharacterCardUtility
        public static void DrawCard(Rect rect, ThingWithComps selectedThing)
        {
            GUI.BeginGroup(rect);

            var compToggleDef = selectedThing.GetComp<CompToggleDef>();

            if (compToggleDef != null)
            {
                var ts = Text.CalcSize(selectedThing.LabelCap).x;
                var y = rect.y;
                var rect2 = new Rect(rect.width / 2 - ts + SpacingOffset, y, rect.width, HeaderSize);
                y += rect2.height;
                Text.Font = GameFont.Medium;
                Widgets.Label(rect2, selectedThing.LabelCap);
                Text.Font = GameFont.Small;
                Widgets.ListSeparator(ref y, rect2.width, "Select one of the following:");

                // add all the buttons for the toggle defs
                foreach (var td in compToggleDef.toggleDefs)
                {
                    var rect3 = new Rect(0f, y, rect.width, 20f);
                    var isactive = false;
                    if (selectedThing.def == td) isactive = true;
                    if (Widgets.RadioButtonLabeled(rect3, td.LabelCap, isactive))
                    {
                        //Log.Message(".. change location to "+td.LabelCap);

                        // CHange def then give it a new id. Hopefully nothing index on the id
                        var map = selectedThing.Map;
                        var loc = selectedThing.Position;
                        var rot = selectedThing.Rotation;
                        selectedThing.DeSpawn();
                        selectedThing.def = td;
                        selectedThing.thingIDNumber = -1;
                        ThingIDMaker.GiveIDTo(selectedThing); // necessary
                        GenSpawn.Spawn(selectedThing, loc, map, rot);
                        break;
                    }
                    y += 25f;
                }
            }

            GUI.EndGroup();
        }
    }
}