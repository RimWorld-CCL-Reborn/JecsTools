using System;
using UnityEngine;
using Verse;

namespace JecsTools
{
    [StaticConstructorOnStartup]
    internal class TexButton
    {
        public static readonly Texture2D  quickstartIconTex = ContentFinder<Texture2D>.Get("quickstartIcon");

    }
}
